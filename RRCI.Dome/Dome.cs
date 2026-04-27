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
                profile.WriteValue(driverID, "CLSID", "{9b8eb283-e2fe-4f80-abfe-ee9c8f51681c}");
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
        public bool Connected
        {
            get => connected;
            set
            {
                connected = value;
                shutter = value ? ShutterState.shutterClosed : shutter;
            }
        }

        // =====================================================
        // SHUTTER
        // =====================================================
        public ShutterState ShutterStatus => shutter;

        public void OpenShutter()
        {
            if (!connected) return;

            roofState = RoofState.Opening;
            moving = true;
            shutter = ShutterState.shutterOpening;

            SendCommand("open");
            SimulateMove();

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
            SimulateMove();

            roofState = RoofState.Closed;
            shutter = ShutterState.shutterClosed;
            moving = false;
        }

        public void AbortSlew()
        {
            abortMove = true;
            moving = false;
            SendCommand("stop");
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
            return "OK";
        }

        private void SendCommand(string cmd)
        {
            // SERIAL OUTPUT HERE
            // SerialPort.Write(cmd + "#");
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
            get { return "C# ASCOM Driver"; }
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
            get { return "RRCI"; }
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