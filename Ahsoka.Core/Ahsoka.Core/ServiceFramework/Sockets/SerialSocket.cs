using System;

#pragma warning disable CS1591

namespace Ahsoka.ServiceFramework;

#region Socket

/// <summary>
/// Socket to adapt Ahsoka Messages to Serial Interfacees.
/// </summary>
public abstract class SerialSocket : IAhsokaSocket
{
    public byte[] SocketID { get; } = Guid.NewGuid().ToByteArray();

    public bool IsConnected { get; set; }

    protected IAhsokaEndPoint EndPoint { get; set; }

    public virtual void SendMessage(byte[] header, byte[] data, byte[] clientId, bool isBroadcast)
    {
        byte[] headerSize = BitConverter.GetBytes((UInt16)header.Length);

        byte[] transmitBuffer = new byte[headerSize.Length + header.Length + data.Length];

        Array.Copy(headerSize, 0, transmitBuffer, 0, headerSize.Length);
        Array.Copy(header, 0, transmitBuffer, headerSize.Length, header.Length);
        Array.Copy(data, 0, transmitBuffer, headerSize.Length + header.Length, data.Length);

        if (!TransmitOverInterface(transmitBuffer, clientId))
        {
            // We should maybe throw an error or log something in this case because we were unable to successfully
            // transmit the value through the underlying interface.
        }
    }

    public virtual bool ReceiveMessage(out byte[] header, out byte[] data, out byte[] clientId, bool isNotification)
    {
        byte[] packetData;
        if (!ReceiveOverInterface(out packetData, out clientId))
        {
            // There was no message available for us to retrieve from the underlying interface.
            header = null;
            data = null;
            clientId = null;
            return false;
        }

        UInt16 headerSize = BitConverter.ToUInt16(packetData, 0);

        header = new byte[headerSize];
        Array.Copy(packetData, BitConverter.GetBytes(headerSize).Length, header, 0, headerSize);

        int dataSize = packetData.Length - (BitConverter.GetBytes(headerSize).Length + header.Length);
        data = new byte[dataSize];
        Array.Copy(packetData, BitConverter.GetBytes(headerSize).Length + header.Length, data, 0, dataSize);

        return true;
    }

    protected abstract bool TransmitOverInterface(byte[] packetData, byte[] clientId);

    protected abstract bool ReceiveOverInterface(out byte[] packetData, out byte[] clientId);

    void IAhsokaSocket.Connect(IAhsokaEndPoint endPoint) { OnConnect(endPoint); }

    protected abstract void OnConnect(IAhsokaEndPoint endPoint);

    void IAhsokaSocket.Disconnect() { OnDisconnect(); }

    protected abstract void OnDisconnect();

}
#endregion

#pragma warning restore CS1591