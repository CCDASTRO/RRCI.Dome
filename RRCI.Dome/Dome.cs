using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;

namespace RRCI.DomeDriver
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("9b8eb283-e2fe-4f80-abfe-ee9c8f51681c")]
    [ProgId("RRCI.Dome")]
    public class Dome : IDomeV2, IDisposable
    {
        private const string DriverId = "RRCI.Dome";
        private const int DefaultTimeoutMs = 3000;
        private const int MotionTimeoutSeconds = 120;
        private const int SensorGraceDelaySeconds = 2;

        private readonly TraceLogger tl;
        private readonly ArrayList supportedActions = new ArrayList();

        private Serial serial;

        private bool connected;
        private bool moving;

        private bool openingCommandActive;
        private bool closingCommandActive;

        private DateTime motionStartTime;

        private ShutterState lastKnownShutterState =
            ShutterState.shutterError;

        public Dome()
        {
            tl = new TraceLogger("", DriverId);

            // Match SetupDialogForm.cs which stores this as "TraceLogger"
            tl.Enabled = GetBoolSetting("TraceLogger", false);

            tl.LogMessage("Constructor", "Driver starting");
        }

        #region COM Registration

        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "Dome";
                profile.Register(DriverId, "Rolling Roof Controller Interface");
                profile.WriteValue(DriverId, "Description", "Rolling Roof Controller Interface");
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "Dome";
                profile.Unregister(DriverId);
            }
        }

        #endregion

        #region Profile Helpers

        private string GetSetting(string key, string defaultValue)
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "Dome";
                return profile.GetValue(DriverId, key, "", defaultValue);
            }
        }

        private bool GetBoolSetting(string key, bool defaultValue)
        {
            string value = GetSetting(
                key,
                defaultValue ? "True" : "False");

            return value.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        private SerialSpeed GetSerialSpeed(string baud)
        {
            switch (baud)
            {
                case "1200": return SerialSpeed.ps1200;
                case "2400": return SerialSpeed.ps2400;
                case "4800": return SerialSpeed.ps4800;
                case "9600": return SerialSpeed.ps9600;
                case "19200": return SerialSpeed.ps19200;
                case "38400": return SerialSpeed.ps38400;
                case "57600": return SerialSpeed.ps57600;
                case "115200": return SerialSpeed.ps115200;
                default: return SerialSpeed.ps9600;
            }
        }

        private bool SafeModeEnabled =>
            GetBoolSetting("SafeMode", false);

        private bool MotionSensorEnabled =>
            GetBoolSetting("MotionSensor", false);

        #endregion

        #region Connection

        public bool Connected
        {
            get => connected;
            set
            {
                if (value == connected)
                    return;

                if (value)
                    Connect();
                else
                    Disconnect();
            }
        }

        private void Connect()
        {
            tl.LogMessage("Connect", "Connecting");

            try
            {
                serial = new Serial();

                string port = GetSetting("COM", "");
                string baud = GetSetting("Baud", "9600");

                if (string.IsNullOrWhiteSpace(port))
                    throw new DriverException("COM port not configured");

                serial.PortName = port;
                serial.Speed = GetSerialSpeed(baud);

                serial.Connected = true;

                // Allow Arduino to reset after serial connection.
                Thread.Sleep(2500);

                try
                {
                    serial.ClearBuffers();
                }
                catch
                {
                }

                connected = true;

                // Verify communications.
                string pong = Query("ping", 5000);
                if (!pong.Contains("PONG"))
                    throw new DriverException("No PONG response from controller");

                // Send runtime configuration to the Arduino firmware.
                Query(SafeModeEnabled ? "setsafe:1" : "setsafe:0");
                Query(MotionSensorEnabled ? "setmotion:1" : "setmotion:0");

                tl.LogMessage(
                    "Connect",
                    $"SafeMode={SafeModeEnabled}, MotionSensor={MotionSensorEnabled}");

                tl.LogMessage("Connect", "Connected");
            }
            catch (Exception ex)
            {
                tl.LogMessage("Connect", ex.ToString());

                connected = false;
                CleanupSerial();

                throw;
            }
        }

        private void Disconnect()
        {
            tl.LogMessage("Disconnect", "Disconnecting");

            connected = false;
            moving = false;

            openingCommandActive = false;
            closingCommandActive = false;

            CleanupSerial();

            tl.LogMessage("Disconnect", "Disconnected");
        }

        private void CleanupSerial()
        {
            try
            {
                if (serial != null)
                {
                    try
                    {
                        serial.Connected = false;
                    }
                    catch
                    {
                    }

                    serial.Dispose();
                    serial = null;
                }
            }
            catch
            {
            }
        }

        private void EnsureConnected()
        {
            if (!connected)
                throw new NotConnectedException("Dome not connected");
        }

        #endregion

        #region Serial Query

        private string Query(string command, int timeoutMs = DefaultTimeoutMs)
        {
            EnsureConnected();

            lock (this)
            {
                try
                {
                    tl.LogMessage("TX", command);

                    serial.Transmit(command + "#");

                    DateTime timeout =
                        DateTime.Now.AddMilliseconds(timeoutMs);

                    while (DateTime.Now < timeout)
                    {
                        try
                        {
                            string response =
                                serial.ReceiveTerminated("#");

                            if (!string.IsNullOrWhiteSpace(response))
                            {
                                response =
                                    response.Trim().ToUpperInvariant();

                                tl.LogMessage("RX", response);

                                return response;
                            }
                        }
                        catch
                        {
                            Thread.Sleep(20);
                        }
                    }

                    throw new DriverException("Timeout waiting for response");
                }
                catch (Exception ex)
                {
                    tl.LogMessage("Query", ex.Message);
                    throw;
                }
            }
        }

        #endregion

        #region Shutter Status

        public ShutterState ShutterStatus
        {
            get
            {
                EnsureConnected();

                try
                {
                    string status = Query("status");

                    // Firmware explicitly reports an error state.
                    if (status.Contains("ERROR"))
                    {
                        moving = false;
                        openingCommandActive = false;
                        closingCommandActive = false;

                        lastKnownShutterState =
                            ShutterState.shutterError;

                        return lastKnownShutterState;
                    }

                    bool openSensorActive = status.Contains("OPEN");
                    bool closedSensorActive = status.Contains("CLOSED");

                    // Moving state takes priority.
                    if (moving)
                    {
                        double elapsed =
                            (DateTime.Now - motionStartTime).TotalSeconds;

                        // Driver-side timeout protection.
                        if (elapsed > MotionTimeoutSeconds)
                        {
                            tl.LogMessage(
                                "ShutterStatus",
                                "Motion timeout reached");

                            moving = false;
                            openingCommandActive = false;
                            closingCommandActive = false;

                            lastKnownShutterState =
                                ShutterState.shutterError;

                            return lastKnownShutterState;
                        }

                        // Ignore sensors briefly after issuing command.
                        if (elapsed < SensorGraceDelaySeconds)
                        {
                            if (openingCommandActive)
                                return ShutterState.shutterOpening;

                            if (closingCommandActive)
                                return ShutterState.shutterClosing;
                        }

                        if (openingCommandActive)
                        {
                            if (openSensorActive)
                            {
                                tl.LogMessage(
                                    "ShutterStatus",
                                    "OPEN sensor confirmed");

                                moving = false;
                                openingCommandActive = false;
                                closingCommandActive = false;

                                lastKnownShutterState =
                                    ShutterState.shutterOpen;

                                return lastKnownShutterState;
                            }

                            return ShutterState.shutterOpening;
                        }

                        if (closingCommandActive)
                        {
                            if (closedSensorActive)
                            {
                                tl.LogMessage(
                                    "ShutterStatus",
                                    "CLOSED sensor confirmed");

                                moving = false;
                                openingCommandActive = false;
                                closingCommandActive = false;

                                lastKnownShutterState =
                                    ShutterState.shutterClosed;

                                return lastKnownShutterState;
                            }

                            return ShutterState.shutterClosing;
                        }
                    }

                    // Idle sensor reporting.
                    if (openSensorActive)
                    {
                        moving = false;
                        openingCommandActive = false;
                        closingCommandActive = false;

                        lastKnownShutterState =
                            ShutterState.shutterOpen;

                        return lastKnownShutterState;
                    }

                    if (closedSensorActive)
                    {
                        moving = false;
                        openingCommandActive = false;
                        closingCommandActive = false;

                        lastKnownShutterState =
                            ShutterState.shutterClosed;

                        return lastKnownShutterState;
                    }

                    // Neither sensor active while idle.
                    tl.LogMessage(
                        "ShutterStatus",
                        "No sensors active - reporting ERROR");

                    moving = false;
                    openingCommandActive = false;
                    closingCommandActive = false;

                    lastKnownShutterState =
                        ShutterState.shutterError;

                    return lastKnownShutterState;
                }
                catch (Exception ex)
                {
                    tl.LogMessage("ShutterStatus", ex.Message);

                    moving = false;
                    openingCommandActive = false;
                    closingCommandActive = false;

                    lastKnownShutterState =
                        ShutterState.shutterError;

                    return lastKnownShutterState;
                }
            }
        }

        public void OpenShutter()
        {
            EnsureConnected();

            tl.LogMessage("OpenShutter", "Opening roof");

            string response = Query("open", 10000);

            if (!response.StartsWith("OK"))
                throw new DriverException(response);

            moving = true;
            openingCommandActive = true;
            closingCommandActive = false;

            motionStartTime = DateTime.Now;

            lastKnownShutterState =
                ShutterState.shutterOpening;
        }

        public void CloseShutter()
        {
            EnsureConnected();

            tl.LogMessage("CloseShutter", "Closing roof");

            string response = Query("close", 10000);

            if (!response.StartsWith("OK"))
                throw new DriverException(response);

            moving = true;
            openingCommandActive = false;
            closingCommandActive = true;

            motionStartTime = DateTime.Now;

            lastKnownShutterState =
                ShutterState.shutterClosing;
        }

        public void AbortSlew()
        {
            tl.LogMessage("AbortSlew", "Abort requested");

            try
            {
                Query("abort", 3000);
            }
            catch
            {
            }

            moving = false;
            openingCommandActive = false;
            closingCommandActive = false;

            lastKnownShutterState =
                ShutterState.shutterError;
        }

        public bool Slewing => moving;

        #endregion

        #region Commands

        public void CommandBlind(string command, bool raw)
        {
            Query(command);
        }

        public bool CommandBool(string command, bool raw)
        {
            string response = Query(command);
            return response.StartsWith("OK");
        }

        public string CommandString(string command, bool raw)
        {
            return Query(command);
        }

        #endregion

        #region ASCOM Required Members

        public string Action(string actionName, string actionParameters)
        {
            return string.Empty;
        }

        public ArrayList SupportedActions => supportedActions;

        public void SetupDialog()
        {
            using (SetupDialogForm form = new SetupDialogForm())
            {
                form.ShowDialog();
            }
        }

        public string DriverInfo =>
            "Driver for Arduino Roof Controller";

        public string DriverVersion =>
            "1.3.0";

        public short InterfaceVersion => 2;

        public string Name =>
            "Rolling Roof Controller Interface";

        public string Description =>
            "ASCOM Roof Controller";

        public bool AtHome => false;
        public bool AtPark => false;

        public bool CanFindHome => false;
        public bool CanPark => false;
        public bool CanSetPark => false;
        public bool CanSlave => false;
        public bool CanSyncAzimuth => false;
        public bool CanSetAltitude => false;
        public bool CanSetAzimuth => false;
        public bool CanSetShutter => true;

        public bool Slaved
        {
            get => false;
            set
            {
                if (value)
                    throw new PropertyNotImplementedException("Slaved", false);
            }
        }

        public double Altitude =>
            throw new PropertyNotImplementedException();

        public double Azimuth =>
            throw new PropertyNotImplementedException();

        public void FindHome() =>
            throw new MethodNotImplementedException();

        public void Park() =>
            throw new MethodNotImplementedException();

        public void SetPark() =>
            throw new MethodNotImplementedException();

        public void SlewToAltitude(double altitude) =>
            throw new MethodNotImplementedException();

        public void SlewToAzimuth(double azimuth) =>
            throw new MethodNotImplementedException();

        public void SyncToAzimuth(double azimuth) =>
            throw new MethodNotImplementedException();

        #endregion

        #region IDisposable

        public void Dispose()
        {
            tl.LogMessage("Dispose", "Driver shutting down");

            Disconnect();

            tl.Enabled = false;
            tl.Dispose();
        }

        #endregion
    }
}
