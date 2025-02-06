using ProtoBuf;

namespace Ahsoka.Core;

///----------------------------
/// Service Routing Message Header
/// This object represents the Layer 2 Message Format for OpenPV Service Messages.
///----------------------------
[ProtoContract]
public class AhsokaMessageHeader
{
    /// This ID defines the Type of Message being carried in the Envelop / Header
    /// Note: Existing OpenPV / Ahsoka Message Architecture 
    [ProtoMember(1)]
    public int TransportId { get; set; }

    /// (Optional) This ID defines a unique Message / Response used when out of order messages are needed
    /// usually left empty since OpenPV Endpoints gaurantee message order
    /// Note: Existing OpenPV / Ahsoka Message Architecture 
    [ProtoMember(2)]
    public int ClientMessageId { get; set; }

    /// (Optional) Unique ID for a Client used in Services that support Multiple Clients (usually autopopulated with GUID)
    /// Note: Part of Existing OpenPV / Ahsoka Message Architecture 
    [ProtoMember(3)]
    public byte[] ClientId { get; set; }

    /// (Optional) The size of the message payload that follows this header 
    [ProtoMember(4)]
    public int MessageSize { get; set; }

    /// (Optional) Routing for Non-Endpoint Style Transports. 
    /// Note: Endpoint 0 is Reserved for Service Discovery for Non-Endpoint Transports
    /// Note: New for OpenPV / Ahsoka Message Architecture 
    [ProtoMember(5)]
    public uint EndpointId { get; set; }

    /// (Optional) Indicates that the message has used deflate compression in addition to pbuff encoding
    /// Note: New for OpenPV / Ahsoka Message Architecture 
    [ProtoMember(6)]
    public bool IsMessageCompressed { get; set; }

    /// (Optional) Security Token used to protect service from unauthorized calls
    /// Tokens are created by the Security Service using the UnlockEndPoint() method
    [ProtoMember(7)]
    public byte[] ServiceToken { get; set; }
}

///----------------------------
/// Empty Object / Message used when no information is needed 
/// other than the TransportID
///----------------------------
[ProtoContract]
public class EmptyNotification
{

}

///----------------------------
/// Empty Object / Message used when no information is needed 
/// other than the TransportID
///----------------------------
[ProtoContract]
public class InvalidResponseNotification
{

}

/// ----------------------------
/// Empty Object / Message used when no information is needed 
/// other than the TransportID
///----------------------------
[ProtoContract]
public class ClientRegistration
{
    /// <summary>
    /// Connection Registration message for Client
    /// </summary>
    [ProtoMember(1)]
    public byte[] ClientId { get; set; }
}

/// <summary>
/// Internal use TransportIds
/// </summary>
public enum CoreTransportIds
{
    /// <summary>
    /// None
    /// </summary>
    None = 0,

    /// <summary>
    /// Invalid Token sent to Secure Service
    /// </summary>
    InvalidToken = -1,

    /// <summary>
    /// Register a Client for Notification
    /// </summary>
    RegisterClient = -2,

    /// <summary>
    /// Deregister a Client
    /// </summary>
    DeregisterClient = -3,

    /// <summary>
    /// Error Thrown in Service 
    /// </summary>
    ServiceErrorThrown = -4,
}

/// <summary>
/// Message Returned from errors in Service Layer
/// </summary>
[ProtoContract]
public class ServiceErrorThrownResponse
{
    /// <summary>
    /// Service which threw this error
    /// </summary>
    [ProtoMember(2)]
    public string ServiceName { get; set; }

    /// <summary>
    /// Error Info
    /// </summary>
    [ProtoMember(3)]
    public string ErrorInformation { get; set; }
}