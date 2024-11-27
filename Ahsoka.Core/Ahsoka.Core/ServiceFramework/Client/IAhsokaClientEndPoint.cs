using Ahsoka.Dispatch;
using Ahsoka.Services.Security;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// Interface for Client EndPoints
/// </summary>
public interface IAhsokaClientEndPoint : IAhsokaEndPoint, IDispatchSource
{
    /// <summary>
    /// Creates a matching Service for this EndPoint
    /// </summary>
    /// <returns></returns>
    IAhsokaServiceEndPoint CreateDefaultService();

    /// <summary>
    /// Client ID for this EndPoint
    /// </summary>
    /// <returns></returns>
    byte[] GetClientId();

    /// <summary>
    /// Notification Client ID for this EndPoint
    /// </summary>
    /// <returns></returns>
    byte[] GetNotificationId();

    /// <summary>
    /// Sets a Security Token for Security Service Communication
    /// </summary>
    /// <param name="securityToken"></param>
    void SetToken(Token securityToken);

    /// <summary>
    /// Method used by Sockets to send Notification Messages to  a Client EndPoint
    /// </summary>
    /// <param name="message"></param>
    void HandleNotification(AhsokaClientMessage message);
}
