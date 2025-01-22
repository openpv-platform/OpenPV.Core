using Ahsoka.Services.Security;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// Base Template for Ahsoka Service EndPoints
/// </summary>
/// <typeparam name="TTransportIds"></typeparam>
public abstract partial class AhsokaServiceBase<TTransportIds> : AhsokaEndPoint<TTransportIds>, IAhsokaServiceEndPoint
    where TTransportIds : Enum, new()
{
    #region Fields
    object syncRoot = new object();
    private string publisherId => ServiceConfig.ServiceName;
    int messageReceiveCount = 0;
    int messageSendCount = 0;
    bool isServiceStarted = false;

    // Socket Management
    List<IAhsokaServiceSocket> handledSockets;

    Dictionary<Guid, Guid> clientIdToNotificationIds = [];
    Dictionary<Guid, IAhsokaServiceSocket> validNotificationIds = [];

    #endregion

    #region Service Request Object
    /// <summary>
    /// Message Definition for Ahsoka Endpoints which carries a Header and Message Object
    /// </summary>
    public class AhsokaServiceRequest
    {
        /// <summary>
        /// Message Header
        /// </summary>
        internal IAhsokaServiceSocket Socket { get; set; }

        /// <summary>
        /// Message Header
        /// </summary>
        internal AhsokaMessageHeader Header { get; set; }

        /// <summary>
        /// Message Header
        /// </summary>
        public TTransportIds TransportId { get { return Header.TransportId.IntToEnum<TTransportIds>(); } }

        /// <summary>
        /// Message Object - This value is Decoded by Ahsoka EndPoints
        /// </summary>
        public Object Message { get; internal set; }
    }
    #endregion

    #region Properties
    int IAhsokaServiceEndPoint.MessageReceiveCount { get => messageReceiveCount; }

    int IAhsokaServiceEndPoint.MessageSendCount { get => messageSendCount; }

    /// <summary>
    /// Indicates of the Service is currently Listening
    /// </summary>
    public override bool IsStarted { get { return isServiceStarted; } }

    #endregion

    #region Methods
    /// <summary>
    /// Base Constructor for Ahsoka Service EndPoints
    /// </summary>
    /// <param name="config"></param>
    /// <param name="messages"></param>
    protected AhsokaServiceBase(ServiceConfiguration config, AhsokaMessagesBase messages) :
        base(config, messages)
    {

    }

    /// <summary>
    /// Default Start for Ahsoka Service Endpoints
    /// </summary>
    public override void Start()
    {
        Start(null);
    }

    /// <summary>
    /// Custom Startup for Services allowing Multiple Sockets to be Handles in a single EndPoint
    /// </summary>
    /// <param name="sockets"></param>
    public void Start(params IAhsokaServiceSocket[] sockets)
    {
        if (!isServiceStarted)
        {
            if (sockets == null || sockets.Length == 0)
                sockets = [((IAhsokaServiceEndPoint)this).CreateDefaultSocket(ServiceConfig)];

            handledSockets = sockets.ToList();

            // Raise Before Connecting
            OnEndPointStarting();

            foreach (var item in handledSockets)
                item.Connect(this);

            // Add Self to Runtime
            ((IAhsokaEndPoint)this).CurrentRuntime = AhsokaRuntime.Default;
            AhsokaRuntime.Default.AddEndPointInternal(this);

            // After Connected
            OnEndPointStarted(); // Adds Runtime

            isServiceStarted = true;
        }
    }

    /// <summary>
    /// Add a Local Socket to this Service
    /// </summary>
    /// <param name="socket"></param>
    void IAhsokaServiceEndPoint.AddLocalSocket(IAhsokaServiceSocket socket)
    {
        if (!(socket is LocalSocket || socket is GatewaySocket))
            throw new ArgumentException("Socket must be of type 'LocalSocket' or 'GatewaySocket'");

        lock (syncRoot)
            handledSockets.Add(socket);
    }

    /// <summary>
    /// Remove a Local Socket to this Service
    /// </summary>
    /// <param name="socket"></param>
    void IAhsokaServiceEndPoint.RemoveLocalSocket(IAhsokaServiceSocket socket)
    {
        if (!(socket is LocalSocket || socket is GatewaySocket))
            throw new ArgumentException("Socket must be of type 'LocalSocket' or 'GatewaySocket'");

        lock (syncRoot)
            handledSockets.Remove(socket);
    }

    /// <summary>
    /// Stops the EndPoint and all related Sockets.
    /// </summary>
    public override void Stop()
    {
        if (isServiceStarted)
        {
            // Don't operate on Collection in case Disconnect Removes a Socket
            foreach (var item in handledSockets.ToArray())
                item.Disconnect();

            AhsokaRuntime.Default.RemoveEndPointInternal(this);

            OnEndPointStopped(); // Removes Runtime

            isServiceStarted = false;
        }
    }

    /// <summary>
    /// Sends a Response Message for a given Service Request
    /// </summary>
    /// <param name="message"></param>
    /// <param name="replyObject"></param>
    public void SendResponse(AhsokaServiceRequest message, Object replyObject = null)
    {
        messageSendCount++;

        if (replyObject == null)
            replyObject = new EmptyNotification();

        // Prepare Response Message 
        var messageToSend = CreateSocketMessage(message.Header, replyObject, false);

        // Send to Specific Client Or All Cients if Broadcast
        message.Socket.SendMessage(messageToSend);
    }

    /// <summary>
    /// Sends an Async Notification to a Client or All Clients
    /// </summary>
    /// <param name="transportId"></param>
    /// <param name="message"></param>
    /// <param name="specificClient"></param>
    public void SendNotification(TTransportIds transportId, Object message, byte[] specificClient = null)
    {
        messageSendCount++;

        if (message == null)
            message = new EmptyNotification();

        // Prepare Response Message 
        var messageToSend = CreateSocketMessage(new AhsokaMessageHeader() { TransportId = transportId.EnumToInt(), EndpointId = ((IAhsokaEndPoint)this).EndPointId }, message, true);

        // Send to Specific Client Or All Clients if Broadcast
        if (specificClient != null)
        {
            var notifId = clientIdToNotificationIds[new Guid(specificClient)];
            validNotificationIds[notifId]?.SendMessage(messageToSend);
        }
        else
        {
            foreach (var item in validNotificationIds)
            {
                messageToSend.ClientId = item.Key.ToByteArray();
                item.Value.SendMessage(messageToSend);
            }
        }
    }

    /// <summary>
    /// Send a Cache Key / Value Pair to any Registered IServiceCache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public int UpdateCacheValue(string key, object value)
    {
        if (IServiceCache.Default == null)
            return -1;

        return IServiceCache.Default.UpdateCacheValue(this.ServiceConfig.ServiceName, key, value, publisherId);
    }

    /// <summary>
    /// Raises an Event if a Event Service has been Registered.
    /// </summary>
    /// <param name="serviceEvent"></param>
    /// <param name="state"></param>
    /// <returns></returns>
    public int UpdateEvent(EventData serviceEvent, EventState state)
    {
        if (IServiceEvent.Default == null)
            return -1;

        return IServiceEvent.Default.SendEvent(serviceEvent, state);
    }

    /// <summary>
    /// Internal Raw Service Handler
    /// </summary>
    /// <param name="serviceSocket"></param>
    /// <param name="message"></param>
    /// <exception cref="ApplicationException"></exception>
    void IAhsokaServiceEndPoint.HandleServiceRequest(IAhsokaServiceSocket serviceSocket, AhsokaServiceMessage message)
    {
        // Allows for Capturing Raw Messages
        if (OnInterseptMessage(serviceSocket, message))
            return;

        messageReceiveCount++;

        // Lookup and Decode Message
        TTransportIds transportType = message.Header.TransportId.IntToEnum<TTransportIds>();
        var messageInfo = ServiceMessageInfo.GetMessageInfo(transportType);

        if (messageInfo == null)
            throw new ApplicationException("Could not Find Matching Message Object in Message List for transport " + transportType);// Not much we can do with a mesage we can't decode

        // Decode the Message Data 
        using MemoryStream objectData = new(message.MessageData);
        var recievedMessage = new AhsokaServiceRequest()
        {
            Socket = serviceSocket,
            Message = ProtoBuf.Serializer.Deserialize(messageInfo.RequestType, objectData), // Deserialize Request Type (Services)
            Header = message.Header
        };

        // Protect Multiple Sockets Calling HandleResponse Concurrently (LocalSocket, TCPSocket, Any IAhsokaSocket)
        lock (syncRoot)
        {
            // Handle Global Notification Registration
            if (recievedMessage.Header.TransportId == CoreTransportIds.RegisterClient.EnumToInt())
            {
                var registration = recievedMessage.Message as ClientRegistration;
                var notifId = new Guid(registration.ClientId);
                validNotificationIds[notifId] = serviceSocket;
                clientIdToNotificationIds[new Guid(recievedMessage.Header.ClientId)] = notifId;

                // Raise Call when Client is Connected
                OnClientConnected();

                return;
            }
            else if (recievedMessage.Header.TransportId == CoreTransportIds.DeregisterClient.EnumToInt())
            {
                var registration = recievedMessage.Message as ClientRegistration;
                validNotificationIds.Remove(new Guid(registration.ClientId));
                clientIdToNotificationIds.Remove(new Guid(recievedMessage.Header.ClientId));

                return;
            }

            // Validate Security If Enabled
            if (this.ServiceConfig.Behaviors.HasFlag(EndPointBehaviors.SecurityEnabled) && messageInfo.RequiresSecurity)
            {
                if (recievedMessage.Header.ServiceToken == null || !TokenSource.Default.IsTokenValid(message.Header.ServiceToken, message.Header.ClientId))
                {
                    // Send Invalid Transport Value
                    recievedMessage.Header.TransportId = CoreTransportIds.InvalidToken.EnumToInt();
                    SendResponse(recievedMessage, new InvalidResponseNotification());
                    return;
                }
            }

            try
            {
                // Handle Standard Message / Response Messages
                // Handler is responsible for callign Reply if needed.
                OnHandleServiceRequest(recievedMessage);
            }
            catch (Exception ex)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"An error occured while handling a service message in {this.ServiceConfig.ServiceName} \r\n {ex.ToString()}");
                recievedMessage.Header.TransportId = CoreTransportIds.ServiceErrorThrown.EnumToInt();
                SendResponse(recievedMessage, new ServiceErrorThrownResponse()
                {
                    ServiceName = this.ServiceConfig.ServiceName,
                    ErrorInformation = ex.Message,
                });
            }
        }
    }

    /// <summary>
    /// Handle Messages and Return Result Messages
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    protected virtual void OnHandleServiceRequest(AhsokaServiceRequest message) { }

    /// Raised when a Clients Connects to the Service
    /// </summary>
    protected virtual void OnClientConnected() { }

    /// <summary>
    /// Allows Service to InterseptRawMessage and Handle it externally.
    /// </summary>
    /// <param name="serviceSocket"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    protected virtual internal bool OnInterseptMessage(IAhsokaServiceSocket serviceSocket, AhsokaServiceMessage message) { return false; }

    /// <summary>
    /// Creates a Socket when no Socket is Specified
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    IAhsokaServiceSocket IAhsokaServiceEndPoint.CreateDefaultSocket(ServiceConfiguration config)
    {
        if (config.SocketType == TransportSocketTypes.TcpSocket)
            return new TCPServiceSocket();
        else
            throw new Exception("Socket Type is Not Supported");
    }

    private AhsokaServiceMessage CreateSocketMessage(AhsokaMessageHeader header, Object message, bool isNotif)
    {
        using MemoryStream dataStream = new();
        ProtoBuf.Serializer.Serialize(dataStream, message);
        return new AhsokaServiceMessage() { Header = header, MessageData = dataStream.ToArray(), IsNotification = isNotif };
    }
}

#endregion

