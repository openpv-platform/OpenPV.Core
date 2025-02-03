namespace Ahsoka.Core;

/// <summary>
/// Client Messages used internally by AhsokaClientEndPoints and Sockets
/// </summary>
public class AhsokaClientMessage
{
    /// <summary>
    /// Message Header
    /// </summary>
    public AhsokaMessageHeader Header { get; set; }

    /// <summary>
    /// Message Data (Known only by matching Clients / Service Endpoints
    /// </summary>
    public byte[] MessageData { get; set; }

    /// <summary>
    ///  Internal Value used for Clients waiting for response messages
    /// </summary>
    internal int ResponseTimeout
    {
        get;
        set;
    } = -1;
}
#pragma warning restore CS1591