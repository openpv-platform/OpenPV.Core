using ProtoBuf;
using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.Network;

[ProtoContract]
public class WiFiAPMessageTypes
{
    public enum Ids
    {
        None = 0,
        GetAdapters = 1,
        StartAccessPoint = 2,
        StopAccessPoint = 3,
        GetNetworkConfiguration = 6,
        SetNetworkConfiguration = 8,
        GetClientList = 9,
        ClientAdded = 10,
        ClientRemoved = 11,
    }
}

[ProtoContract]
public class AccessPointServiceConfiguration
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2)]
    public string Description { get; set; }

    [ProtoMember(3)]
    public AccessPointConfiguration AccessPointConfiguration { get; set; }

    [ProtoMember(4)]
    public DHCPServerConfiguration DhcpServerConfiguration { get; set; }

    [ProtoMember(5)]
    public string OverrideMacAddress { get; set; }

    [ProtoMember(6)]
    public bool SaveAsDefault { get; set; }
}

[ProtoContract]
public class AccessPointConfiguration
{
    [ProtoMember(1)]
    public string Ssid { get; set; }

    [ProtoMember(2)]
    public SecurityType SecurityType { get; set; }

    [ProtoMember(3)]
    public string EncryptionPassKey { get; set; }

    [ProtoMember(4)]
    public bool BroadcastSsid { get; set; }

    [ProtoMember(5)]
    public int ChannelNumber { get; set; }

    [ProtoMember(6)]
    public DHCPServerConfiguration DHCPServerConfiguration { get; set; }
}

[ProtoContract]
public class DHCPServerConfiguration
{
    [ProtoMember(1)]
    public string DhcpIpBaseAddress { get; set; }

    [ProtoMember(2)]
    public int DhcpBaseOffset { get; set; }

    [ProtoMember(3)]
    public int DhcpBasePoolSize { get; set; }

    [ProtoMember(4)]
    public string Dns { get; set; }
}

[ProtoContract]
public class AccessPointClientList
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2)]
    public int ClientCount { get; set; }

    [ProtoMember(3, IsRequired = true)]
    public List<AccessPointClient> Clients { get; set; } = new();
}

[ProtoContract]
public class AccessPointClient
{
    [ProtoMember(1)]
    public string MacAddress { get; set; }

    [ProtoMember(2)]
    public int Rssi { get; set; }
}