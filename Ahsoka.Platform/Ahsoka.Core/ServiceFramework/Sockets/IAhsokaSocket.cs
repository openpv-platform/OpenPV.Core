using EnovationCloud.Core.Framework;
using System;
using System.Runtime.CompilerServices;

namespace Ahsoka.ServiceFramework;

#region Socket
/// <summary>
/// Interface for Ahsoka Sockets that adds Client Registration
/// </summary>
public interface IAhsokaServiceSocket : IAhsokaSocket
{
    /// <summary>
    /// Enables a client to recieve notification messages
    /// </summary>
    /// <param name="clientRegistration"></param>
    void RegisterClient(ClientRegistration clientRegistration);

    /// <summary>
    /// Disables a client to recieve notification messages
    /// </summary>
    /// <param name="clientRegistration"></param>
    void DeregisterClient(ClientRegistration clientRegistration);
}

/// <summary>
/// Interface for Ahsoka Sockets
/// </summary>
public interface IAhsokaSocket
{
    /// <summary>
    /// Default Timeout for Send / Recieve
    /// </summary>
    protected const int MaxTimeoutValue = 7 * 24 * 60 * 1000;

    /// <summary>
    /// Indicates the State of the Socket
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connect the Socket with the Specfied Configuration
    /// </summary>
    void Connect(IAhsokaEndPoint endPoint);

    /// <summary>
    /// Disconnect the Socket
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Send Data over the Socket Transport
    /// </summary>
    /// <param name="header"></param>
    /// <param name="data"></param>
    /// <param name="clientId"></param>
    /// <param name="isBroadcast"></param>
    void SendMessage(byte[] header, byte[] data, byte[] clientId, bool isBroadcast);

    /// <summary>
    /// Recieve a message over the Socket Transport
    /// </summary>
    /// <param name="header"></param>
    /// <param name="data"></param>
    /// <param name="clientId"></param>
    /// <param name="timeOutInMs"></param>
    /// <returns></returns>
    bool ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId, int timeOutInMs = int.MaxValue);
}



#endregion


/// <summary>
/// Extensions for Ahsoka Sockets
/// </summary>
public static class AhsokaSocketExtensions
{
    /// <summary>
    /// Guid Convertion
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static string ToGuidString(this byte[] bytes)
    {
        return bytes.ToGuid().ToString();
    }

    /// <summary>
    /// Guid Convertion
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static Guid ToGuid(this byte[] bytes)
    {
        return new Guid(bytes);
    }
}

