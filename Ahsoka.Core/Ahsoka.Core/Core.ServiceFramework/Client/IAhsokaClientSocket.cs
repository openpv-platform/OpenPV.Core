namespace Ahsoka.Core;

/// <summary>
/// Client Socket Interface
/// </summary>
public interface IAhsokaClientSocket
{
    /// <summary>
    /// Connect and Use Endpoint for Handlign Messages.
    /// </summary>
    /// <param name="endPoint"></param>
    bool Connect(IAhsokaClientEndPoint endPoint);

    /// <summary>
    /// Disconnect the Socket
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Send Data over the Socket Transport
    /// </summary>
    /// <param name="message"></param>
    void SendMessage(AhsokaClientMessage message);
}
