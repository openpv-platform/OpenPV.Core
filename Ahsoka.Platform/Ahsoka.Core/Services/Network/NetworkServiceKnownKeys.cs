using Ahsoka.ServiceFramework;

namespace Ahsoka.Services.Network;

/// <summary>
/// List of Key Values Available in this Service
/// </summary>
public class NetworkServiceDataKeys : KnownDataKeys
{
    /// <summary>
    /// Is the Service Available
    /// </summary>
    public const string Available = "Available";

    /// <summary>
    /// CurrentState of the WiFi Network
    /// </summary>
    public const string NetworkState = "NetworkState";

    /// <summary>
    /// Conneced Network Name
    /// </summary>
    public const string ClientNetworkName = "ClientNetworkName";

    /// <summary>
    /// Conneced Network Name
    /// </summary>
    public const string IPAddress = "IPAddress";
}
