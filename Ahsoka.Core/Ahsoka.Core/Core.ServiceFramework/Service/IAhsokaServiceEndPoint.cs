namespace Ahsoka.Core;


/// <summary>
/// Interface for AhsokaService EndPoints
/// </summary>
public interface IAhsokaServiceEndPoint : IAhsokaEndPoint
{
    /// <summary>
    /// Messages Received by Service
    /// </summary>
    int MessageReceiveCount { get; }

    /// <summary>
    /// Messages Sent by Service
    /// </summary>
    int MessageSendCount { get; }

    /// <summary>
    /// Create the Default Socket required by the Configuration
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    IAhsokaServiceSocket CreateDefaultSocket(ServiceConfiguration config);

    /// <summary>
    /// Add Local Socket to this EndPoint
    /// </summary>
    /// <param name="socket"></param>
    /// <returns></returns>
    void AddLocalSocket(IAhsokaServiceSocket socket);

    /// <summary>
    /// Add Local Socket to this EndPoint
    /// </summary>
    /// <param name="socket"></param>
    /// <returns></returns>
    void RemoveLocalSocket(IAhsokaServiceSocket socket);

    /// <summary>
    /// Start EndPoint with List of Sockets
    /// </summary>
    /// <param name="sockets"></param>
    void Start(params IAhsokaServiceSocket[] sockets);

    /// <summary>
    /// Method allowing EndPoints to Receive Service Messages from a ServiceSocket
    /// </summary>
    /// <param name="serviceSocket"></param>
    /// <param name="message"></param>
    void HandleServiceRequest(IAhsokaServiceSocket serviceSocket, AhsokaServiceMessage message);
}

