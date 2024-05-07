using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.IO;

namespace Ahsoka.ServiceFramework;

#pragma warning disable CS1591

public interface IAhsokaEndPoint
{
    ServiceConfiguration ServiceConfig { get; }

    AhsokaRuntime CurrentRuntime { get; set; }

    byte[] GetClientId();

    void Connect();

    void Disconnect();
}

public abstract class AhsokaEndPoint<TTransportIds> : IAhsokaEndPoint
    where TTransportIds : Enum
{
    #region Fields
    byte[] clientId = Guid.NewGuid().ToByteArray();
    readonly AhsokaMessagesBase serviceMessageInfo;
    int messageReceiveCount = 0;
    int messageSendCount = 0;
    #endregion

    #region Properties

    public byte[] GetClientId() { return clientId; }
    public bool IsRunning { get { return Socket == null ? false : Socket.IsConnected; } }
    protected IAhsokaSocket Socket { get; private set; }
    public AhsokaRuntime CurrentRuntime { get; set; }
    public ServiceConfiguration ServiceConfig { get; private set; }
    public int MessageReceiveCount { get => messageReceiveCount; set => messageReceiveCount = value; }
    public int MessageSendCount { get => messageSendCount; set => messageSendCount = value; }
    public AhsokaMessagesBase ServiceMessageInfo => serviceMessageInfo;
    #endregion

    public AhsokaEndPoint(ServiceConfiguration config, AhsokaMessagesBase messages)
    {
        ServiceConfig = config;
        serviceMessageInfo = messages;

        // Windows can only host on the same box due to firewall rules
        if (SystemInfo.CurrentPlatform == PlatformFamily.Windows64)
            config.TcpListenAddress = config.TcpListenAddress.Replace("*", "localhost");
    }

    void IAhsokaEndPoint.Connect()
    {
        Socket = CreateSocket(ServiceConfig);
        Socket.Connect(this);
        OnEndPointConnected();
    }

    void IAhsokaEndPoint.Disconnect()
    {
        Socket.Disconnect();
        OnEndPointDisconnected();
    }

    public void UpdateConfiguration(ServiceConfiguration config)
    {
        bool wasConnected = IsRunning;
        if (wasConnected)
            this.Socket.Disconnect();

        this.ServiceConfig = config;

        if (wasConnected)
            this.Socket.Connect(this);
    }

    protected void SendMessageInternal(AhsokaMessageHeader messageHeader, object messageObject = null, bool sendAsNotification = false)
    {
        if (IsRunning)
        {
            messageObject ??= new EmptyNotification();

            // Prepare Response Message 
            using MemoryStream headerStream = new();
            ProtoBuf.Serializer.Serialize(headerStream, messageHeader);

            using MemoryStream dataStream = new();
            ProtoBuf.Serializer.Serialize(dataStream, messageObject);

            Socket.SendMessage(headerStream.ToArray(), dataStream.ToArray(), messageHeader.ClientId, sendAsNotification);

            messageSendCount++;
        }
        else
        {

        }
    }

    protected bool ReceiveMessage(out AhsokaMessageHeader messageHeader, out object messageObject, out ServiceMessageInfo messageInfo,  bool isOutOfBand, int timoutInMs = int.MaxValue)
    {
        messageObject = null;
        messageHeader = null;
        messageInfo = null;

        bool recievedMessage = Socket.ReceiveMessage(out byte[] headerData, out byte[] messageData, out byte[] clientId, isOutOfBand, timoutInMs);

        if (recievedMessage)
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
            messageInfo = serviceMessageInfo.GetMessageInfo(transportType);

            if (messageInfo != null) // Not much we can do with a mesage we can't decode
                using (MemoryStream objectData = new(messageData))
                    messageObject = ProtoBuf.Serializer.Deserialize(GetReceiptType(messageInfo), objectData);
        }

        return recievedMessage;
    }

    abstract protected IAhsokaSocket CreateSocket(ServiceConfiguration config);

    abstract protected Type GetReceiptType(ServiceMessageInfo value);

    abstract protected void OnHandleReceive(AhsokaMessageHeader messageHeader, object message);

    protected virtual void OnEndPointDisconnected() { }

    protected virtual void OnEndPointConnected() { }

    protected virtual void OnClientConnected() { }
}


#pragma warning restore CS1591