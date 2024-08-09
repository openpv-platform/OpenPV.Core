using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591

public interface IAhsokaEndPoint
{
    ServiceConfiguration ServiceConfig { get; }

    AhsokaRuntime CurrentRuntime { get; set; }

    void Connect();

    void Disconnect();

    void ReceiveMessage(bool isNotification);

    void WaitForDisconnect();

    WaitHandle GetWaitHandle();
}

public abstract class AhsokaEndPoint<TTransportIds> : IAhsokaEndPoint
    where TTransportIds : Enum
{
    #region Fields
    readonly AhsokaMessagesBase serviceMessageInfo;
    int messageReceiveCount = 0;
    int messageSendCount = 0;
    #endregion

    #region Properties

    public bool IsConnected { get { return Socket == null ? false : Socket.IsConnected; } }
    protected IAhsokaSocket Socket { get; private set; }
    AhsokaRuntime IAhsokaEndPoint.CurrentRuntime { get; set; }
    public ServiceConfiguration ServiceConfig { get; private set; }
    public int MessageReceiveCount { get => messageReceiveCount; set => messageReceiveCount = value; }
    public int MessageSendCount { get => messageSendCount; set => messageSendCount = value; }
    public AhsokaMessagesBase ServiceMessageInfo => serviceMessageInfo;
    readonly ManualResetEventSlim shutdownWaiter = new(false);
    #endregion

    public AhsokaEndPoint(ServiceConfiguration config, AhsokaMessagesBase messages)
    {
        ServiceConfig = config;
        serviceMessageInfo = messages;

        // Windows can only host on the same box due to firewall rules
        if (SystemInfo.CurrentPlatform == PlatformFamily.Windows64)
            config.TcpListenAddress = config.TcpListenAddress.Replace("*", "localhost");
    }

    public void Connect()
    {
        // Before Connecting
        OnEndPointConnecting();

        Socket = CreateSocket(ServiceConfig);
        Socket.Connect(this);
        ((IAhsokaEndPoint)this).CurrentRuntime = AhsokaRuntime.Default;
        AhsokaRuntime.Default.AddEndPointInternal(this);

        // After Connected
        OnEndPointConnected();
    }

    public void Disconnect()
    {
        try
        {
            Socket.Disconnect();
            AhsokaRuntime.Default.RemoveEndPointInternal(this);
            OnEndPointDisconnected();
        }
        catch { }
        finally
        {
            shutdownWaiter.Set();
        }
    }

    /// <summary>
    /// Allows the consumer to wait this endpoint to Disconnect / ShutDown
    /// </summary>
    public void WaitForDisconnect()
    {
        shutdownWaiter.Wait();
    }

    /// <summary>
    /// Allows the consumer to wait this endpoint to Disconnect / ShutDown
    /// </summary>
    public WaitHandle GetWaitHandle()
    {
        return shutdownWaiter.WaitHandle;
    }

    public void UpdateConfiguration(ServiceConfiguration config)
    {
        bool wasConnected = IsConnected;
        if (wasConnected)
            this.Socket.Disconnect();

        this.ServiceConfig = config;

        if (wasConnected)
            this.Socket.Connect(this);
    }

    protected void SendMessageInternal(AhsokaMessageHeader messageHeader, object messageObject = null, bool isBroadcast = false)
    {
        if (IsConnected)
        {
            messageObject ??= new EmptyNotification();

            // Prepare Response Message 
            using MemoryStream headerStream = new();
            ProtoBuf.Serializer.Serialize(headerStream, messageHeader);

            using MemoryStream dataStream = new();
            ProtoBuf.Serializer.Serialize(dataStream, messageObject);
            
            Socket.SendMessage(headerStream.ToArray(), dataStream.ToArray(), messageHeader.ClientId, isBroadcast);

            messageSendCount++;
        }
        else
        {

        }
    }

    void IAhsokaEndPoint.ReceiveMessage(bool isNotification)
    {
        AhsokaMessageHeader messageHeader = null;
        object messageObject = null;

        bool receivedMessage = Socket.ReceiveMessage(out byte[] headerData, out byte[] messageData, out byte[] clientId, isNotification);

        if (receivedMessage)
        {
            messageReceiveCount++;

            // Decode Request Header
            using (MemoryStream header = new(headerData))
            {
                messageHeader = ProtoBuf.Serializer.Deserialize<AhsokaMessageHeader>(header);
                if (clientId != null)
                    messageHeader.ClientId = clientId;
            }

            // Lookup and Decode Message
            TTransportIds transportType = messageHeader.TransportId.IntToEnum<TTransportIds>();
            var messageInfo = serviceMessageInfo.GetMessageInfo(transportType);

            if (messageInfo != null) // Not much we can do with a mesage we can't decode
                using (MemoryStream objectData = new(messageData))
                    messageObject = ProtoBuf.Serializer.Deserialize(GetReceiptType(messageInfo), objectData);

            OnReceiveMessage(messageHeader, messageObject, messageInfo);
        }
    }

    abstract protected IAhsokaSocket CreateSocket(ServiceConfiguration config);

    abstract protected Type GetReceiptType(ServiceMessageInfo value);

    abstract protected void OnReceiveMessage(AhsokaMessageHeader messageHeader, object message, ServiceMessageInfo info);

    abstract protected void OnHandleReceive(AhsokaMessageHeader messageHeader, object message);

    protected virtual void OnEndPointConnecting() { }
    
    protected virtual void OnEndPointDisconnected() { }

    protected virtual void OnEndPointConnected() { }

    protected virtual void OnClientConnected() { }
}


#pragma warning restore CS1591