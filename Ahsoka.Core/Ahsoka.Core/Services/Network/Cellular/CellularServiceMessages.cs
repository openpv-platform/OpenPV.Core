using Ahsoka.Core;
using Ahsoka.Core.Utility;
using Ahsoka.Installer;
using System.Collections.Generic;

namespace Ahsoka.Services.Network;

/// <summary>
/// Metadata Class for Network Service
/// </summary>
public class CellularServiceMessages : AhsokaMessagesBase
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
    public const string IPAddress = "IPAddress";


    /// <InheritDoc/>
    public CellularServiceMessages() : base(CellularService.Name)
    {
        this.RegisterServiceRequest(CellularMessageTypes.Ids.GetAdapters, typeof(EmptyNotification), typeof(AdapterRequestList));
        this.RegisterServiceRequest(CellularMessageTypes.Ids.GetNetworkInformation, typeof(AdapterRequest), typeof(AdapterConfiguration));
        this.RegisterServiceRequest(CellularMessageTypes.Ids.StartNetwork, typeof(CellularServiceConfiguration), typeof(CellularConnectionResult));
        this.RegisterServiceRequest(CellularMessageTypes.Ids.StopNetwork, typeof(AdapterRequest), typeof(EmptyNotification));

        this.RegisterServiceNotification(CellularMessageTypes.Ids.NetworkStateChanged, typeof(CellularNetworkInformation));
    }

    /// <InheritDoc/>
    public override byte[] GetProtoMessageFile()
    {
        return this.GenerateProtoFile("AhsokaCellularService", typeof(CellularMessageTypes.Ids));
    }

    /// <InheritDoc/>
    protected override void OnGetParameters(out string group, List<ParameterData> values, PackageInformation info)
    {
        group = WiFiClientService.Name;

        values.Add(new() { Name = Available, DefaultValue = false, ValueType = ParameterValueTypes.Boolean });
        values.Add(new() { Name = NetworkState, DefaultValue = (int)WirelessConnectionState.ClientReady, ValueType = ParameterValueTypes.UnsignedInteger, Enumerations = EnumExtensions.EnumToDictionary<WirelessConnectionState>() });
        values.Add(new() { Name = IPAddress, DefaultValue = "0.0.0.0", ValueType = ParameterValueTypes.String });
    }
}