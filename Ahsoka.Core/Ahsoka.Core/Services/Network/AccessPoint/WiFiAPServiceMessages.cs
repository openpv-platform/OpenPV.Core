using Ahsoka.Core;
using Ahsoka.Installer;
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
    public WiFiAPServiceMessages() : base(WiFiAPService.Name)
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
    public override byte[] GetProtoMessageFile()
    {
        return this.GenerateProtoFile("AhsokaAPService", typeof(WiFiAPMessageTypes.Ids));
    }

    /// <InheritDoc/>
    protected override void OnGetParameters(out string group, List<ParameterData> values, PackageInformation info)
    {
        group = WiFiClientService.Name;

        values.Add(new() { Name = Available, DefaultValue = false, ValueType = ParameterValueTypes.Boolean });
    }
}