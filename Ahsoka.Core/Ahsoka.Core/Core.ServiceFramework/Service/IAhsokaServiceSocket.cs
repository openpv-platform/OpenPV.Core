namespace Ahsoka.Core;

/// <summary>
/// Interface for Service Sockets
/// </summary>
public interface IAhsokaServiceSocket
{
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
    void SendMessage(AhsokaServiceMessage message);
}
