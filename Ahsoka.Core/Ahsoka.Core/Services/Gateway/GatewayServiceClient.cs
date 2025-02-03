using Ahsoka.Core;
using Ahsoka.Core.Utility;
using System.Security;

namespace Ahsoka.Services.Gateway;

/// <summary>
/// Client used to talk to Any Service via Impersonation
/// </summary>
public class GatewayServiceClient : AhsokaClientBase<GatewayMessageTypes.Ids>
{
    uint endpointId;

    /// <summary>
    /// Default Constructor which uses Default Service Configuration
    /// </summary>
    /// <param name="impersonateServiceName"></param>
    public GatewayServiceClient(string impersonateServiceName) :
        this(impersonateServiceName, ConfigurationLoader.GetServiceConfig(GatewayService.Name))
    {

    }

    /// <summary>
    /// Constructor used to specify a custom Service Configuration
    /// </summary>
    /// <param name="impersonateServiceName"></param>
    /// <param name="config"></param>
    public GatewayServiceClient(string impersonateServiceName, ServiceConfiguration config) :
        base(config, new GatewayServiceMessages())
    {
        endpointId = (uint)HashCode32.GetHashCode32(impersonateServiceName);
    }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndPoint OnCreateDefaultService()
    {
        return new GatewayService(this.ServiceConfig);
    }

    /// <summary>
    /// Creates an EndPoint Message with EndPointID Set for Routing on Service Side.
    /// </summary>
    /// <param name="transportId"></param>
    /// <param name="requestObject"></param>
    /// <returns></returns>
    protected override AhsokaClientMessage CreateSocketMessage(GatewayMessageTypes.Ids transportId, object requestObject)
    {
        var endPointMessage = base.CreateSocketMessage(transportId, requestObject);
        endPointMessage.Header.EndpointId = endpointId;
        return endPointMessage;
    }


    /// <summary>
    /// Send an Non-Acked MEssage
    /// </summary>
    /// <param name="transportId"></param>
    /// <param name="messageObject"></param>
    public void SendMessage(int transportId, object messageObject = null)
    {
        SendMessage((GatewayMessageTypes.Ids)transportId, messageObject);
    }

    /// <summary>
    /// Sends a message and waits for an Empty Response
    /// </summary>
    /// <param name="transportId"></param>
    /// <param name="requestObject"></param>
    /// <param name="timeoutInMs"></param>
    public void SendMessageWithResponse(int transportId, object requestObject = null, int timeoutInMs = int.MaxValue)
    {
        SendMessageWithResponse<EmptyNotification>((GatewayMessageTypes.Ids)transportId, requestObject, timeoutInMs);
    }

    /// <summary>
    /// Sends a message and returns a Typed Response
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="transportId"></param>
    /// <param name="requestObject"></param>
    /// <param name="timeoutInMs"></param>
    /// <returns></returns>
    /// <exception cref="SecurityException"></exception>
    public T SendMessageWithResponse<T>(int transportId, object requestObject = null, int timeoutInMs = int.MaxValue) where T : class
    {
        return SendMessageWithResponse<T>((GatewayMessageTypes.Ids)transportId, requestObject, timeoutInMs);
    }
}
