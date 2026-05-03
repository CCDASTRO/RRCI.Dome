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
        #region COM Registration

        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "Dome";

                string driverID = "RRCI.Dome";

                profile.Register(driverID, "RRCI Dome Driver");

                //profile.WriteValue(driverID, "CLSID", t.GUID.ToString("B"));
                profile.WriteValue(driverID, "Description", "RRCI Dome Driver");
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "Dome";

                string driverID = "RRCI.Dome";

                profile.Unregister(driverID);
            }
        }

        #endregion

        


        private bool connected;
        private bool moving;
        private bool abortMove;

        private ShutterState shutter = ShutterState.shutterClosed;

        private enum RoofState
        {
            Closed,
            Open,
            Opening,
            Closing,
            Unknown
        }

        private RoofState roofState = RoofState.Unknown;

        // =====================================================
        // CONNECTION
        // =====================================================
        private System.Threading.Timer heartbeatTimer;
        private string ReadResponse()

        {
            if (serial == null || !serial.Connected)
                throw new ASCOM.NotConnectedException("Serial not connected");

            try
            {
                return serial.ReceiveTerminated("#");
            }
            catch
            {
                return "";
            }
        }

        private void SendHeartbeat()
        {
            if (serial != null && serial.Connected)
            {
                SendCommand("ping");
            }
        }
        private ASCOM.Utilities.SerialSpeed GetSerialSpeed(string baud)
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
                default: return SerialSpeed.ps9600; // fallback
            }
        }
        private ASCOM.Utilities.Serial serial;
        public bool Connected
        {
            get => connected;

            set
            {
                if (value == connected)
                    return;

                if (value)
                {
                    try
                    {
                        // ----------------------------
                        // CREATE ASCOM SERIAL
                        // ----------------------------
                        serial = new ASCOM.Utilities.Serial();

                        string port = GetSetting("COM", "");
                        string baud = GetSetting("Baud", "9600");

                        if (string.IsNullOrWhiteSpace(port))
                            throw new ASCOM.DriverException("COM port not configured");

                        serial.PortName = port;
                        serial.Speed = GetSerialSpeed(baud);

                        // ----------------------------
                        // OPEN PORT (IMPORTANT FIRST STEP)
                        // ----------------------------
                        serial.Connected = true;

                        // ----------------------------
                        // CRITICAL: allow Arduino USB reset
                        // ----------------------------
                        System.Threading.Thread.Sleep(2500);

                        // ----------------------------
                        // CLEAR STARTUP GARBAGE
                        // ----------------------------
                        try { serial.ClearBuffers(); } catch { }

                        // ----------------------------
                        // HANDSHAKE RETRY LOOP (ROBUST)
                        // ----------------------------
                        string reply = null;
                        bool ok = false;

                        for (int attempt = 0; attempt < 3; attempt++)
                        {
                            try
                            {
                                serial.Transmit("ping#");

                                // wait for response with timeout safety
                                DateTime timeout = DateTime.Now.AddSeconds(3);

                                while (DateTime.Now < timeout)
                                {
                                    try
                                    {
                                        reply = serial.ReceiveTerminated("#");
                                        break;
                                    }
                                    catch
                                    {
                                        System.Threading.Thread.Sleep(50);
                                    }
                                }

                                if (!string.IsNullOrWhiteSpace(reply) &&
                                    reply.Contains("PONG"))
                                {
                                    ok = true;
                                    break;
                                }
                            }
                            catch
                            {
                                // ignore and retry
                            }

                            System.Threading.Thread.Sleep(500);
                        }

                        // ----------------------------
                        // FAIL SAFE IF NO RESPONSE
                        // ----------------------------
                        if (!ok)
                        {
                            try { serial.Connected = false; } catch { }
                            serial = null;

                            throw new ASCOM.DriverException(
                                "No PONG response from dome controller after 3 attempts"
                            );
                        }

                        // ----------------------------
                        // START HEARTBEAT ONLY AFTER SUCCESS
                        // ----------------------------
                        StartHeartbeat();

                        connected = true;
                    }
                    catch
                    {
                        try
                        {
                            if (serial != null)
                            {
                                try { serial.Connected = false; } catch { }
                                serial = null;
                            }
                        }
                        catch { }

                        connected = false;
                        throw;
                    }
                }
                else
                {
                    // ----------------------------
                    // DISCONNECT CLEANLY
                    // ----------------------------
                    StopHeartbeat();

                    try
                    {
                        if (serial != null)
                        {
                            try { serial.Connected = false; } catch { }
                            serial = null;
                        }
                    }
                    catch { }

                    connected = false;
                }
            }
        }
        private string GetSetting(string key, string defaultValue)
{
    using (Profile profile = new Profile())
    {
        profile.DeviceType = "Dome";
        return profile.GetValue("RRCI.Dome", key, "", defaultValue);
    }
}

        private void StartHeartbeat()
        {
            heartbeatTimer = new System.Threading.Timer(
                _ => SendHeartbeat(),
                null,
                0,
                30000
            );
        }

        private void StopHeartbeat()
        {
            heartbeatTimer?.Dispose();
            heartbeatTimer = null;
        }
        // =====================================================
        // SHUTTER
        // =====================================================

        private string GetStatus()
        {
            SendCommand("status");
            return ReadResponse();
        }

        public ShutterState ShutterStatus
        {
            get
            {
                string s = GetStatus();

                if (s.Contains("OPEN;")) return ShutterState.shutterOpen;
                if (s.Contains("CLOSED;")) return ShutterState.shutterClosed;
                if (s.Contains("OPENING;")) return ShutterState.shutterOpening;
                if (s.Contains("CLOSING;")) return ShutterState.shutterClosing;

                return ShutterState.shutterError;
            }
        }

        public void OpenShutter()
        {
            if (!connected) return;

            roofState = RoofState.Opening;
            moving = true;
            shutter = ShutterState.shutterOpening;

            SendCommand("open");
            string response = ReadResponse();

            if (!response.StartsWith("OK"))
                throw new ASCOM.DriverException("Command failed: " + response);

            roofState = RoofState.Open;
            shutter = ShutterState.shutterOpen;
            moving = false;
        }

        public void CloseShutter()
        {
            if (!connected) return;

            roofState = RoofState.Closing;
            moving = true;
            shutter = ShutterState.shutterClosing;

            SendCommand("close");
            string response = ReadResponse();

            if (!response.StartsWith("OK"))
                throw new ASCOM.DriverException("Command failed: " + response);

            roofState = RoofState.Closed;
            shutter = ShutterState.shutterClosed;
            moving = false;
        }

        public void AbortSlew()
        {
            abortMove = true;
            moving = false;
            SendCommand("abort");
        }

        // =====================================================
        // COMMANDS (SINGLE SIGNATURE ONLY)
        // =====================================================
        public void CommandBlind(string command, bool raw)
        {
            SendCommand(command);
        }

        public bool CommandBool(string command, bool raw)
        {
            SendCommand(command);
            return true;
        }

        public string CommandString(string command, bool raw)
        {
            SendCommand(command);
            return ReadResponse();
        }

        private void SendCommand(string cmd)
        {
            if (serial == null || !serial.Connected)
                throw new ASCOM.NotConnectedException("Dome not connected");

            serial.Transmit(cmd + "#");
        }

        // =====================================================
        // SIMPLE SIMULATION LOOP (SAFE FOR COM EXPORT)
        // =====================================================
        private void SimulateMove()
        {
            int t = 0;

            while (t < 10 && !abortMove)
            {
                System.Threading.Thread.Sleep(200);
                t++;
            }

            abortMove = false;
        }

        // =====================================================
        // ASCOM REQUIRED STUBS
        // =====================================================
        public string Action(string actionName, string actionParameters)
        {
            return "";
        }
        private readonly System.Collections.ArrayList _supportedActions = new System.Collections.ArrayList();

        public System.Collections.ArrayList SupportedActions
        {
            get { return _supportedActions; }
        }
        public void SetupDialog()
        {
            try
            {
                using (SetupDialogForm form = new SetupDialogForm())
                {
                    form.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    "SetupDialog failed: " + ex.Message);
            }
        }
        public string DriverInfo
        {
            get { return "Driver for Arduino Roof Controler, ccdastro.net"; }
        }

        public string DriverVersion
        {
            get { return "1.0"; }
        }

        public short InterfaceVersion
        {
            get { return 2; }
        }

        public string Name
        {
            get { return "Rolling Roof Controller Interface (RRCI)"; }
        }

        public string Description
        {
            get { return "ASCOM Roof Controller"; }
        }

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
            get
            {
                // Since slaving is not supported, always return false
                return false;
            }
            set
            {
                // If client tries to enable slaving, reject it
                if (value)
                {
                    throw new ASCOM.PropertyNotImplementedException("Slaved", false);
                }

                
            }
        }

        public double Altitude => throw new ASCOM.PropertyNotImplementedException();
        public double Azimuth => throw new ASCOM.PropertyNotImplementedException();

        public void FindHome() => throw new ASCOM.MethodNotImplementedException();
        public void Park() => throw new ASCOM.MethodNotImplementedException();
        public void SetPark() => throw new ASCOM.MethodNotImplementedException();
        public void SlewToAltitude(double Altitude) => throw new ASCOM.MethodNotImplementedException();
        public void SlewToAzimuth(double Azimuth) => throw new ASCOM.MethodNotImplementedException();
        public void SyncToAzimuth(double Azimuth) => throw new ASCOM.MethodNotImplementedException();

        // =====================================================
        // DISPOSE
        // =====================================================
        public void Dispose()
        {
            try
            {
                abortMove = true;
                connected = false;
                moving = false;
            }
            catch { }
        }
    }
}