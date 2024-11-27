namespace Ahsoka.ServiceFramework;

/// <summary>
/// AhsokaMessage Definition for Sockets.  Carries Header and Unknown Message Data
/// </summary>
public class AhsokaServiceMessage
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
    /// Message Data (Known only by matching Clients / Service Endpoints
    /// </summary>
    public byte[] ClientId
    {
        get { return Header.ClientId; }
        set { Header.ClientId = value; }
    }
}

