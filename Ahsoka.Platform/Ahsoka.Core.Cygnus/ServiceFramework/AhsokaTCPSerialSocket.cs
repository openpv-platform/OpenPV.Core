using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;

#pragma warning disable CS1591

namespace Ahsoka.ServiceFramework;

#region Client Serial Socket
public sealed class TCPClientSerialSocket : AhsokaSerialSocket
{
    DealerSocket Socket { get; set; }


    protected override void OnConnect(IAhsokaEndPoint endPoint)
    {
        try
        {
            this.EndPoint = endPoint;
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Starting Client {EndPoint.ServiceConfig.ServiceName} @ {EndPoint.ServiceConfig.DataURI}");


            byte[] clientId = Guid.NewGuid().ToByteArray();

            Socket = new DealerSocket();
            Socket.Options.Linger = TimeSpan.FromSeconds(1);
            Socket.Options.Identity = clientId;

            Socket.Connect(EndPoint.ServiceConfig.DataURI);

            IsConnected = true;
        }
        catch { IsConnected = false; }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Starting Client {EndPoint.ServiceConfig.ServiceName} @ {EndPoint.ServiceConfig.DataURI} with Result {IsConnected}");
    }

    protected override void OnDisconnect()
    {
        if (IsConnected)
        {
            try
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Disconnecting {EndPoint.ServiceConfig.ServiceName}");

                Socket.Disconnect(EndPoint.ServiceConfig.DataURI);

                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Disconnected {EndPoint.ServiceConfig.ServiceName}");
            }
            catch { }
            finally { IsConnected = false; }
        }
    }

    protected override bool TransmitOverInterface(byte[] packetData, byte[] clientId)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Client Message from {EndPoint.ServiceConfig.ServiceName}");

        NetMQSocket socket = Socket;

        NetMQMessage message = new();
        if (clientId != null)
            message.Append(clientId);
        message.Append(packetData);

        lock (socket)
        {
            socket.SendMultipartMessage(message);
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Sent Client Message Completed from {EndPoint.ServiceConfig.ServiceName} ");

        return true;
    }

    protected override bool ReceiveOverInterface(out byte[] packetData, out byte[] clientId, int timeOutInMs = int.MaxValue)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Client Message on {EndPoint.ServiceConfig.ServiceName}");

        // Select Socket to Recieve
        NetMQSocket socket = Socket;

        NetMQMessage message = new();
        if (socket.TryReceiveMultipartMessage(ref message, 1))
        {
            byte[] newData = message.Pop().Buffer;
            packetData = newData;
            clientId = null;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Received Client Message on {EndPoint.ServiceConfig.ServiceName} ");
            return true;
        }
        else
        {
            packetData = clientId = null;
            return false;
        }
    }
}
#endregion

#region Service Serial Socket
public sealed class TCPServiceSerialSocket : AhsokaSerialSocket
{
    NetMQPoller SocketPoller { get; set; }
    RouterSocket Socket { get; set; }
    NetMQMonitor Monitor { get; set; }

    protected override void OnConnect(IAhsokaEndPoint endPoint)
    {
        try
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Starting Service {EndPoint.ServiceConfig.ServiceName} @ {EndPoint.ServiceConfig.DataListenURI}");
            SocketPoller = new NetMQPoller();
            Socket = new RouterSocket();
            Socket.Options.Linger = TimeSpan.FromSeconds(1);
            Socket.Bind(EndPoint.ServiceConfig.DataListenURI);
            SocketPoller.Add(Socket);

            Monitor = new NetMQMonitor(Socket, $"inproc://{Guid.NewGuid().ToString()}.inproc", SocketEvents.All);
            Monitor.AttachToPoller(SocketPoller);

            IsConnected = true;

            Socket.ReceiveReady += (o, e) =>
            {
                ((IAhsokaServiceEndpoint)EndPoint).ServiceDataReady();
            };

            Monitor.Accepted += (o, e) =>
            {
                ((IAhsokaServiceEndpoint)EndPoint).ClientConnected();
            };
          
            SocketPoller.RunAsync($"{EndPoint.ServiceConfig.ServiceName}_Handler");

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Starting Service {EndPoint.ServiceConfig.ServiceName} @ {EndPoint.ServiceConfig.DataListenURI} with Result {IsConnected}");
        }
        catch { IsConnected = false; }
    }

    protected override void OnDisconnect()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Disconnecting {EndPoint.ServiceConfig.ServiceName}");

        if (IsConnected)
        {
            Monitor.DetachFromPoller();
            Socket.Unbind(EndPoint.ServiceConfig.DataListenURI);
        }

        IsConnected = false;

        SocketPoller.Stop();
        SocketPoller.RemoveAndDispose(Socket);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Disconnected {EndPoint.ServiceConfig.ServiceName}");

    }

    protected override bool TransmitOverInterface(byte[] packetData, byte[] clientId)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Service Message on {EndPoint.ServiceConfig.ServiceName} ");

        NetMQMessage message = new();

        message.Append(clientId);
        message.Append(packetData);

        lock (Socket) // Protect Socket
        {
            Socket.SendMultipartMessage(message);
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Finished Sending Service Message on {EndPoint.ServiceConfig.ServiceName} ");

        return true;
    }

    protected override bool ReceiveOverInterface(out byte[] packetData, out byte[] clientId, int timeOutInMs = int.MaxValue)
    {
        NetMQMessage message = new();
        if (Socket.TryReceiveMultipartMessage(ref message, 2))
        {
            clientId = message.Pop().Buffer;
            packetData = message.Pop().Buffer;

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Recieved Service Message on {EndPoint.ServiceConfig.ServiceName} ");
            return true;
        }

        packetData = clientId = null;
        return false;
    }
}
#endregion

#pragma warning restore CS1591