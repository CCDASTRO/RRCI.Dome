using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using System;
using System.Collections;
using System.Runtime.InteropServices;

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
        private const int HeartbeatIntervalMs = 30000;

        private readonly TraceLogger tl;

        private Serial serial;
        private System.Threading.Timer heartbeatTimer;

        private bool connected;
        private bool moving;

        private readonly ArrayList supportedActions = new ArrayList();

        public Dome()
        {
            tl = new TraceLogger("", DriverId);
            tl.Enabled = GetTraceEnabled();

            tl.LogMessage("Constructor", "Driver starting");
        }

        // =====================================================
        // TRACE LOGGING
        // =====================================================

        private bool GetTraceEnabled()
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "Dome";

                string value = profile.GetValue(
                    DriverId,
                    "TraceLogger",
                    "",
                    "False");

                return value.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                       value.Equals("1", StringComparison.OrdinalIgnoreCase);
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
                tl.LogMessage("Connected Set", value.ToString());

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
            tl.LogMessage("Connect", "Starting connection");

            try
            {
                serial = new Serial();

                string port = GetSetting("COM", "");
                string baud = GetSetting("Baud", "9600");

                if (string.IsNullOrWhiteSpace(port))
                    throw new ASCOM.DriverException("COM port not configured");

                serial.PortName = port;
                serial.Speed = GetSerialSpeed(baud);

                serial.Connected = true;

                // Arduino USB reset delay
                System.Threading.Thread.Sleep(2500);

                try
                {
                    serial.ClearBuffers();
                }
                catch
                {
                }

                bool handshakeOk = false;

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        string reply = Query("ping", 3000);

                        if (!string.IsNullOrWhiteSpace(reply) &&
                            reply.Contains("PONG"))
                        {
                            handshakeOk = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        tl.LogMessage("Connect", "Handshake failed: " + ex.Message);
                        System.Threading.Thread.Sleep(500);
                    }
                }

                if (!handshakeOk)
                    throw new ASCOM.DriverException(
                        "No PONG response from controller");

                connected = true;

                StartHeartbeat();

                tl.LogMessage(
                    "Connect",
                    $"Connected to {port} @ {baud}");
            }
            catch (Exception ex)
            {
                tl.LogMessage("Connect", "FAILED: " + ex.Message);

                CleanupSerial();
                connected = false;

                throw;
            }
        }

        private void Disconnect()
        {
            tl.LogMessage("Disconnect", "Disconnecting");

            StopHeartbeat();
            CleanupSerial();

            connected = false;
            moving = false;

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

        // =====================================================
        // HEARTBEAT
        // =====================================================

        private void StartHeartbeat()
        {
            StopHeartbeat();

            heartbeatTimer = new System.Threading.Timer(
                _ => SendHeartbeat(),
                null,
                HeartbeatIntervalMs,
                HeartbeatIntervalMs);

            tl.LogMessage("Heartbeat", "Started");
        }

        private void StopHeartbeat()
        {
            if (heartbeatTimer != null)
            {
                heartbeatTimer.Dispose();
                heartbeatTimer = null;
            }

            tl.LogMessage("Heartbeat", "Stopped");
        }

        private void SendHeartbeat()
        {
            if (serial == null || !serial.Connected)
                return;

            try
            {
                Query("ping", 1000);
            }
            catch (Exception ex)
            {
                tl.LogMessage("Heartbeat", "Ping failed: " + ex.Message);
            }
        }

        // =====================================================
        // SERIAL QUERY
        // =====================================================

        private string Query(string command, int timeoutMs = DefaultTimeoutMs)
        {
            if (serial == null || !serial.Connected)
                throw new ASCOM.NotConnectedException("Dome not connected");

            lock (serial)
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
                                response = response.Trim();

                                tl.LogMessage("RX", response);

                                return response;
                            }
                        }
                        catch
                        {
                            System.Threading.Thread.Sleep(20);
                        }
                    }

                    throw new ASCOM.DriverException(
                        "Timeout waiting for response");
                }
                catch (Exception ex)
                {
                    tl.LogMessage("Query", ex.Message);
                    throw;
                }
            }
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

        private string GetSetting(string key, string defaultValue)
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "Dome";

                return profile.GetValue(
                    DriverId,
                    key,
                    "",
                    defaultValue);
            }
        }

        private void EnsureConnected()
        {
            if (!connected)
                throw new ASCOM.NotConnectedException(
                    "Dome not connected");
        }

        // =====================================================
        // SHUTTER
        // =====================================================

        public ShutterState ShutterStatus
        {
            get
            {
                string status = Query("status");

                if (status.Contains("OPENING"))
                    return ShutterState.shutterOpening;

                if (status.Contains("CLOSING"))
                    return ShutterState.shutterClosing;

                if (status.Contains("OPEN"))
                    return ShutterState.shutterOpen;

                if (status.Contains("CLOSED"))
                    return ShutterState.shutterClosed;

                return ShutterState.shutterError;
            }
        }

        public void OpenShutter()
        {
            EnsureConnected();

            tl.LogMessage("OpenShutter", "Opening");

            moving = true;

            try
            {
                string response = Query("open", 10000);

                if (!response.StartsWith("OK"))
                    throw new ASCOM.DriverException(response);
            }
            finally
            {
                moving = false;
            }
        }

        public void CloseShutter()
        {
            EnsureConnected();

            tl.LogMessage("CloseShutter", "Closing");

            moving = true;

            try
            {
                string response = Query("close", 10000);

                if (!response.StartsWith("OK"))
                    throw new ASCOM.DriverException(response);
            }
            finally
            {
                moving = false;
            }
        }

        public void AbortSlew()
        {
            moving = false;

            try
            {
                Query("abort");
            }
            catch
            {
            }
        }

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

        public string DriverVersion => "1.1.3";

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

        public bool Slewing => moving;

        public bool Slaved
        {
            get => false;
            set
            {
                if (value)
                    throw new ASCOM.PropertyNotImplementedException(
                        "Slaved", false);
            }
        }

        public double Altitude =>
            throw new ASCOM.PropertyNotImplementedException();

        public double Azimuth =>
            throw new ASCOM.PropertyNotImplementedException();

        public void FindHome() =>
            throw new ASCOM.MethodNotImplementedException();

        public void Park() =>
            throw new ASCOM.MethodNotImplementedException();

        public void SetPark() =>
            throw new ASCOM.MethodNotImplementedException();

        public void SlewToAltitude(double altitude) =>
            throw new ASCOM.MethodNotImplementedException();

        public void SlewToAzimuth(double azimuth) =>
            throw new ASCOM.MethodNotImplementedException();

        public void SyncToAzimuth(double azimuth) =>
            throw new ASCOM.MethodNotImplementedException();

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