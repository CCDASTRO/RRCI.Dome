using ASCOM.Utilities;
using System;

public class DomeSettings
{
    private const string DeviceType = "Dome";
    private const string DriverId = "RRCI.Dome";

    private readonly Profile _profile;

    public DomeSettings()
    {
        _profile = new Profile
        {
            DeviceType = DeviceType
        };
    }

    private string Get(string key, string defaultValue)
    {
        return _profile.GetValue(DriverId, key, defaultValue);
    }

    private void Set(string key, string value)
    {
        _profile.WriteValue(DriverId, key, value);
    }

    private bool GetBool(string key, bool defaultValue = false)
    {
        string val = Get(key, defaultValue ? "True" : "False");
        return val.Equals("True", StringComparison.OrdinalIgnoreCase) ||
               val == "1";
    }

    private void SetBool(string key, bool value)
    {
        Set(key, value ? "True" : "False");
    }

    // -------------------------
    // PUBLIC PROPERTIES
    // -------------------------

    public string COMPort
    {
        get => Get("COM", "");
        set => Set("COM", value);
    }

    public string Baud
    {
        get => Get("Baud", "9600");
        set => Set("Baud", value);
    }

    public string Timeout
    {
        get => Get("Timeout", "5000");
        set => Set("Timeout", value);
    }

    public string DeviceId
    {
        get => Get("DeviceId", DriverId);
        set => Set("DeviceId", value);
    }

    public bool SafeMode
    {
        get => GetBool("SafeMode");
        set => SetBool("SafeMode", value);
    }

    public bool AutoClose
    {
        get => GetBool("AutoClose");
        set => SetBool("AutoClose", value);
    }

    public bool RainSensor
    {
        get => GetBool("RainSensor");
        set => SetBool("RainSensor", value);
    }
}