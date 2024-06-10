using Ahsoka.Services.Data;
using Ahsoka.System;
using Ahsoka.Utility;
using System;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591

public class AhsokaServiceAttribute : Attribute
{
    public string ServiceName { get; init; }

    public AhsokaServiceAttribute(string serviceName) { ServiceName = serviceName; }
}

public class AhsokaServiceDataValues : Attribute
{
    public string ServiceName { get; init; }

    public AhsokaServiceDataValues(string serviceName) { ServiceName = serviceName; }
}
public interface IAhsokaServiceEndpoint : IAhsokaEndPoint
{
    int MessageReceiveCount { get; set; }
    int MessageSendCount { get; set; }

    void ServiceDataReady();
}

public abstract class AhsokaServiceBase<TTransportIds, TAhsokaSocket> : AhsokaEndPoint<TTransportIds, TAhsokaSocket>, IAhsokaServiceEndpoint
    where TTransportIds : Enum
    where TAhsokaSocket : AhsokaSocket, new()
{
    #region Fields
    private string publisherId => ServiceConfig.ServiceName;
    #endregion

    #region Methods
    protected AhsokaServiceBase(ServiceConfiguration config, AhsokaMessagesBase messages) :
        base(config, messages)
    {

    }

    protected virtual AhsokaMessageHeader CreateReplyMessageHeader(AhsokaMessageHeader header, TTransportIds transportId)
    {
        return new AhsokaMessageHeader()
        {
            TransportId = transportId.EnumToInt(),
            ClientMessageId = header.ClientMessageId,
            ClientId = header.ClientId,
        };
    }

    protected override Type GetReceiptType(ServiceMessageInfo value)
    {
        // We recieve Requests (Service)
        return value.RequestType;
    }

    void IAhsokaServiceEndpoint.ServiceDataReady()
    {
        // Recieve In Band Data
        if (ReceiveMessage(out AhsokaMessageHeader responseHeader, out object returnObject, false))
        {
            OnHandleReceive(responseHeader, returnObject);
        }
    }

    internal KeyInfo UpdateDataServiceKey(string key, object value)
    {
        return DataService.DefaultDataService?.UpdateKeyValue(this.ServiceConfig.ServiceName, key, value, publisherId);
    }

    #endregion
}

#pragma warning restore CS1591