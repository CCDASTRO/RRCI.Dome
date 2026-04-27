using ASCOM.DeviceInterface;

public static class RoofStateManager
{
    public static readonly object Lock = new object();

   // public static ShutterState State = ShutterState.shutterUnknown;
    public static bool Slewing = false;
    public static bool AbortRequested = false;

    public static bool Connected = false;
}