using ProtoBuf;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.Gateway;

[ProtoContract]
public class GatewayMessageTypes
{
    public enum Ids
    {
        None = 0,
    }
}
