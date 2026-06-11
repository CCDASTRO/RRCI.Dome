using System;

namespace RRCI.DomeDriver
{
    public static class RoofTelemetry
    {
        //Pushover state

        public static bool EnablePushover = false;

        public static string PushoverToken = "";

        public static string PushoverUserKey = "";

        public static bool NotifyRoofOpened = true;

        public static bool NotifyRoofClosed = true;

        public static bool NotifyRoofFault = true;

        public static bool NotifyConnectionLost = true;

        public static bool NotifyConnectionRestored = true;

        // Pulse tracking
        public static int CurrentPulseCount = 0;

        // User calibrated full-open pulse count
        public static int OpenPulseCount = 5000;
        public static bool CalibrationRequested = false;

        // Calculated percentage open
        public static int PercentOpen = 0;

        // Hall pulse timing
        public static DateTime LastPulseTime =
            DateTime.MinValue;

        // Roof movement state
        public static bool Moving = false;

        // Human-readable state
        public static string ShutterState = "Closed";

        // Limit states
        public static bool OpenLimitActive = false;
        public static bool ClosedLimitActive = true;

        // Fault handling
        public static bool Faulted = false;

        public static string FaultMessage = "";

        // Movement timing
        public static DateTime MovementStartTime =
            DateTime.MinValue;
    }
}