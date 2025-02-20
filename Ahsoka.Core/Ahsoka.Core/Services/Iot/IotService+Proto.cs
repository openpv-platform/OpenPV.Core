using ProtoBuf;
using System.Text;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.Iot;

[ProtoContract]
public class IotMessageTypes
{
    public enum Ids
    {
        None = 0,
        CreateClient = 1,
        DestroyClient = 3,
        SubscribeTopic = 4,
        UnsubscribeTopic = 5,
        PublishTopic = 6,
        TopicReceivedNotification = 7,
    }
}

[ProtoContract]
public class ClientInfo
{
    [ProtoMember(1)]
    public string BrokerAddress { get; set; }

    [ProtoMember(2)]
    public int BrokerPort { get; set; }

    [ProtoMember(3)]
    public string DeviceUserName { get; set; }

    [ProtoMember(4)]
    public string DevicePassword { get; set; }

    [ProtoMember(5)]
    public bool UseCustomTlsCert { get; set; }

    [ProtoMember(6)]
    public string CustomTlsPublicKeyPath { get; set; }

    [ProtoMember(7)]
    public string CustomTlsCertPrivateKeyPath { get; set; }

    [ProtoMember(8)]
    public string CustomTlsRootCAPath { get; set; }
}

[ProtoContract]
public class ClientMessage
{
    [ProtoMember(1)]
    public string ClientId { get; set; }
}

[ProtoContract]
public class ClientConnectionInfo
{
    [ProtoMember(1)]
    public string ClientId { get; set; }

    [ProtoMember(2)]
    public ClientState ClientState { get; set; }

    [ProtoMember(3)]
    public string BrokerAddress { get; set; }

    [ProtoMember(4)]
    public int BrokerPort { get; set; }

    [ProtoMember(5)]
    public string ResultText { get; set; }
}

public enum ClientState
{
    NoState = 0,
    Connected = 1,
    TopicSubscribed = 2,
    TopicUnsubscribed = 3,
    Disconnected = 4,
    ConnectionError = 5,
}

[ProtoContract]
public class TopicSubscription
{
    [ProtoMember(1)]
    public string ClientId { get; set; }

    [ProtoMember(2)]
    public string TopicName { get; set; }

    [ProtoMember(3)]
    public TopicQualityOfService QualityOfService { get; set; }
}

[ProtoContract]
public class TopicResult
{
    [ProtoMember(1)]
    public string ClientId { get; set; }

    [ProtoMember(2)]
    public string ResultText { get; set; }

    [ProtoMember(3)]
    public TopicResultStatus Status { get; set; }
}

public enum TopicQualityOfService
{
    AtMostOnce = 0,
    AtLeastOnce = 1,
    ExactlyOnce = 2,
}

[ProtoContract]
public class TopicMessage
{
    [ProtoMember(1)]
    public string ClientId { get; set; }

    [ProtoMember(2)]
    public string TopicName { get; set; }

    [ProtoMember(4)]
    public byte[] Payload { get; set; }

    /// <summary>
    /// Payload as a String
    /// </summary>
    public string PayloadString
    {
        get { return UTF8Encoding.UTF8.GetString(Payload); }
        set { Payload = UTF32Encoding.UTF8.GetBytes(value); }
    }
}


public enum TopicResultStatus
{
    Success = 0,
    Error = 1,
}
