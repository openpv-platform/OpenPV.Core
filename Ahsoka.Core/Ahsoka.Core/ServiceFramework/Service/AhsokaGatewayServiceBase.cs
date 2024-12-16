using Ahsoka.System;
using System;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// Base Gateway Service
/// </summary>
/// <typeparam name="TTransportIds"></typeparam>
public abstract partial class GatewayServiceBase<TTransportIds> : AhsokaServiceBase<TTransportIds>, IAhsokaGatewayEndPoint
    where TTransportIds : Enum, new()
{
    /// <summary>
    /// Gateway Socket
    /// </summary>
    public GatewaySocket GatewaySocket { get; set; } = new();

    /// <summary>
    /// Base Constructor for Ahsoka Service EndPoints
    /// </summary>
    /// <param name="config"></param>
    /// <param name="messages"></param>
    protected GatewayServiceBase(ServiceConfiguration config, AhsokaMessagesBase messages) :
        base(config, messages)
    {

    }

    /// <summary>
    /// Handles / Routes Messages from the Origin to Destination
    /// </summary>
    /// <param name="registeredSocket"></param>
    /// <param name="message"></param>
    void IAhsokaGatewayEndPoint.HandleGatewayMessage(IAhsokaServiceSocket registeredSocket, AhsokaServiceMessage message)
    {
        OnHandleGatewayMessage(registeredSocket, message);
    }

    /// <summary>
    /// Add Gateway Socket to Service
    /// </summary>
    protected override void OnEndPointStarting()
    {
        ((IAhsokaGatewayEndPoint)this).AddLocalSocket(GatewaySocket);
        base.OnEndPointStarting();
    }

    /// <summary>
    /// Raised when a Clients Connects to the Service
    /// </summary>
    /// <param name="registeredSocket"></param>
    /// <param name="message"></param>
    protected virtual void OnHandleGatewayMessage(IAhsokaServiceSocket registeredSocket, AhsokaServiceMessage message) 
    { 
        registeredSocket.SendMessage(message);
    }
}

