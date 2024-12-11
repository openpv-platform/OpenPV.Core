using Ahsoka.ServiceFramework;
using Ahsoka.System;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.Network;

/// <summary>
/// Client for managing Network Interfaces and WiFi Connections.
/// </summary>
public class WiFiClientServiceClient : AhsokaClientBase<WiFiClientMessageTypes.Ids>
{
    /// <summary>
    /// Default Constructor which uses the Default Service Configuration
    /// </summary>
    public WiFiClientServiceClient() :
        this(ConfigurationLoader.GetServiceConfig(WiFiClientService.Name))
    {

    }

    /// <summary>
    /// Constructor used for using a Custom Service Configuration
    /// </summary>
    /// <param name="config">Service Configuration</param>
    public WiFiClientServiceClient(ServiceConfiguration config) :
        base(config, new WiFiClientServiceMessages())
    {

    }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndPoint OnCreateDefaultService()
    {
        return new WiFiClientService(this.ServiceConfig);
    }


    /// <summary>
    /// Get a list of Network Adapters on the system.
    /// </summary>
    /// <param name="timeoutInMs">How long to wait for the adapter list.</param>
    /// <returns></returns>
    public AdapterResult GetAdapter(int timeoutInMs = int.MaxValue)
    {
        return SendMessageWithResponse<AdapterResult>(WiFiClientMessageTypes.Ids.GetAdapter, null, timeoutInMs);
    }


    /// <summary>
    /// Get the IP Information for the Specified Adapter.  Allows the client to get information about the IP Network and Address.
    /// </summary>
    /// <param name="adapterConfigurationRequest"></param>
    /// <param name="timeoutInMs"></param>
    /// <returns></returns>
    public IPInfo GetIPInfo(AdapterRequest adapterConfigurationRequest, int timeoutInMs = int.MaxValue)
    {
        return SendMessageWithResponse<IPInfo>(WiFiClientMessageTypes.Ids.GetNetworkInfo, adapterConfigurationRequest, timeoutInMs);
    }


    /// <summary>
    /// Get the full configuration for a single Adapter
    /// </summary>
    /// <param name="adapterConfigurationRequest">Adapter to Return</param>
    /// <param name="timeoutInMs">How long to wait for the configuration to return.</param>
    /// <returns></returns>
    public AdapterConfiguration GetNetworkConfiguration(AdapterRequest adapterConfigurationRequest, int timeoutInMs = int.MaxValue)
    {
        return SendMessageWithResponse<AdapterConfiguration>(WiFiClientMessageTypes.Ids.GetNetworkConfiguration, adapterConfigurationRequest, timeoutInMs);
    }

    /// <summary>
    /// Sets a new Adapter Configuration for the given Adapter allowing configuration of the Client / Station Modes as well as DHCP Services.
    /// </summary>
    /// <param name="adapterConfiguration">Configuration</param>
    /// <param name="timeoutInMs">How long to wait before returning if the service does not reply</param>
    /// <returns></returns>
    public AdapterResult SetNetworkConfiguration(AdapterConfiguration adapterConfiguration, int timeoutInMs = int.MaxValue)
    {
        return SendMessageWithResponse<AdapterResult>(WiFiClientMessageTypes.Ids.SetNetworkConfiguration, adapterConfiguration, timeoutInMs);
    }


    /// <summary>
    /// Gets a list of Local Networks which can be discovered by a WiFi Adapter.
    /// </summary>
    /// <param name="adapterRequest">Adapter to Query</param>
    /// <returns></returns>
    public WirelessNetworkInfoList DiscoverNetworks(AdapterRequest adapterRequest)
    {
        return SendMessageWithResponse<WirelessNetworkInfoList>(WiFiClientMessageTypes.Ids.DiscoverNetworks, adapterRequest);
    }

    /// <summary>
    /// Gets a list of Saved Networks
    /// </summary>
    /// <param name="profileRequest">Adapter to Query</param>
    /// <param name="timeoutInMs">How long to wait before returning if the service does not reply</param>
    /// <returns></returns>
    public NetworkProfilesList GetNetworkProfiles(ProfileRequest profileRequest, int timeoutInMs = int.MaxValue)
    {
        return SendMessageWithResponse<NetworkProfilesList>(WiFiClientMessageTypes.Ids.GetNetworkProfiles, profileRequest, timeoutInMs);
    }

    /// <summary>
    /// Updates the list of Saved Networks (Used to change the order of networks in the list)
    /// </summary>
    /// <param name="profiles">Profile List with Changes to Update</param>
    /// <param name="timeoutInMs">How long to wait before returning if the service does not reply</param>
    public void SetNetworkProfiles(NetworkProfilesList profiles, int timeoutInMs = int.MaxValue)
    {
        SendMessageWithResponse<EmptyNotification>(WiFiClientMessageTypes.Ids.SetNetworkProfiles, profiles);
    }

    /// <summary>
    /// Connect to the Specified Network with the Specified Connection Information
    /// </summary>
    /// <param name="connection">Network to Connect</param>
    [ExcludeFromCodeCoverage]
    public void Connect(WirelessConnectionRequest connection)
    {
        SendMessageWithResponse<WirelessConnectionResponse>(WiFiClientMessageTypes.Ids.ConnectToNetwork, connection);
    }

    /// <summary>
    /// Disconnect the Adapter from any Wireless Networks it may be connected to.
    /// </summary>
    /// <param name="adapterRequest">Adapter to Disconnect</param>
    [ExcludeFromCodeCoverage]
    public void Disconnect(AdapterRequest adapterRequest)
    {
        SendMessageWithResponse(WiFiClientMessageTypes.Ids.DisconnectNetwork, adapterRequest);
    }


    /// <summary>
    /// Attempt to connect to first saved network in range
    /// </summary>
    /// <param name="connectRequest">Auto Connect Parameters</param>
    /// <param name="timeoutInMs">How long to wait before returning if the service does not reply</param>
    /// <returns></returns>
    public void AttemptAutoConnect(AutoConnectRequest connectRequest, int timeoutInMs = int.MaxValue)
    {
        SendMessageWithResponse<NetworkProfilesList>(WiFiClientMessageTypes.Ids.AttemptAutoConnect, connectRequest, timeoutInMs);
    }
}
