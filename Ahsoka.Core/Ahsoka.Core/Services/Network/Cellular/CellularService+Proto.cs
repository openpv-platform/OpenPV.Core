using ProtoBuf;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.Network;

[ProtoContract]
public class CellularMessageTypes
{
    public enum Ids
    {
        None = 0,
        GetAdapters = 1,
        StartNetwork = 2,
        StopNetwork = 3,
        GetNetworkInformation = 6,

        NetworkStateChanged = 8,
    }
}


[ProtoContract]
public class CellularServiceConfiguration
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2)]
    public string Description { get; set; }

    [ProtoMember(3)]
    public string Apn { get; set; }

    [ProtoMember(4)]
    public string ApnUser { get; set; }

    [ProtoMember(5)]
    public string ApnPassword { get; set; }

    [ProtoMember(6)]
    public int WaitForTimeSyncSeconds { get; set; }
}


[ProtoContract]
public class CellularConnectionResult
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2)]
    public bool ConnectionStarted { get; set; }

    [ProtoMember(3)]
    public string Description { get; set; }
}


[ProtoContract]
public class CellularNetworkInformation
{
    [ProtoMember(1)]
    public string AdapterId { get; set; }

    [ProtoMember(2)]
    public CellularNetworkState ConnectionState { get; set; }

    [ProtoMember(3)]
    public string ServiceName { get; set; }

    [ProtoMember(4)]
    public string NetworkOperatorName { get; set; }

    [ProtoMember(5)]
    public string Rssi { get; set; }

    [ProtoMember(6)]
    public string Rsrq { get; set; }

    [ProtoMember(7)]
    public string Rsrp { get; set; }

    [ProtoMember(8)]
    public string Snr { get; set; }

    [ProtoMember(9)]
    public IPInfo IpInfo { get; set; }
}

public enum CellularNetworkState
{
    NotConnected = 0,
    ConnectionInProgress = 1,
    Connected = 2,
}