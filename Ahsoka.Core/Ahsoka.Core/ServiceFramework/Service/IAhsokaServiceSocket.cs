namespace Ahsoka.ServiceFramework;

/// <summary>
/// Interface for Service Sockets
/// </summary>
public interface IAhsokaServiceSocket
{
    /// <summary>
    /// Default Timeout for Send / Recieve
    /// </summary>
    protected const int MaxTimeoutValue = 7 * 24 * 60 * 1000;

    /// <summary>
    /// Connect and Use Endpoint for Handlign Messages.
    /// </summary>
    /// <param name="endPoint"></param>
    void Connect(IAhsokaServiceEndPoint endPoint);

    /// <summary>
    /// Disconnect the Socket
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Send Data over the Socket Transport
    /// </summary>
    /// <param name="message"></param>
    /// <param name="isReply"></param>
    void SendMessage(AhsokaServiceMessage message, bool isReply);
}
