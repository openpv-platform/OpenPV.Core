using Ahsoka.Dispatch;
using Ahsoka.Services.Security;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// Base Template for Ahsoka Client Endpoints
/// </summary>
/// <typeparam name="TTransportIds"></typeparam>
public abstract partial class AhsokaClientBase<TTransportIds> : AhsokaEndPoint<TTransportIds>, IAhsokaClientEndPoint
    where TTransportIds : Enum, new()
{
    #region Fields
    List<IClientEndPointBehavior<TTransportIds>> behaviors = new();
    int messageId = 0;
    bool isConnected = false;
    
    // Unique ID's for Clients
    byte[] clientId = Guid.NewGuid().ToByteArray();
    byte[] notificationClientId = Guid.NewGuid().ToByteArray();

    IAhsokaClientSocket socket;
    IAhsokaServiceEndPoint autoStartService = null;
    Token securityToken = null;
    #endregion

    #region Properties
    /// <summary>
    /// Indicates if the Service is Started
    /// </summary>
    public override bool IsStarted { get { return isConnected; } }
    #endregion

    #region Service Notification Handler 
    /// <summary>
    /// Arg Object for This Endpoint
    /// </summary>
    public class AhsokaNotificationArgs : NotificationEventArgs<TTransportIds> { }

    /// <summary>
    /// Event for Receiving Out of Band Messages
    /// </summary>
    public event EventHandler<AhsokaNotificationArgs> NotificationReceived;

    // Dispatch Event Handler
    event EventHandler<EventArgs> IDispatchSource.HandleDispatchMessage
    {
        add
        {
            NotificationReceived += value.Invoke;
        }

        remove
        {
            NotificationReceived -= value.Invoke;
        }
    }

    #endregion

    #region Methods
    /// <summary>
    /// Base Constructor for Ahsoka Client Endpoints
    /// </summary>
    /// <param name="config"></param>
    /// <param name="messages"></param>
    protected AhsokaClientBase(ServiceConfiguration config, AhsokaMessagesBase messages) :
        base(config, messages)
    {

    }

    /// <summary>
    /// Unique ID for this Clients Message Channel
    /// </summary>
    /// <returns></returns>
    public byte[] GetClientId() { return clientId; }

    /// <summary>
    /// Unique ID for this Clients Notification Channel
    /// </summary>
    /// <returns></returns>
    public byte[] GetNotificationId() { return notificationClientId; }

    /// <summary>
    /// Default Start Function using default TCP Client
    /// </summary>
    public override void Start()
    {
        Start(CreateDefaultSocket(this.ServiceConfig));
    }

    /// <summary>
    /// Start Accepting Custom Socket
    /// </summary>
    /// <param name="clientSocket"></param>
    public void Start(IAhsokaClientSocket clientSocket)
    {
        if (!IsStarted)
        {    
            // Before Connecting
            OnEndPointStarting();

            AutoStartServiceIfNeeded();

            socket = clientSocket;
            socket.Connect(this);

            ((IAhsokaEndPoint)this).CurrentRuntime = AhsokaRuntime.Default;
            AhsokaRuntime.Default.AddEndPointInternal(this);

            // Connected then Register
            isConnected = true;

            // Register Client for Notifications
            UpdateClientRegistration(CoreTransportIds.RegisterClient);

            // After Connected
            OnEndPointStarted();

        }
    }

    /// <summary>
    ///  Stop EndPoint
    /// </summary>
    public override void Stop()
    {
        if (IsStarted)
        {
            // Deregister Client for Notifications
            UpdateClientRegistration(CoreTransportIds.DeregisterClient);

            socket.Disconnect();

            AhsokaRuntime.Default.RemoveEndPointInternal(this);

            if (autoStartService != null)
                autoStartService.Stop();

            OnEndPointStopped();

            isConnected = false;
        }
    }

    /// <summary>
    ///  Restarts the Client with a new Connection
    /// </summary>
    /// <param name="config"></param>
    public void UpdateConfiguration(ServiceConfiguration config)
    {
        bool wasConnected = IsStarted;
        if (wasConnected)
            this.socket.Disconnect();

        this.ServiceConfig = config;

        if (wasConnected)
            this.socket.Connect(this);
    }

    private void UpdateClientRegistration(CoreTransportIds transportId)
    {
        TTransportIds id = transportId.EnumToInt().IntToEnum<TTransportIds>();
        SendMessage(id, new ClientRegistration()
        {
            ClientId = ((IAhsokaClientEndPoint)this).GetNotificationId(),
        });
    }

    IAhsokaServiceEndPoint IAhsokaClientEndPoint.CreateDefaultService()
    {
        return OnCreateDefaultService();
    }

    /// <summary>
    /// Auto Start Function - Starts a Matching Service if one is not Running.
    /// </summary>
    protected virtual void AutoStartServiceIfNeeded()
    {
        // Handle AutoStartServices
        if (autoStartService == null && ServiceConfig.Behaviors.HasFlag(EndPointBehaviors.AutoStart))
        {
            // Find a matching Service if Available
            var existingServiceMatch = AhsokaRuntime.Default.GetService(this.ServiceConfig.ServiceName, this.ServiceConfig.DataChannel);
            if (existingServiceMatch != null && existingServiceMatch.IsStarted)
                return;

            // Create a Service 
            autoStartService = ((IAhsokaClientEndPoint)this).CreateDefaultService();
            autoStartService.Start();
        }
    }

    /// <summary>
    /// Set a Security token for use when Talkign to Secure Services
    /// </summary>
    /// <param name="securityToken"></param>
    public void SetToken(Token securityToken)
    {
        this.securityToken = securityToken;
    }

    /// <summary>
    /// Add a Behavior Object to this EndPoint
    /// </summary>
    /// <param name="behavior"></param>
    public void AddBehavior(IClientEndPointBehavior<TTransportIds> behavior)
    {
        behavior.Init(this, ServiceConfig);
        behaviors.Add(behavior);
    }

    void IAhsokaClientEndPoint.HandleNotification(AhsokaClientMessage message)
    {
        // Lookup and Decode Message
        TTransportIds transportType = message.Header.TransportId.IntToEnum<TTransportIds>();
        var messageInfo = ServiceMessageInfo.GetMessageInfo(transportType);

        if (messageInfo == null)
            throw new ApplicationException("Could not Find Matching Message Object in Message List for transport " + transportType);// Not much we can do with a mesage we can't decode

        // Decode the Message Data 
        using MemoryStream objectData = new(message.MessageData);
        var args = new AhsokaNotificationArgs()
        {
            TransportId = message.Header.TransportId.IntToEnum<TTransportIds>(),
            NotificationObject = ProtoBuf.Serializer.Deserialize(messageInfo.ReplyType, objectData)
        };

        foreach (IClientEndPointBehavior<TTransportIds> behavior in behaviors)
            behavior.OnNotificationMessageRecieved(args.TransportId, args.NotificationObject);

        OnReceiveNotification(args);

        // Raise Event if not Handled
        if (args.ShouldRaiseEvent)
            NotificationReceived?.Invoke(this, args);
    }

    /// <summary>
    /// Send an Non-Acked MEssage
    /// </summary>
    /// <param name="transportId"></param>
    /// <param name="messageObject"></param>
    public void SendMessage(TTransportIds transportId, object messageObject = null)
    {
        if (IsStarted)
        {
            AhsokaClientMessage message = CreateSocketMessage(transportId, messageObject);
            socket.SendMessage(message);
        }
    }

    /// <summary>
    /// Sends a message and waits for an Empty Response
    /// </summary>
    /// <param name="transportId"></param>
    /// <param name="requestObject"></param>
    /// <param name="timeoutInMs"></param>
    public void SendMessageWithResponse(TTransportIds transportId, object requestObject = null, int timeoutInMs = int.MaxValue)
    {
        SendMessageWithResponse<EmptyNotification>(transportId, requestObject, timeoutInMs);
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
    public virtual T SendMessageWithResponse<T>(TTransportIds transportId, object requestObject = null, int timeoutInMs = int.MaxValue) where T : class
    {
        if (!IsStarted)
            Start();

        if (requestObject == null)
            requestObject = new EmptyNotification();

        foreach (IClientEndPointBehavior<TTransportIds> behavior in behaviors)
            behavior.OnSendMessage(transportId, requestObject);

        AhsokaClientMessage message = CreateSocketMessage(transportId, requestObject);

        T returnObject = null;

        // Send Message and Get Response
        var response = socket.SendMessageWithResponse(message, timeoutInMs);
        if (response != null)
        {
            // Check Security
            if (response.Header.TransportId == CoreTransportIds.InvalidToken.EnumToInt())
                throw new SecurityException("The Service returned an InvalidToken Message. This usually indicates that this security enabled endpoint was not unlocked with a valid token");
           
            // Decode Object
            using MemoryStream responseData = new(response.MessageData);
            returnObject = ProtoBuf.Serializer.Deserialize<T>(responseData);

            // Throw Service Errors on Client
            if (response.Header.TransportId == CoreTransportIds.ServiceErrorThrown.EnumToInt() && returnObject is ServiceErrorThrownResponse errorMessage)
                throw new SecurityException($"The Service {errorMessage.ServiceName} returned an error. {errorMessage.ErrorInformation}");
        }

        return returnObject;
    }

    /// <summary>
    /// Creates a Socket when no Socket is Specified
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    protected IAhsokaClientSocket CreateDefaultSocket(ServiceConfiguration config)
    {
        if (config.SocketType == TransportSocketTypes.TcpSocket)
            return new TCPClientSocket();
        else
            throw new Exception("Socket Type is Not Supported");
    }

    private AhsokaClientMessage CreateSocketMessage(TTransportIds transportId, object requestObject)
    {
        if (requestObject == null) 
            requestObject = new EmptyNotification();

        using MemoryStream requestData = new();
        ProtoBuf.Serializer.Serialize(requestData, requestObject);

        return new AhsokaClientMessage()
        {
            MessageData = requestData.ToArray(),
            Header = new()
            {
                TransportId = transportId.EnumToInt(),
                ClientMessageId = messageId++,
                ServiceToken = securityToken?.ClientToken
            }
        };
    }

    /// <summary>
    /// Intercepts Notifications allowing them to be handled before passing to Client Application
    /// </summary>
    /// <param name="args"></param>
    protected virtual void OnReceiveNotification(AhsokaNotificationArgs args) { }

    /// <summary>
    /// Creates a Service to match this EndPoint
    /// </summary>
    /// <returns></returns>
    protected abstract IAhsokaServiceEndPoint OnCreateDefaultService();
    #endregion
}

/// <summary>
/// Notification Message
/// </summary>
/// <typeparam name="T"></typeparam>
public class NotificationEventArgs<T> : EventArgs
{
    /// <summary>
    /// The Message Type
    /// </summary>
    public T TransportId { get; set; }

    /// <summary>
    /// Object Received with this Notification
    /// </summary>
    public object NotificationObject { get; set; }

    /// <summary>
    /// Indicates if the public NotificationReceived Event should be raised
    /// </summary>
    public bool ShouldRaiseEvent { get; set; } = true;
}
