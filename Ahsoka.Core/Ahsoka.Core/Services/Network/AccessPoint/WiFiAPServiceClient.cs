using Ahsoka.ServiceFramework;
using Ahsoka.System;

namespace Ahsoka.Services.Network;

/// <summary>
/// Client for managing Network Interfaces and WiFi Connections.
/// </summary>
public class WiFiAPServiceClient : AhsokaClientBase<WiFiAPMessageTypes.Ids>
{
    /// <summary>
    /// Default Constructor which uses the Default Service Configuration
    /// </summary>
    public WiFiAPServiceClient() :
        this(ConfigurationLoader.GetServiceConfig(WiFiAPService.Name))
    {

    }

    /// <summary>
    /// Constructor used for using a Custom Service Configuration
    /// </summary>
    /// <param name="config">Service Configuration</param>
    public WiFiAPServiceClient(ServiceConfiguration config) :
        base(config, new WiFiAPServiceMessages())
    {

    }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndPoint OnCreateDefaultService()
    {
        return new WiFiAPService(this.ServiceConfig);
    }


    /// <summary>
    /// Get a list of Network Adapters on the system.
    /// </summary>
    /// <param name="timeoutInMs">How long to wait for the adapter list.</param>
    /// <returns></returns>
    public AdapterRequestList GetAdapters(int timeoutInMs = int.MaxValue)
    {
        return SendMessageWithResponse<AdapterRequestList>(WiFiAPMessageTypes.Ids.GetAdapters, null, timeoutInMs);
    }

    /// <summary>
    /// Enable Access Point on the Adapter
    /// </summary>
    /// <param name="adapter"></param>
    /// <param name="timeoutInMs"></param>
    public void StartAccessPoint(AdapterRequest adapter, int timeoutInMs = int.MaxValue)
    {
       SendMessageWithResponse(WiFiAPMessageTypes.Ids.StartAccessPoint, adapter, timeoutInMs);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="adapter"></param>
    /// <param name="timeoutInMs"></param>
    public void StopAccessPoint(AdapterRequest adapter, int timeoutInMs = int.MaxValue)
    {
        SendMessageWithResponse(WiFiAPMessageTypes.Ids.StopAccessPoint, adapter, timeoutInMs);
    }


    /// <summary>
    /// Get the full configuration for a single Adapter
    /// </summary>
    /// <param name="adapterRequest"></param>
    /// <param name="timeoutInMs">How long to wait for the configuration to return.</param>
    /// <returns></returns>
    public AccessPointServiceConfiguration GetNetworkConfiguration(AdapterRequest adapterRequest, int timeoutInMs = int.MaxValue)
    {
        return SendMessageWithResponse<AccessPointServiceConfiguration>(WiFiAPMessageTypes.Ids.GetNetworkConfiguration, adapterRequest, timeoutInMs);
    }

    /// <summary>
    /// Sets a new Adapter Configuration for the given Adapter allowing configuration of the Client / Station Modes as well as DHCP Services.
    /// </summary>
    /// <param name="adapterConfiguration">Configuration</param>
    /// <param name="timeoutInMs">How long to wait before returning if the service does not reply</param>
    /// <returns></returns>
    public AdapterResult SetNetworkConfiguration(AccessPointServiceConfiguration adapterConfiguration, int timeoutInMs = int.MaxValue)
    {
        return SendMessageWithResponse<AdapterResult>(WiFiAPMessageTypes.Ids.SetNetworkConfiguration, adapterConfiguration, timeoutInMs);
    }

    /// <summary>
    /// Gets list of Connected Clients
    /// </summary>
    /// <param name="adapterRequest"></param>
    /// <param name="timeoutInMs"></param>
    /// <returns></returns>
    public AccessPointClientList GetClientList(AdapterRequest adapterRequest, int timeoutInMs = int.MaxValue)
    {
        return SendMessageWithResponse<AccessPointClientList>(WiFiAPMessageTypes.Ids.GetClientList, adapterRequest, timeoutInMs);
    }
}
