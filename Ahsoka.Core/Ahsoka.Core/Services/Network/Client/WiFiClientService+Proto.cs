using ProtoBuf;
using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.Network;

[ProtoContract]
public class WiFiClientMessageTypes
{
    public enum Ids
    {
        None = 0,
        GetAdapters = 1,
        GetNetworkConfiguration = 6,
        SetNetworkConfiguration = 8,

        // Client Only Calls
        DiscoverNetworks = 9,
        ConnectToNetwork = 12,
        DisconnectNetwork = 13,
        ConnectionStateNotification = 14,
        GetNetworkProfiles = 15,
        SetNetworkProfiles = 17,
        GetNetworkInfo = 18,
        WirelessSignalChanged = 19,
        AttemptAutoConnect = 20,
    }
}

//---------------------------------
//  Adapter Configuration
//---------------------------------
[ProtoContract]
public class AdapterRequestList
{
    [ProtoMember(1, IsRequired = true)]
    public List<AdapterRequest> Adapters { get; set; } = new();
}

//---------------------------------
//  Adapter Configuration
//---------------------------------
[ProtoContract]
public class AdapterRequest
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }
}

/// <summary>
/// Request Object for Requesting a WiFi Connection be Started.
/// Adds Default Timeout Values for Connection, IP Address and Time Sync.
/// </summary>
[ProtoContract]
public partial class AutoConnectRequest
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(8)]
    public int WaitForConnectionSeconds { get; set; }

    [ProtoMember(9)]
    public int WaitForIpAddressSeconds { get; set; }

    [ProtoMember(10)]
    public int WaitForTimeSyncSeconds { get; set; }

    /// <summary>
    /// Default Constructor
    /// </summary>
    public AutoConnectRequest()
    {
        this.WaitForConnectionSeconds = 60;
        this.WaitForIpAddressSeconds = 30;
        this.WaitForTimeSyncSeconds = 30;
    }
}


[ProtoContract]
public class AdapterConfiguration
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2)]
    public string Description { get; set; }

    [ProtoMember(4)]
    public IPConfiguration ClientConfiguration { get; set; }

    [ProtoMember(7)]
    public string OverrideMacAddress { get; set; }

    [ProtoMember(99)]
    public bool SaveAsDefault { get; set; }
}

[ProtoContract]
public class AdapterResult
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2)]
    public string ErrorDescription { get; set; }

    [ProtoMember(3)]
    public bool ConfigurationSuccessful { get; set; }
}

[ProtoContract]
public class IPConfiguration
{
    [ProtoMember(1)]
    public IPAddressConfiguration ConnectionType { get; set; }

    [ProtoMember(2)]
    public string IpAddress { get; set; }

    [ProtoMember(3)]
    public string GatewayIp { get; set; }

    [ProtoMember(4)]
    public string Dns { get; set; }
}

public enum IPAddressConfiguration
{
    Dhcp,
    StaticIp
}

//---------------------------------
//  Wireless Network Connections
//---------------------------------
[ProtoContract]
public class WirelessNetworkInfoList
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2, IsRequired = true)]
    public List<WirelessNetworkInfo> Networks { get; set; } = new();
}

[ProtoContract]
public class WirelessNetworkInfo
{
    [ProtoMember(1)]
    public SecurityType SecurityType { get; set; }

    [ProtoMember(2)]
    public WirelessConnectionState State { get; set; }

    [ProtoMember(3)]
    public string Ssid { get; set; }

    [ProtoMember(4)]
    public int SignalLevel { get; set; }
}

public enum SecurityType
{
    NoSecurity,
    Wpa,
    Wpa2,
    NotSupportedType = 0xFF,
}

/// <summary>
/// Request Object for Requesting a WiFi Connection be Started.
/// Adds Default Timeout Values for Connection, IP Address and Time Sync.
/// </summary>
[ProtoContract]
public class WirelessConnectionRequest
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2)]
    public string Ssid { get; set; }

    [ProtoMember(3)]
    public string PassKey { get; set; }

    [ProtoMember(4)]
    public bool Remember { get; set; }

    [ProtoMember(5)]
    public bool Encrypted { get; set; }

    [ProtoMember(6)]
    public int WaitForConnectionSeconds { get; set; }

    [ProtoMember(7)]
    public int WaitForIpAddressSeconds { get; set; }

    [ProtoMember(8)]
    public int WaitForTimeSyncSeconds { get; set; }

    /// <summary>
    /// Default Constructor
    /// </summary>
    public WirelessConnectionRequest()
    {
        this.WaitForConnectionSeconds = 60;
        this.WaitForIpAddressSeconds = 30;
        this.WaitForTimeSyncSeconds = 30;
    }
}


[ProtoContract]
public class WirelessConnectionNotification
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2)]
    public WirelessConnectionState State { get; set; }

    [ProtoMember(3)]
    public string Ssid { get; set; }
}

[ProtoContract]
public class WirelessConnectionResponse
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2)]
    public WirelessConnectionState State { get; set; }

    [ProtoMember(3)]
    public string Ssid { get; set; }
}

public enum WirelessConnectionState
{
    ClientReady = 0,
    ClientDiscovering = 2,
    ClientIdentifying = 3,
    ClientWaitingForIp = 4,
    ClientWaitingForTime = 5,
    ClientConnecting = 6,
    ClientConnected = 7,
    ClientDisconnecting = 8,
    ClientDisconnected = 9,
    ClientConnectionFailed = 10,
    ClientConnectionSignalChanged = 11,
}

[ProtoContract]
public class ProfileRequest
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2)]
    public bool UseOsProfiles { get; set; }
}

[ProtoContract]
public class TimeSyncRequest
{
    [ProtoMember(1)]
    public int TimeoutSeconds { get; set; }
}

[ProtoContract]
public class TimeSyncResponse
{
    [ProtoMember(1)]
    public bool TimeSyncComplete { get; set; }
}

[ProtoContract]
public class NetworkProfilesList
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2, IsRequired = true)]
    public List<WirelessConnectionRequest> NetworkProfiles { get; set; } = new();
}

[ProtoContract]
public class IPInfo
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2)]
    public string MacAddress { get; set; }

    [ProtoMember(3)]
    public string IpPAddress { get; set; }

    [ProtoMember(4)]
    public string GatewayIp { get; set; }

    [ProtoMember(5, IsRequired = true)]
    public List<string> Dns { get; set; } = new List<string>();
}

