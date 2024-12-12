using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System.Collections.Generic;

namespace Ahsoka.Services.Network;

/// <summary>
/// Metadata Class for Network Service
/// </summary>
public class WiFiAPServiceMessages : AhsokaMessagesBase
{

    /// <summary>
    /// Is the Service Available
    /// </summary>
    public const string Available = "Available";

    /// <InheritDoc/>
    public WiFiAPServiceMessages()
    {
        this.RegisterServiceRequest(WiFiAPMessageTypes.Ids.GetAdapters, typeof(EmptyNotification), typeof(AdapterRequestList));
        this.RegisterServiceRequest(WiFiAPMessageTypes.Ids.StartAccessPoint, typeof(AdapterRequest), typeof(AdapterResult));
        this.RegisterServiceRequest(WiFiAPMessageTypes.Ids.StopAccessPoint, typeof(AdapterRequest), typeof(AdapterResult));

        this.RegisterServiceRequest(WiFiAPMessageTypes.Ids.GetNetworkConfiguration, typeof(AdapterRequest), typeof(AccessPointServiceConfiguration));
        this.RegisterServiceRequest(WiFiAPMessageTypes.Ids.SetNetworkConfiguration, typeof(AccessPointServiceConfiguration), typeof(AdapterResult));

        this.RegisterServiceRequest(WiFiAPMessageTypes.Ids.GetClientList, typeof(AdapterRequest), typeof(AccessPointClientList));

        this.RegisterServiceNotification(WiFiAPMessageTypes.Ids.ClientAdded, typeof(AccessPointClient));
        this.RegisterServiceNotification(WiFiAPMessageTypes.Ids.ClientRemoved, typeof(AccessPointClient));
    }

    /// <InheritDoc/>
    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\WiFiAPService.proto", Properties.Resources.WiFiAPService);
        return result;
    }

    /// <InheritDoc/>
    protected override void OnGetParameters(out string group, List<ParameterData> values, PackageInformation info)
    {
        group = WiFiClientService.Name;

        values.Add(new() { Name = Available, DefaultValue = false, ValueType = ParameterValueTypes.Boolean });
    }
}