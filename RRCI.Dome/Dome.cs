using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using RRCI.Dome;
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

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

        private TraceLogger tl;
        private System.Threading.Timer heartbeatTimer;
        private readonly ArrayList supportedActions = new ArrayList();
        private Serial serial;

        private bool connected;
        private bool moving;
        private bool openingCommandActive;
        private bool closingCommandActive;
        private DateTime motionStartTime;
        private StatusForm statusForm;
        private Thread statusThread;
        private int lastPulseCount = 0;

private DateTime lastPulseCheckTime =
    DateTime.MinValue;

        private ShutterState lastKnownShutterState = ShutterState.shutterError;

        public Dome()
        {
            tl = new TraceLogger("", DriverId);
            tl.Enabled = TraceEnabled;

            supportedActions.Add("Calibrate");

            RoofTelemetry.EnablePushover =
                GetSetting(
                    "EnablePushover",
                    "False")
                .Equals(
                    "True",
                    StringComparison.OrdinalIgnoreCase);

            RoofTelemetry.PushoverToken =
                GetSetting(
                    "PushoverToken",
                    "");

            RoofTelemetry.PushoverUserKey =
                GetSetting(
                    "PushoverUserKey",
                    "");

            RoofTelemetry.NotifyRoofOpened =
                GetSetting(
                    "NotifyRoofOpened",
                    "True")
                .Equals(
                    "True",
                    StringComparison.OrdinalIgnoreCase);

            RoofTelemetry.NotifyRoofClosed =
                GetSetting(
                    "NotifyRoofClosed",
                    "True")
                .Equals(
                    "True",
                    StringComparison.OrdinalIgnoreCase);

            RoofTelemetry.NotifyRoofFault =
                GetSetting(
                    "NotifyRoofFault",
                    "True")
                .Equals(
                    "True",
                    StringComparison.OrdinalIgnoreCase);

            RoofTelemetry.NotifyConnectionLost =
                GetSetting(
                    "NotifyConnectionLost",
                    "True")
                .Equals(
                    "True",
                    StringComparison.OrdinalIgnoreCase);

            RoofTelemetry.NotifyConnectionRestored =
                GetSetting(
                    "NotifyConnectionRestored",
                    "True")
                .Equals(
                    "True",
                    StringComparison.OrdinalIgnoreCase);

            tl.LogMessage(
                "Constructor",
                "Driver starting");
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
            string value = GetSetting(key, defaultValue ? "True" : "False");

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

        

        private bool TraceEnabled =>
            GetBoolSetting("TraceLogger", false);

        #endregion

        #region Connection

        public bool Connected
        {
            get => connected;
            set
            {
                if (value == connected) return;

                if (value)
                    Connect();
                else
                    Disconnect();
            }
        }

        private async void SendNotification(
            NotificationType type,
            string message)
        {
            if (!RoofTelemetry.EnablePushover)
            {
                return;
            }

            bool enabled = false;

            switch (type)
            {
                case NotificationType.RoofOpened:
                    enabled =
                        RoofTelemetry.NotifyRoofOpened;
                    break;

                case NotificationType.RoofClosed:
                    enabled =
                        RoofTelemetry.NotifyRoofClosed;
                    break;

                case NotificationType.RoofFault:
                    enabled =
                        RoofTelemetry.NotifyRoofFault;
                    break;

                case NotificationType.ConnectionLost:
                    enabled =
                        RoofTelemetry.NotifyConnectionLost;
                    break;

                case NotificationType.ConnectionRestored:
                    enabled =
                        RoofTelemetry.NotifyConnectionRestored;
                    break;
            }

            if (!enabled)
            {
                return;
            }

            tl.LogMessage("Pushover", message);

            await PushoverNotifier.SendAsync(
                RoofTelemetry.PushoverToken,
                RoofTelemetry.PushoverUserKey,
                message);
        }
        private void Connect()
        {
            tl.LogMessage("Connect", "Connecting");

            try
            {
                CleanupSerial();

                serial = new Serial();

                // IMPORTANT: ASCOM.Utilities.Serial can leave the port in an unstable
                // state if DTR/RTS are changed. Use the default settings and rely on
                // the cleanup code below.
                serial.Handshake = SerialHandshake.None;
                serial.ReceiveTimeout = 5;

                string port = GetSetting("COM", "");
                string baud = GetSetting("Baud", "9600");

                if (string.IsNullOrWhiteSpace(port))
                    throw new DriverException("COM port not configured");

                serial.PortName = port;
                serial.Speed = GetSerialSpeed(baud);
                serial.Connected = true;

                Thread.Sleep(3000); // Allow Arduino reset and USB serial re-enumeration

                try { serial.ClearBuffers(); }
                catch { }

                connected = true;
                
                // -----------------------------------------
                // Initialize telemetry
                // -----------------------------------------

                RoofTelemetry.CurrentPulseCount = 0;

                RoofTelemetry.PercentOpen = 0;
                if (int.TryParse(
                    GetSetting(
                        "OpenPulseCount",
                        "5000"),
                    out int openPulses))
                {
                    RoofTelemetry.OpenPulseCount =
                        openPulses;
                }
                else
                {
                    RoofTelemetry.OpenPulseCount = 5000;
                }
                tl.LogMessage("Connect", $"Loaded OpenPulseCount={RoofTelemetry.OpenPulseCount}");
                using (Profile profile = new Profile())
                {
                    profile.DeviceType = "Dome";

                    tl.LogMessage(
                        "Connect",
                        "Profile OpenPulseCount=" +
                        profile.GetValue(
                            DriverId,
                            "OpenPulseCount",
                            "",
                            "NOTFOUND"));
                }
                RoofTelemetry.Moving = false;

                RoofTelemetry.Faulted = false;

                RoofTelemetry.FaultMessage = "";

                RoofTelemetry.ShutterState =
                    "Connected";

                RoofTelemetry.LastPulseTime =
                    DateTime.Now;

                // -----------------------------------------

                string pong = Query("ping", 5000);
                
                if (!pong.ToUpperInvariant().Contains("PONG"))
                    throw new DriverException("No PONG response from controller");

                Query(SafeModeEnabled ? "setsafe:1" : "setsafe:0");
                Query(MotionSensorEnabled ? "setmotion:1" : "setmotion:0");

                try
                {
                    UpdatePulseTelemetry();

                    string startupStatus =
                        Query("status", 3000)
                        .ToUpperInvariant();

                    tl.LogMessage(
                        "Connect",
                        $"Startup Status={startupStatus}");

                    if (startupStatus.Contains("STATE:OPEN;"))
                    {
                        RoofTelemetry.CurrentPulseCount =
                            RoofTelemetry.OpenPulseCount;

                        RoofTelemetry.PercentOpen = 100;

                        RoofTelemetry.ShutterState =
                            "Open";
                    }
                    else if (startupStatus.Contains("STATE:CLOSED;"))
                    {
                        RoofTelemetry.CurrentPulseCount = 0;

                        RoofTelemetry.PercentOpen = 0;

                        RoofTelemetry.ShutterState =
                            "Closed";
                    }

                    tl.LogMessage(
                        "Connect",
                        $"Initial PulseCount={RoofTelemetry.CurrentPulseCount}");

                    tl.LogMessage(
                        "Connect",
                        $"Initial PercentOpen={RoofTelemetry.PercentOpen}");
                }
                catch (Exception ex)
                {
                    tl.LogMessage(
                        "Connect",
                        $"Initial telemetry failed: {ex.Message}");
                }

                string controllerType = GetSetting( "ControllerType", "1");

                Query("setmode:" + controllerType);
                tl.LogMessage("Connect", $"ControllerType={controllerType}");

                StartHeartbeat();

                // -------------------------------------
                // Open live status window
                // -------------------------------------

                if (statusThread == null)
                {
                    statusThread = new Thread(() =>
                    {
                        statusForm = new StatusForm(this);

                        Application.Run(statusForm);
                    });

                    statusThread.SetApartmentState(
                        ApartmentState.STA);

                    statusThread.IsBackground = true;

                    statusThread.Start();
                }

                tl.LogMessage(
                    "Connect",
                    $"SafeMode={SafeModeEnabled}, MotionSensor={MotionSensorEnabled}");

                tl.LogMessage("Connect", "Connected");
            }
            catch (Exception ex)
            {
                tl.LogMessage("Connect", ex.ToString());
                connected = false;
                StopHeartbeat();
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
            RoofTelemetry.Moving = false;

            RoofTelemetry.ShutterState =
                "Disconnected";
            // -------------------------------------
            // Close status window
            // -------------------------------------

            if (statusForm != null)
            {
                try
                {
                    statusForm.Invoke(
                        new Action(() =>
                        {
                            statusForm.ForceClose();
                        }));
                }
                catch
                {
                }

                statusForm = null;
            }

            statusThread = null;
            StopHeartbeat();
            CleanupSerial();

            // Wait for Windows and the USB serial driver to fully release the port.
            Thread.Sleep(3000);

            tl.LogMessage("Disconnect", "Disconnected");
        }

        private void CleanupSerial()
        {
            Serial localSerial = serial;
            serial = null;

            if (localSerial == null)
                return;

            tl?.LogMessage("CleanupSerial", "Closing COM port");

            try
            {
                if (localSerial.Connected)
                    localSerial.Connected = false;
            }
            catch (Exception ex)
            {
                tl?.LogMessage("CleanupSerial", "Close exception: " + ex.Message);
            }

            try
            {
                localSerial.Dispose();
            }
            catch (Exception ex)
            {
                tl?.LogMessage("CleanupSerial", "Dispose exception: " + ex.Message);
            }

            // Release any remaining COM references.
            localSerial = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Thread.Sleep(3000);

            tl?.LogMessage("CleanupSerial", "COM port released");
        }

        private void EnsureConnected()
        {
            if (!connected)
                throw new NotConnectedException("Dome not connected");
        }

        #endregion

        #region Heartbeat

        private void StartHeartbeat()
        {
            StopHeartbeat();

            heartbeatTimer = new System.Threading.Timer(
                HeartbeatCallback,
                null,
                30000,
                30000);

            tl?.LogMessage("Heartbeat", "Started");
        }

        private void HeartbeatCallback(object state)
        {
            try
            {
                if (connected && serial != null)
                    Query("ping", 2000);
            }
            catch (Exception ex)
            {
                tl?.LogMessage("Heartbeat", ex.Message);
            }
        }

        private void StopHeartbeat()
        {
            if (heartbeatTimer == null)
                return;

            try
            {
                using (ManualResetEvent waitHandle = new ManualResetEvent(false))
                {
                    heartbeatTimer.Dispose(waitHandle);
                    waitHandle.WaitOne(5000);
                }
            }
            catch (Exception ex)
            {
                tl?.LogMessage("Heartbeat", "Stop exception: " + ex.Message);
            }

            heartbeatTimer = null;
            tl?.LogMessage("Heartbeat", "Stopped");
        }

        #endregion

        #region Serial Query

        private string Query(string command, int timeoutMs = DefaultTimeoutMs)
        {
            EnsureConnected();

            lock (this)
            {
                tl.LogMessage("TX", command);
                serial.Transmit(command + "#");

                DateTime timeout = DateTime.Now.AddMilliseconds(timeoutMs);

                while (DateTime.Now < timeout)
                {
                    try
                    {
                        string response = serial.ReceiveTerminated("#");
                        if (!string.IsNullOrWhiteSpace(response))
                        {
                            response = response.Trim();
                            tl.LogMessage("RX", response);
                            return response;
                        }
                    }
                    catch
                    {
                        Thread.Sleep(20);
                    }
                }

                throw new DriverException("Timeout waiting for response to: " + command);
            }
        }
        private void UpdatePulseTelemetry()
        {
            try
            {
                // -------------------------------------
                // Hall sensor disabled
                // -------------------------------------

                if (!MotionSensorEnabled)
                    return;

                // -------------------------------------
                // Request pulse count
                // -------------------------------------

                string response =
                    Query("getpulsecount", 2000);

                if (!response.StartsWith(
                    "PULSES:",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // -------------------------------------
                // Parse pulse count
                // -------------------------------------

                string value =
                    response.Replace("PULSES:", "")
                    .Trim();

                if (!int.TryParse(value, out int count))
                    return;

                // -------------------------------------
                // Update telemetry
                // -------------------------------------
                RoofTelemetry.CurrentPulseCount = count;
                tl.LogMessage("Telemetry", $"Pulse={RoofTelemetry.CurrentPulseCount}, Percent={RoofTelemetry.PercentOpen}");
                tl.LogMessage("Telemetry", $"Current={count} Open={RoofTelemetry.OpenPulseCount} Percent={RoofTelemetry.PercentOpen}");
                tl.LogMessage("PulseDebug", $"Assigned Count={count}");

                // -------------------------------------
                // Calculate roof percentage
                // -------------------------------------

                if (RoofTelemetry.OpenPulseCount > 0)
                {
                    int percent =
                        (int)(
                            (double)count /
                            RoofTelemetry.OpenPulseCount
                            * 100.0
                        );

                    percent = Math.Max(
                        0,
                        Math.Min(100, percent));

                    // ---------------------------------
                    // Reverse percentage while closing
                    // ---------------------------------

                    if (closingCommandActive)
                    {
                        RoofTelemetry.PercentOpen =
                            100 - percent;
                    }
                    else
                    {
                        RoofTelemetry.PercentOpen =
                            percent;
                    }
                }

                // -------------------------------------
                // Detect pulse activity
                // -------------------------------------

                if (count != lastPulseCount)
                {
                    RoofTelemetry.LastPulseTime =
                        DateTime.Now;

                    lastPulseCount = count;

                    lastPulseCheckTime =
                        DateTime.Now;
                }

                // -------------------------------------
                // Hall timeout protection
                // -------------------------------------

                if (moving &&
                    MotionSensorEnabled &&
                    (DateTime.Now - motionStartTime)
                    .TotalSeconds > 3)
                {
                    double pulseElapsed =
                        (
                            DateTime.Now -
                            RoofTelemetry.LastPulseTime
                        ).TotalSeconds;

                    if (pulseElapsed > 3)
                    {
                        tl.LogMessage(
                            "PulseMonitor",
                            "Hall pulse timeout");

                        moving = false;

                        openingCommandActive = false;
                        closingCommandActive = false;

                        try
                        {
                            Query("abort", 3000);
                        }
                        catch
                        {
                        }

                        RoofTelemetry.Moving = false;

                        RoofTelemetry.Faulted = true;

                        RoofTelemetry.FaultMessage =
                            "Hall pulse timeout";

                        RoofTelemetry.ShutterState =
                            "Error";

                        lastKnownShutterState =
                            ShutterState.shutterError;
                    }
                }

                // -------------------------------------
                // Overshoot protection
                // -------------------------------------

                if (RoofTelemetry.OpenPulseCount > 0)
                {
                    int maxAllowed =
                        (int)(
                            RoofTelemetry.OpenPulseCount
                            * 1.05
                        );

                    if (RoofTelemetry.CurrentPulseCount >
                        maxAllowed)
                    {
                        tl.LogMessage(
                            "PulseMonitor",
                            "Pulse overshoot detected");

                        moving = false;

                        openingCommandActive = false;
                        closingCommandActive = false;

                        try
                        {
                            Query("abort", 3000);
                        }
                        catch
                        {
                        }

                        RoofTelemetry.Moving = false;

                        RoofTelemetry.Faulted = true;

                        RoofTelemetry.FaultMessage =
                            "Pulse overshoot detected";

                        RoofTelemetry.ShutterState =
                            "Error";

                        lastKnownShutterState =
                            ShutterState.shutterError;
                    }
                }
            }
            catch (Exception ex)
            {
                tl.LogMessage(
                    "UpdatePulseTelemetry",
                    ex.Message);
            }
        }
        #endregion

        #region Shutter Control

        public ShutterState ShutterStatus
        {
            get
            {
                EnsureConnected();

                try
                {
                    string status = Query("status").ToUpperInvariant();

                    // -------------------------------------
                    // Extract pulse count from status reply
                    // -------------------------------------

                    try
                    {
                        int pulsePos = status.IndexOf("PULSES:");

                        if (pulsePos >= 0)
                        {
                            string pulseText =
                                status.Substring(pulsePos + 7);

                            int endPos =
                                pulseText.IndexOf(';');

                            if (endPos >= 0)
                            {
                                pulseText =
                                    pulseText.Substring(0, endPos);
                            }

                            if (int.TryParse(
                                pulseText.Trim(),
                                out int pulseCount))
                            {
                                
                                
                                RoofTelemetry.CurrentPulseCount =
                                    pulseCount;

                                tl.LogMessage("StatusPulse", $"PulseCount={pulseCount}");

                                if (RoofTelemetry.OpenPulseCount > 0)
                                {
                                    RoofTelemetry.PercentOpen =
                                        Math.Max(
                                            0,
                                            Math.Min(
                                                100,
                                                (int)(
                                                    pulseCount *
                                                    100.0 /
                                                    RoofTelemetry.OpenPulseCount)));
                                }
                            }
                        }
                    }
                    catch
                    {
                    }

                    // -------------------------------------------------
                    // Controller-reported error
                    // -------------------------------------------------

                    if (status.Contains("ERROR"))
                    {
                        moving = false;
                        openingCommandActive = false;
                        closingCommandActive = false;

                        RoofTelemetry.Moving = false;
                        RoofTelemetry.Faulted = true;
                        RoofTelemetry.FaultMessage =
                            "Controller reported error";

                        RoofTelemetry.ShutterState = "Error";

                        return lastKnownShutterState =
                            ShutterState.shutterError;
                    }

                    // -------------------------------------------------
                    // Sensor states
                    // -------------------------------------------------

                    bool openSensorActive = status.Contains("STATE:OPEN;");

                    bool closedSensorActive = status.Contains("STATE:CLOSED;");

                    // Controller forgot pulse count but roof is known open

                    if (openSensorActive &&
                        RoofTelemetry.CurrentPulseCount == 0 &&
                        RoofTelemetry.OpenPulseCount > 0)
                    {
                        RoofTelemetry.CurrentPulseCount =
                            RoofTelemetry.OpenPulseCount;

                        RoofTelemetry.PercentOpen = 100;
                    }

                    RoofTelemetry.OpenLimitActive =
                        openSensorActive;

                    RoofTelemetry.ClosedLimitActive =
                        closedSensorActive;

                    // -------------------------------------------------
                    // Motion handling
                    // -------------------------------------------------
                    UpdatePulseTelemetry();
                    if (moving)
                    {
                        // -------------------------------------
                        // Update hall pulse telemetry
                        // -------------------------------------

                        

                        double elapsed =
                            (DateTime.Now - motionStartTime)
                            .TotalSeconds;

                        // ---------------------------------------------
                        // Hard failsafe timeout
                        // ---------------------------------------------

                        if (elapsed > MotionTimeoutSeconds)
                        {
                            tl.LogMessage(
                                "ShutterStatus",
                                "Motion timeout reached");

                            moving = false;
                            openingCommandActive = false;
                            closingCommandActive = false;

                            RoofTelemetry.Moving = false;

                            RoofTelemetry.Faulted = true;

                            RoofTelemetry.FaultMessage =
                                "Hard movement timeout";

                            RoofTelemetry.ShutterState =
                                "Error";

                            return lastKnownShutterState =
                                ShutterState.shutterError;
                        }

                        // ---------------------------------------------
                        // Initial grace delay
                        // ---------------------------------------------

                        if (elapsed < SensorGraceDelaySeconds)
                        {
                            if (openingCommandActive)
                            {

                                UpdatePulseTelemetry();
                                RoofTelemetry.ShutterState =
                                    "Opening";

                                return ShutterState
                                    .shutterOpening;
                            }

                            if (closingCommandActive)
                            {
                                RoofTelemetry.ShutterState =
                                    "Closing";

                                return ShutterState
                                    .shutterClosing;
                            }
                        }

                        // ---------------------------------------------
                        // Opening logic
                        // ---------------------------------------------

                        if (openingCommandActive)
                        {
                            UpdatePulseTelemetry();

                            RoofTelemetry.ShutterState =
                                "Opening";

                            if (openSensorActive)
                            {
                                moving = false;
                                openingCommandActive = false;

                                RoofTelemetry.Moving = false;

                                RoofTelemetry.ShutterState =
                                    "Open";

                                RoofTelemetry.Faulted = false;
                                RoofTelemetry.FaultMessage = "";

                                SendNotification(
                                    NotificationType.RoofOpened,
                                    "🏠 RRCI roof opened");

                                return lastKnownShutterState =
                                    ShutterState.shutterOpen;
                            }

                            return ShutterState.shutterOpening;
                        }

                        // ---------------------------------------------
                        // Closing logic
                        // ---------------------------------------------

                        if (closingCommandActive)
                        {
                            RoofTelemetry.ShutterState =
                                "Closing";

                            if (closedSensorActive)
                            {
                                moving = false;
                                closingCommandActive = false;

                                RoofTelemetry.Moving = false;

                                RoofTelemetry.ShutterState =
                                    "Closed";

                                RoofTelemetry.Faulted = false;
                                RoofTelemetry.FaultMessage = "";

                                SendNotification(
                                    NotificationType.RoofClosed,
                                    "🏠 RRCI roof closed");

                                return lastKnownShutterState =
                                    ShutterState.shutterClosed;
                            }

                            return ShutterState
                                .shutterClosing;
                        }
                    }

                    // -------------------------------------------------
                    // Non-moving sensor states
                    // -------------------------------------------------

                    if (openSensorActive)
                    {
                        
                        RoofTelemetry.PercentOpen = 100;

                        RoofTelemetry.ShutterState = "Open";

                        RoofTelemetry.Faulted = false;
                        RoofTelemetry.FaultMessage = "";
                        
                        
                        return lastKnownShutterState =
                            ShutterState.shutterOpen;
                    }

                    if (closedSensorActive)
                    {
                        RoofTelemetry.CurrentPulseCount = 0;

                        RoofTelemetry.PercentOpen = 0;

                        RoofTelemetry.ShutterState = "Closed";

                        RoofTelemetry.Faulted = false;
                        RoofTelemetry.FaultMessage = "";

                        return lastKnownShutterState =
                            ShutterState.shutterClosed;
                    }

                    // -------------------------------------------------
                    // Controller reported OPENING
                    // -------------------------------------------------

                    if (status.Contains("OPENING"))
                    {
                        RoofTelemetry.ShutterState =
                            "Opening";

                        return lastKnownShutterState =
                            ShutterState.shutterOpening;
                    }

                    // -------------------------------------------------
                    // Controller reported CLOSING
                    // -------------------------------------------------

                    if (status.Contains("CLOSING"))
                    {
                        RoofTelemetry.ShutterState =
                            "Closing";

                        return lastKnownShutterState =
                            ShutterState.shutterClosing;
                    }

                    // -------------------------------------------------
                    // Controller reports IDLE while roof moving
                    // -------------------------------------------------

                    if (status.Contains("STATE:IDLE"))
                    {
                        if (moving)
                        {
                            if (openingCommandActive)
                            {
                                RoofTelemetry.ShutterState =
                                    "Opening";

                                return lastKnownShutterState =
                                    ShutterState.shutterOpening;
                            }

                            if (closingCommandActive)
                            {
                                RoofTelemetry.ShutterState =
                                    "Closing";

                                return lastKnownShutterState =
                                    ShutterState.shutterClosing;
                            }
                        }

                        RoofTelemetry.ShutterState =
                            "Idle";

                        return lastKnownShutterState;
                    }

                    // -------------------------------------------------
                    // Unknown state
                    // -------------------------------------------------

                    tl.LogMessage(
                        "ShutterStatus",
                        "Unknown status: " + status);

                    RoofTelemetry.ShutterState =
                        "Unknown";

                    return lastKnownShutterState;
                }
                catch (Exception ex)
                {
                    tl.LogMessage("ShutterStatus", ex.Message);

                    RoofTelemetry.Moving = false;

                    RoofTelemetry.Faulted = true;

                    RoofTelemetry.FaultMessage =
                        ex.Message;

                    RoofTelemetry.ShutterState = "Error";

                    return lastKnownShutterState =
                        ShutterState.shutterError;
                }
            }
        }

        public void OpenShutter()
        {
            EnsureConnected();

            string response = Query("open", 10000);

            if (!response.StartsWith(
                "OK",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new DriverException(response);
            }

            moving = true;

            openingCommandActive = true;
            closingCommandActive = false;

            motionStartTime = DateTime.Now;

            lastKnownShutterState =
                ShutterState.shutterOpening;

            // -----------------------------------------
            // Telemetry updates
            // -----------------------------------------

            RoofTelemetry.Moving = true;

            RoofTelemetry.MovementStartTime =
                motionStartTime;

            RoofTelemetry.ShutterState =
                "Opening";

            RoofTelemetry.Faulted = false;

            RoofTelemetry.FaultMessage = "";

            RoofTelemetry.LastPulseTime =
                DateTime.Now;
        }

        public void CloseShutter()
        {
            EnsureConnected();

            string response = Query("close", 10000);

            if (!response.StartsWith(
                "OK",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new DriverException(response);
            }

            moving = true;

            openingCommandActive = false;
            closingCommandActive = true;

            motionStartTime = DateTime.Now;

            lastKnownShutterState =
                ShutterState.shutterClosing;

            // -----------------------------------------
            // Telemetry updates
            // -----------------------------------------

            RoofTelemetry.Moving = true;

            RoofTelemetry.MovementStartTime =
                motionStartTime;

            RoofTelemetry.ShutterState =
                "Closing";

            RoofTelemetry.Faulted = false;

            RoofTelemetry.FaultMessage = "";

            RoofTelemetry.LastPulseTime =
                DateTime.Now;
        }

        public string StartCalibration()
        {
            CalibrateOpenPulses();

            return "Calibration Complete";
        }
        private void CalibrateOpenPulses()
        {
            EnsureConnected();

            tl.LogMessage(
                "Calibration",
                "Starting calibration");

            // -------------------------------------
            // Reset pulse counter
            // -------------------------------------

            Query("resetpulse", 3000);

            Thread.Sleep(1000);

            // -------------------------------------
            // Open roof
            // -------------------------------------

            OpenShutter();

            // -------------------------------------
            // Wait for roof fully open
            // -------------------------------------

            DateTime timeout =
                DateTime.Now.AddMinutes(5);

            while (DateTime.Now < timeout)
            {
                Thread.Sleep(500);

                string status =
                    Query("status", 3000)
                    .ToUpperInvariant();

                tl.LogMessage(
                    "Calibration",
                    $"Status={status}");

                

                if (status.StartsWith("STATE:OPEN;", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            // -------------------------------------
            // Final pulse count
            // -------------------------------------

            string response =
                Query("getpulsecount", 3000);

            tl.LogMessage("PulseDebug", $"Response={response}");

            int pulses = 0;

            string pulseText =
                response.Replace("PULSES:", "")
                        .Replace("#", "")
                        .Trim();

            int.TryParse(
                pulseText,
                out pulses);

            tl.LogMessage(
                "Calibration",
                $"Raw response={response}");

            tl.LogMessage(
                "Calibration",
                $"Pulse text={pulseText}");

            tl.LogMessage(
                "Calibration",
                $"Parsed pulses={pulses}");

            // Add 2% tolerance
            pulses =
                (int)(pulses * 1.02);

            RoofTelemetry.OpenPulseCount =
                pulses;

            // -------------------------------------
            // Save profile setting
            // -------------------------------------

            using (Profile profile =
                new Profile())
            {
                profile.DeviceType = "Dome";
                tl.LogMessage("Calibration", $"Saving OpenPulseCount={pulses}");
                profile.WriteValue(
                    DriverId,
                    "OpenPulseCount",
                    pulses.ToString());

                string verify =
                    profile.GetValue(
                        DriverId,
                        "OpenPulseCount",
                        "",
                        "NOTFOUND");

                tl.LogMessage(
                    "Calibration",
                    $"Verified OpenPulseCount={verify}");
            }
        }
        public void AbortSlew()
        {
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

            RoofTelemetry.Moving = false;

            RoofTelemetry.ShutterState =
                "Aborted";

            lastKnownShutterState =
                ShutterState.shutterError;
        }

        public bool Slewing => moving;

        #endregion

        #region Command Methods

        public void CommandBlind(string command, bool raw) => Query(command);

        public bool CommandBool(string command, bool raw)
        {
            string response = Query(command);
            return response.StartsWith("OK", StringComparison.OrdinalIgnoreCase);
        }

        public string CommandString(string command, bool raw) => Query(command);

        #endregion

        #region ASCOM Required Members

        public string Action(
    string actionName,
    string actionParameters)
        {
            if (string.Equals(
                actionName,
                "Calibrate",
                StringComparison.OrdinalIgnoreCase))
            {
                CalibrateOpenPulses();

                return "Calibration Complete";
            }

            throw new ActionNotImplementedException(
                actionName);
        }
        public ArrayList SupportedActions => supportedActions;

        public void SetupDialog()
        {
            using (SetupDialogForm form = new SetupDialogForm())
            {
                form.ShowDialog();
            }
        }

        public string DriverInfo => "Driver for Arduino Roof Controller";
        public string DriverVersion
        {
            get
            {
                Version v =
                    Assembly.GetExecutingAssembly()
                        .GetName()
                        .Version;

                return $"{v.Major}.{v.Minor}.{v.Build}";
            }
        }
        public short InterfaceVersion => 2;
        public string Name => "Rolling Roof Controller Interface";
        public string Description => "ASCOM Roof Controller";

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

        public double Altitude => throw new PropertyNotImplementedException();
        public double Azimuth => throw new PropertyNotImplementedException();

        public void FindHome() => throw new MethodNotImplementedException();
        public void Park() => throw new MethodNotImplementedException();
        public void SetPark() => throw new MethodNotImplementedException();
        public void SlewToAltitude(double altitude) => throw new MethodNotImplementedException();
        public void SlewToAzimuth(double azimuth) => throw new MethodNotImplementedException();
        public void SyncToAzimuth(double azimuth) => throw new MethodNotImplementedException();

        #endregion

        #region IDisposable

        public void Dispose()
        {
            try
            {
                tl?.LogMessage("Dispose", "Driver shutting down");
                StopHeartbeat();
                Disconnect();

                if (tl != null)
                {
                    tl.Enabled = false;
                    tl.Dispose();
                    tl = null;
                }
            }
            catch { }
        }

        #endregion
    }
}
