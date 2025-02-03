using Ahsoka.Core;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.Network;

/// <summary>
/// Client for managing Network Interfaces and WiFi Connections.
/// </summary>
public class CellularServiceClient : AhsokaClientBase<CellularMessageTypes.Ids>
{
    /// <summary>
    /// Default Constructor which uses the Default Service Configuration
    /// </summary>
    public CellularServiceClient() :
        this(ConfigurationLoader.GetServiceConfig(CellularService.Name))
    {

    }

    /// <summary>
    /// Constructor used for using a Custom Service Configuration
    /// </summary>
    /// <param name="config">Service Configuration</param>
    public CellularServiceClient(ServiceConfiguration config) :
        base(config, new CellularServiceMessages())
    {

    }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndPoint OnCreateDefaultService()
    {
        return new CellularService(this.ServiceConfig);
    }


    /// <summary>
    /// Get a list of Network Adapters on the system.
    /// </summary>
    /// <param name="timeoutInMs">How long to wait for the adapter list.</param>
    /// <returns></returns>
    public AdapterRequestList GetAdapters(int timeoutInMs = int.MaxValue)
    {
        return SendMessageWithResponse<AdapterRequestList>(CellularMessageTypes.Ids.GetAdapters, null, timeoutInMs);
    }

    /// <summary>
    /// Get the full configuration for a single Adapter
    /// </summary>
    /// <param name="adapterConfigurationRequest">Adapter to Return</param>
    /// <param name="timeoutInMs">How long to wait for the configuration to return.</param>
    /// <returns></returns>
    public CellularNetworkInformation GetNetworkInformation(AdapterRequest adapterConfigurationRequest, int timeoutInMs = int.MaxValue)
    {
        return SendMessageWithResponse<CellularNetworkInformation>(CellularMessageTypes.Ids.GetNetworkInformation, adapterConfigurationRequest, timeoutInMs);
    }

    /// <summary>
    /// Sets a new Adapter Configuration for the given Adapter allowing configuration of the Client / Station Modes as well as DHCP Services.
    /// </summary>
    /// <param name="adapterConfiguration">Configuration</param>
    /// <param name="timeoutInMs">How long to wait before returning if the service does not reply</param>
    /// <returns></returns>
    public CellularConnectionResult StartNetwork(CellularServiceConfiguration adapterConfiguration, int timeoutInMs = int.MaxValue)
    {
        return SendMessageWithResponse<CellularConnectionResult>(CellularMessageTypes.Ids.StartNetwork, adapterConfiguration, timeoutInMs);
    }

    /// <summary>
    /// Disconnect the Adapter from any Wireless Networks it may be connected to.
    /// </summary>
    /// <param name="adapterRequest">Adapter to Disconnect</param>
    [ExcludeFromCodeCoverage]
    public void StopNetwork(AdapterRequest adapterRequest)
    {
        SendMessageWithResponse(CellularMessageTypes.Ids.StopNetwork, adapterRequest);
    }

}
