namespace Ahsoka.ServiceFramework;

/// <summary>
/// Endpoint with the ability to handle Routing Messages to Other EndPoints
/// </summary>
public interface IAhsokaGatewayEndPoint : IAhsokaServiceEndPoint
{
    /// <summary>
    /// Routes Messages from Gateway Socket back to Originating Service / Socket
    /// </summary>
    /// <param name="registeredSocket"></param>
    /// <param name="message"></param>
    void HandleGatewayMessage(IAhsokaServiceSocket registeredSocket, AhsokaServiceMessage message);
}

