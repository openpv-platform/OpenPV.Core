using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Cygnus.Reach;

namespace Ahsoka.Services.Cygnus;

/// <summary>
/// The interface that deines one of the outgoing endpoints that communicate with the cygnus clients.
/// </summary>
public interface ICygnusOutgoingEndpoints
{
    /// <summary>
    /// The function that is going to send a notification to any clients connected to the endpoint.
    /// </summary>
    public void SendNotification(AhsokaMessageHeader messageHeader, object messageObject);
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TAhsokaSocket"></typeparam>
public class CygnusServerInterfaceService<TAhsokaSocket> : AhsokaSerialServiceBase<ReachMessageTypes> , ICygnusOutgoingEndpoints
    where TAhsokaSocket : IAhsokaSocket, new()
{
    #region Fields
    /// <summary>
    /// Configuration Name for the Can Service
    /// </summary>
    public const string Name = "CygnusServerInterfaceService";
    private CygnusInterface InterfaceType;
    #endregion

    /// <summary>
    /// Can Service Constructor accepting a customer Service Configuration
    /// </summary>
    /// <param name="interfaceType"></param>
    public CygnusServerInterfaceService(CygnusInterface interfaceType) :
        base(ConfigurationLoader.GetServiceConfig(Name), new CygnusServerServiceMessages())
    {
        InterfaceType = interfaceType;
    }

    void ICygnusOutgoingEndpoints.SendNotification(AhsokaMessageHeader messageHeader, object messageObject)
    {
        SendMessageInternal(messageHeader, messageObject);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="messageHeader"></param>
    /// <param name="message"></param>
    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        // NOTE: BMG (Jan. 26, 2024) All messages are going to be handled by the Cygnus server singleton, this is just a
        // way of passing through the requests to it for each of the individual outward facing interfaces.
        SendMessageInternal(messageHeader, CygnusServerService.Default.Request(InterfaceType, this, messageHeader, message));
    }
}
