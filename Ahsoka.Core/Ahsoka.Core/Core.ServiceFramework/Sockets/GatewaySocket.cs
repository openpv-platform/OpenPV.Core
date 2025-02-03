using System.Collections.Generic;

namespace Ahsoka.Core;

/// <summary>
/// Gateway Socket
/// </summary>
public class GatewaySocket : IAhsokaServiceSocket
{
    IAhsokaGatewayEndPoint gatewayOrigin;
    Dictionary<uint, KeyValuePair<IAhsokaServiceEndPoint, IAhsokaServiceSocket>> endpoints = new();

    /// <summary>
    /// Gateway Socket
    /// </summary>
    public GatewaySocket()
    {

    }

    /// <summary>
    /// Routes Messages from Origin Service to EndPoint
    /// </summary>
    /// <param name="originSocket"></param>
    /// <param name="message"></param>
    public void RouteMessageToEndPoint(IAhsokaServiceSocket originSocket, AhsokaServiceMessage message)
    {
        // Find the Client that we can Route Too.
        if (!endpoints.TryGetValue(message.Header.EndpointId, out var socketInfo))
        {
            var endPoint = AhsokaRuntime.Default.GetServiceByEndPointId(message.Header.EndpointId);
            if (endPoint != null)
            {
                endPoint.AddLocalSocket(this);
                socketInfo = endpoints[message.Header.EndpointId] = new(endPoint, originSocket);
            }
        }

        if (socketInfo.Key == null)
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Gateway Service Received a messsage for EndPoint {message.Header.EndpointId} but no IGatewayEndPoint was found to Service this Message.");
        else
            socketInfo.Key.HandleServiceRequest(this, message);
    }


    void IAhsokaServiceSocket.Connect(IAhsokaServiceEndPoint endPoint)
    {
        gatewayOrigin = endPoint as IAhsokaGatewayEndPoint;
    }

    void IAhsokaServiceSocket.Disconnect()
    {
        endpoints.Clear();
        gatewayOrigin = null;
    }

    void IAhsokaServiceSocket.SendMessage(AhsokaServiceMessage message)
    {
        if (endpoints.TryGetValue(message.Header.EndpointId, out var socketInfo))
            gatewayOrigin.HandleGatewayMessage(socketInfo.Value, message);
        else
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Gateway Service Received a messsage from EndPoint {message.Header.EndpointId} but no IGatewayEndPoint was found to Service this Message.");
    }
}