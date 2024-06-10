using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS1591

[assembly: InternalsVisibleTo("Ahsoka.Core.Cygnus")]

namespace Ahsoka.ServiceFramework;

#region Socket
public abstract class AhsokaSocket
{
    protected const int maxTimeoutValue = 7 * 24 * 60 * 1000;

    internal IAhsokaEndPoint EndPoint { get; init; }

    public bool IsConnected { get; set; }

    internal abstract void Connect();

    internal abstract void Disconnect();

    internal abstract void SendMessage(byte[] header, byte[] data, byte[] clientId, bool sendAsNotification);

    internal abstract bool ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId, bool receiveAsNotification, int timeOutInMs = int.MaxValue);
}
#endregion

public static class AhsokaSocketExtensions
{
    public static string ToGuidString(this byte[] bytes)
    {
        return new Guid(bytes).ToString();
    }
}

#pragma warning restore CS1591