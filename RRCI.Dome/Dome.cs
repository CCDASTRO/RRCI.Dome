// =====================================================
// RRCI ASCOM Dome Driver
//
// Includes:
// - Proper Opening / Closing reporting
// - 2 second movement grace delay after open/close command
// - Slewing state support
// - 120 second timeout protection
// - Sensor confirmation required for Open/Closed
// - Abort support
// - Stable serial communication
// - ASCOM Dome compliance
// =====================================================

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
            tl.Enabled = true;
            tl.LogMessage("Constructor", "Driver starting");
        }
        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "Dome";

                profile.Register(
                    "RRCI.Dome",
                    "Rolling Roof Controller Interface");
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "Dome";

                profile.Unregister("RRCI.Dome");
            }
        }
        // =====================================================
        // CONNECTION
        // =====================================================

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

                using (Profile profile = new Profile())
                {
                    profile.DeviceType = "Dome";

                    string port = profile.GetValue(
                        DriverId,
                        "COM",
                        "",
                        "");

                    if (string.IsNullOrWhiteSpace(port))
                        throw new DriverException("COM port not configured");

                    serial.PortName = port;
                    serial.Speed = SerialSpeed.ps9600;
                }

                serial.Connected = true;

                // Arduino reset delay
                Thread.Sleep(2000);

                try
                {
                    serial.ClearBuffers();
                }
                catch
                {
                }

                connected = true;

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

        // =====================================================
        // SERIAL QUERY
        // =====================================================

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

                    throw new DriverException(
                        "Timeout waiting for response");
                }
                catch (Exception ex)
                {
                    tl.LogMessage("Query", ex.Message);
                    throw;
                }
            }
        }

        // =====================================================
        // SHUTTER STATUS
        // =====================================================

        public ShutterState ShutterStatus
        {
            get
            {
                EnsureConnected();

                try
                {
                    string status = Query("status");

                    bool openSensorActive =
                        status.Contains("OPEN");

                    bool closedSensorActive =
                        status.Contains("CLOSED");

                    // -------------------------------------------------
                    // MOVING STATE HAS PRIORITY
                    // -------------------------------------------------

                    if (moving)
                    {
                        double elapsed =
                            (DateTime.Now - motionStartTime).TotalSeconds;

                        // ---------------------------------------------
                        // Motion timeout protection
                        // ---------------------------------------------

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

                        // ---------------------------------------------
                        // Ignore sensors briefly after command so stale
                        // sensor states do not instantly report
                        // OPEN/CLOSED before movement starts
                        // ---------------------------------------------

                        if (elapsed < SensorGraceDelaySeconds)
                        {
                            if (openingCommandActive)
                                return ShutterState.shutterOpening;

                            if (closingCommandActive)
                                return ShutterState.shutterClosing;
                        }

                        // ---------------------------------------------
                        // OPEN command active
                        // ---------------------------------------------

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

                        // ---------------------------------------------
                        // CLOSE command active
                        // ---------------------------------------------

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

                    // -------------------------------------------------
                    // IDLE SENSOR REPORTING
                    // -------------------------------------------------

                    // Roof fully open
                    if (openSensorActive)
                    {
                        moving = false;
                        openingCommandActive = false;
                        closingCommandActive = false;

                        lastKnownShutterState =
                            ShutterState.shutterOpen;

                        return lastKnownShutterState;
                    }

                    // Roof fully closed
                    if (closedSensorActive)
                    {
                        moving = false;
                        openingCommandActive = false;
                        closingCommandActive = false;

                        lastKnownShutterState =
                            ShutterState.shutterClosed;

                        return lastKnownShutterState;
                    }

                    // ---------------------------------------------
                    // Neither sensor active:
                    // roof is partially open OR mechanical failure
                    // Never keep stale OPEN/CLOSED here
                    // ---------------------------------------------

                    tl.LogMessage(
                        "ShutterStatus",
                        "No sensors active — reporting ERROR");

                    moving = false;
                    openingCommandActive = false;
                    closingCommandActive = false;

                    lastKnownShutterState =
                        ShutterState.shutterError;

                    return lastKnownShutterState;
                }
                catch (Exception ex)
                {
                    tl.LogMessage(
                        "ShutterStatus",
                        ex.Message);

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

        // =====================================================
        // COMMANDS
        // =====================================================

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

        // =====================================================
        // REQUIRED ASCOM MEMBERS
        // =====================================================

        public string Action(string actionName, string actionParameters)
        {
            return "";
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
            "1.2.1";

        public short InterfaceVersion =>
            2;

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
                    throw new PropertyNotImplementedException(
                        "Slaved",
                        false);
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

        // =====================================================
        // DISPOSE
        // =====================================================

        public void Dispose()
        {
            tl.LogMessage("Dispose", "Driver shutting down");

            Disconnect();

            tl.Enabled = false;
            tl.Dispose();
        }
    }
}