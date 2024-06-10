using Ahsoka.System;
using Ahsoka.Utility;
using System;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591

public abstract class AhsokaSerialServiceBase<TTransportIds, TAhsokaSocket> : AhsokaServiceBase<TTransportIds, TAhsokaSocket>
    where TTransportIds : Enum
    where TAhsokaSocket : AhsokaSocket, new()
{
    protected AhsokaSerialServiceBase(ServiceConfiguration config, AhsokaMessagesBase messages) :
        base(config, messages)
    {

    }

    protected override AhsokaMessageHeader CreateReplyMessageHeader(AhsokaMessageHeader header, TTransportIds transportId)
    {
        return new AhsokaMessageHeader()
        {
            TransportId = transportId.EnumToInt(),
            ClientMessageId = header.ClientMessageId,
            ClientId = header.ClientId,
            EndpointId = header.EndpointId,
            IsMessageCompressed = header.IsMessageCompressed,
        };
    }
}

#pragma warning restore CS1591
