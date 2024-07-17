using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System.Collections.Generic;

namespace Ahsoka.Services.Network;

/// <summary>
/// List of Key Values Available in this Service
/// </summary>
public class NetworkServiceParameters : ServiceParametersBase
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
    protected override void OnGetParameters(out string group, List<ParameterData> values, PackageInformation info)
    {
        group = NetworkService.Name;

        values.Add(new() { Name = Available, DefaultValue = false, ValueType =  ParameterValueTypes.Boolean });
        values.Add(new() { Name = NetworkState, DefaultValue = (int)WirelessConnectionState.ClientReady, ValueType =  ParameterValueTypes.UnsignedInteger, Enumerations = EnumExtensions.EnumToDictionary<WirelessConnectionState>() });
        values.Add(new() { Name = IPAddress, DefaultValue = "0.0.0.0", ValueType = ParameterValueTypes.String });
        values.Add(new() { Name = ClientNetworkName, ValueType = ParameterValueTypes.String });
    }
}


