using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ahsoka.Services.Network.ESPWifi;

[ExcludeFromCodeCoverage]
internal class Constants
{
    #region Fields
    // From commands.h ESP-hosted API
    public const int SSID_LENGTH = 32;
    public const int PASSWORD_LENGTH = 64;
    public const int BSSID_LENGTH = 17;
    public const int MAC_LENGTH = 17;
    public const int STATUS_LENGTH = 14;
    #endregion
}

[ExcludeFromCodeCoverage]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct ESPHostedStationConfig
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.SSID_LENGTH)]
    public string ssid;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.PASSWORD_LENGTH)]
    public string password;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.BSSID_LENGTH)]
    public string bssid;
    [MarshalAs(UnmanagedType.U1)]
    public bool wpa3Supported;
    public int rssi;
    public int channel;
    public int encryptionMode;
    public ushort listenInterval;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.STATUS_LENGTH)]
    public string status;
};

[ExcludeFromCodeCoverage]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct MacString
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.MAC_LENGTH)]
    public string mac;
}

[ExcludeFromCodeCoverage]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct ESPHostedSoftAPConfig
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.SSID_LENGTH)]
    public string ssid;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.PASSWORD_LENGTH)]
    public string password;
    public int channel;
    public int encryptionMode;
    public ushort MaxConnections;
    public bool ssidHidden;
    public WifiBandwidth bandwidth;
}

[ExcludeFromCodeCoverage]
internal struct DHCPServerConfig
{
    public string address;
    public int addressMask;
    public int poolOffset;
    public int poolSize;
    public string dns;
}

[ExcludeFromCodeCoverage]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct ESPHostedWifiScanList
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.SSID_LENGTH)]
    public string ssid;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.BSSID_LENGTH)]
    public string bssid;
    public int rssi;
    public int channel;
    public int encryptionMode;
}

/*
 * This is how we would represent a C union of structures, but it is easier to invoke the
 * underlying function with one or the other and overload our function with two separate structures.
[System.Runtime.InteropServices.StructLayout(LayoutKind.Explicit)]
public struct ESPHostedControlConfig
{
    [System.Runtime.InteropServices.FieldOffset(0)] public ESPHostedStationConfig station;
    [System.Runtime.InteropServices.FieldOffset(0)] public ESPHostedSoftAPConfig softap;
}
*/

[ExcludeFromCodeCoverage]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct ESPHostedWifiConnectedStationsList
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.BSSID_LENGTH)]
    public string bssid;
    public int rssi;
}


internal enum WifiEncryptionMode : int
{
    WIFI_AUTH_OPEN = 0,
    WIFI_AUTH_WEP,
    WIFI_AUTH_WPA_PSK,
    WIFI_AUTH_WPA2_PSK,
    WIFI_AUTH_WPA_WPA2_PSK,
    WIFI_AUTH_WPA2_ENTERPRISE,
    WIFI_AUTH_WPA3_PSK,
    WIFI_AUTH_WPA2_WPA3_PSK,
    WIFI_AUTH_MAX
}

internal enum WifiSupportedEncryptionMode : int
{
    WIFI_AP_AUTH_OPEN = 0,
    WIFI_AP_AUTH_WPA_PSK = 2,
    WIFI_AP_AUTH_WPA2_PSK = 3,
    WIFI_AP_AUTH_WPA_WPA2_PSK = 4,
}

internal enum WifiMode : int
{
    WIFI_MODE_NONE = 0,  /* null mode */
    WIFI_MODE_STA,       /* WiFi station mode */
    WIFI_MODE_AP,        /* WiFi soft-AP mode */
    WIFI_MODE_APSTA,     /* WiFi station + soft-AP mode */
    WIFI_MODE_MAX
}
internal enum WifiBandwidth : int
{
    WIFI_BANDWIDTH_HT20 = 1,
    WIFI_BANDWIDTH_HT40
}

internal enum WifiPowerSaveMode : int
{
    WIFI_PS_MIN_MODEM = 1,
    WIFI_PS_MAX_MODEM,
    WIFI_PS_INVALID
}

internal enum WifiTxPowerLevel : sbyte
{
    WIFI_TX_POWER_LEVEL_2dB = 8,
    WIFI_TX_POWER_LEVEL_5dB = 20,
    WIFI_TX_POWER_LEVEL_7dB = 28,
    WIFI_TX_POWER_LEVEL_8dB = 34,
    WIFI_TX_POWER_LEVEL_11dB = 44,
    WIFI_TX_POWER_LEVEL_13dB = 52,
    WIFI_TX_POWER_LEVEL_14dB = 56,
    WIFI_TX_POWER_LEVEL_15dB = 60,
    WIFI_TX_POWER_LEVEL_16dB = 66,
    WIFI_TX_POWER_LEVEL_18dB = 72,
    WIFI_TX_POWER_LEVEL_20dB = 80
}