using ProtoBuf;
using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.Security;

[ProtoContract]
public class SecurityMessageTypes
{
    public enum Ids
    {
        None = 0,
        RequestToken = 2,
        ChallengeReceived = 4,
        CompleteChallenge = 5,
        AuthorizationComplete = 6,
    }
}

// Used for Cert based Auth (Client has Public Key)
[ProtoContract]
public class TokenRequestInfo
{
    [ProtoMember(1)]
    public string ApiKey { get; set; }

    [ProtoMember(2, IsRequired = true)]
    public List<byte[]> ClientIds { get; set; } = new List<byte[]>();

    [ProtoMember(3)]
    public string ClientDescription { get; set; }

    [ProtoMember(4)]
    public string ClientCode { get; set; }
}

[ProtoContract]
public class TokenRequestResponse
{
    [ProtoMember(1)]
    public byte[] RequestId { get; set; }
}

// Used for Local Auth Operator Must Accept Challenge
[ProtoContract]
public class ChallengeRequestInfo
{
    [ProtoMember(1)]
    public byte[] RequestId { get; set; }

    [ProtoMember(2)]
    public string ClientCode { get; set; }

    [ProtoMember(3)]
    public string ClientDescription { get; set; }
}

// Sent by UI when CHallenge is Completed
[ProtoContract]
public class ChallengeCompleteInfo
{
    [ProtoMember(1)]
    public byte[] RequestId { get; set; }

    [ProtoMember(2)]
    public string ClientCode { get; set; }

    [ProtoMember(3)]
    public bool AcceptRequest { get; set; }

    [ProtoMember(4)]
    public byte[] ClientIdSigned { get; set; }

}

[ProtoContract]
public class Token
{
    [ProtoMember(1)]
    public byte[] RequestId { get; set; }

    [ProtoMember(2)]
    public byte[] ClientToken { get; set; }

    [ProtoMember(3)]
    public bool TokenIsValid { get; set; }
}
