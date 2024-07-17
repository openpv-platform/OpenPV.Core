using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System.Collections.Generic;

namespace Ahsoka.Services.Network;

/// <summary>
/// Metadata Class for Network Service
/// </summary>
public class NetworkServiceMessages : AhsokaMessagesBase
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


    /// <InheritDoc/>
    public NetworkServiceMessages()
    {
        this.RegisterServiceRequest(NetworkMessageTypes.Ids.GetAdapters, typeof(EmptyNotification), typeof(AvailableAdapters));
        this.RegisterServiceRequest(NetworkMessageTypes.Ids.EnableAdapter, typeof(AdapterRequest), typeof(AdapterInfo));
        this.RegisterServiceRequest(NetworkMessageTypes.Ids.DisableAdapter, typeof(AdapterRequest), typeof(AdapterInfo));
        this.RegisterServiceRequest(NetworkMessageTypes.Ids.GetAdapterConfiguration, typeof(AdapterRequest), typeof(AdapterConfiguration));
        this.RegisterServiceRequest(NetworkMessageTypes.Ids.SetAdapterConfiguration, typeof(AdapterConfiguration), typeof(AdapterConfigurationResult));
        this.RegisterServiceRequest(NetworkMessageTypes.Ids.DiscoverNetworks, typeof(AdapterRequest), typeof(WirelessNetworkInfoList));
        this.RegisterServiceRequest(NetworkMessageTypes.Ids.ConnectToNetwork, typeof(WirelessConnectionRequest), typeof(WirelessConnectionResponse));
        this.RegisterServiceRequest(NetworkMessageTypes.Ids.DisconnectNetwork, typeof(AdapterRequest), typeof(WirelessConnectionNotification));
        this.RegisterServiceRequest(NetworkMessageTypes.Ids.GetNetworkProfiles, typeof(ProfileRequest), typeof(NetworkProfilesList));
        this.RegisterServiceRequest(NetworkMessageTypes.Ids.SetNetworkProfiles, typeof(NetworkProfilesList), typeof(EmptyNotification));
        this.RegisterServiceRequest(NetworkMessageTypes.Ids.GetNetworkInfo, typeof(AdapterRequest), typeof(IPInfo));
        this.RegisterServiceRequest(NetworkMessageTypes.Ids.AttemptAutoConnect, typeof(AutoConnectRequest), typeof(EmptyNotification));

        this.RegisterServiceNotification(NetworkMessageTypes.Ids.ConnectionStateNotification, typeof(WirelessConnectionNotification));
        this.RegisterServiceNotification(NetworkMessageTypes.Ids.NetworkAdapterReady, typeof(AdapterInfo));
        this.RegisterServiceNotification(NetworkMessageTypes.Ids.WirelessSignalChanged, typeof(WirelessNetworkInfo));
    }

    /// <InheritDoc/>
    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\NetworkService.proto", Properties.Resources.NetworkService);
        return result;
    }

    /// <InheritDoc/>
    protected override void OnGetParameters(out string group, List<ParameterData> values, PackageInformation info)
    {
        group = NetworkService.Name;

        values.Add(new() { Name = Available, DefaultValue = false, ValueType = ParameterValueTypes.Boolean });
        values.Add(new() { Name = NetworkState, DefaultValue = (int)WirelessConnectionState.ClientReady, ValueType = ParameterValueTypes.UnsignedInteger, Enumerations = EnumExtensions.EnumToDictionary<WirelessConnectionState>() });
        values.Add(new() { Name = IPAddress, DefaultValue = "0.0.0.0", ValueType = ParameterValueTypes.String });
        values.Add(new() { Name = ClientNetworkName, ValueType = ParameterValueTypes.String });
    }
}

/// <summary>
/// Request Object for Requesting a WiFi Connection be Started.
/// Adds Default Timeout Values for Connection, IP Address and Time Sync.
/// </summary>
public partial class WirelessConnectionRequest
{
    /// <summary>
    /// Default Constructor
    /// </summary>
    public WirelessConnectionRequest()
    {
        this.WaitForConnectionSeconds = 60;
        this.WaitForIPAddressSeconds = 30;
        this.WaitForTimeSyncSeconds = 30;
    }
}



/// <summary>
/// Request Object for Requesting a WiFi Connection be Started.
/// Adds Default Timeout Values for Connection, IP Address and Time Sync.
/// </summary>
public partial class AutoConnectRequest
{
    /// <summary>
    /// Default Constructor
    /// </summary>
    public AutoConnectRequest()
    {
        this.WaitForConnectionSeconds = 60;
        this.WaitForIPAddressSeconds = 30;
        this.WaitForTimeSyncSeconds = 30;
    }
}


/// <summary>
/// Request Object for Requesting a WiFi Connection be Started.
/// Adds Default Timeout Values for Connection, IP Address and Time Sync.
/// </summary>
public partial class AccessPointConfiguration
{
    /// <summary>
    /// Default Constructor
    /// </summary>
    public AccessPointConfiguration()
    {
        this.ChannelNumber = 1;
    }
}