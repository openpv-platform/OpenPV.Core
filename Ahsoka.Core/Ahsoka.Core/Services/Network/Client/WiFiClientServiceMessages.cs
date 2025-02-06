using Ahsoka.Core;
using Ahsoka.Core.Utility;
using Ahsoka.Installer;
using System.Collections.Generic;

namespace Ahsoka.Services.Network;

/// <summary>
/// Metadata Class for Network Service
/// </summary>
public class WiFiClientServiceMessages : AhsokaMessagesBase
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
    public WiFiClientServiceMessages() : base(WiFiClientService.Name)
    {
        this.RegisterServiceRequest(WiFiClientMessageTypes.Ids.GetAdapters, typeof(EmptyNotification), typeof(AdapterRequestList));
        this.RegisterServiceRequest(WiFiClientMessageTypes.Ids.GetNetworkConfiguration, typeof(AdapterRequest), typeof(AdapterConfiguration));
        this.RegisterServiceRequest(WiFiClientMessageTypes.Ids.SetNetworkConfiguration, typeof(AdapterConfiguration), typeof(AdapterResult));
        this.RegisterServiceRequest(WiFiClientMessageTypes.Ids.DiscoverNetworks, typeof(AdapterRequest), typeof(WirelessNetworkInfoList));
        this.RegisterServiceRequest(WiFiClientMessageTypes.Ids.ConnectToNetwork, typeof(WirelessConnectionRequest), typeof(WirelessConnectionResponse));
        this.RegisterServiceRequest(WiFiClientMessageTypes.Ids.DisconnectNetwork, typeof(AdapterRequest), typeof(WirelessConnectionNotification));
        this.RegisterServiceRequest(WiFiClientMessageTypes.Ids.GetNetworkProfiles, typeof(ProfileRequest), typeof(NetworkProfilesList));
        this.RegisterServiceRequest(WiFiClientMessageTypes.Ids.SetNetworkProfiles, typeof(NetworkProfilesList), typeof(EmptyNotification));
        this.RegisterServiceRequest(WiFiClientMessageTypes.Ids.GetNetworkInfo, typeof(AdapterRequest), typeof(IPInfo));
        this.RegisterServiceRequest(WiFiClientMessageTypes.Ids.AttemptAutoConnect, typeof(AutoConnectRequest), typeof(EmptyNotification));

        this.RegisterServiceNotification(WiFiClientMessageTypes.Ids.ConnectionStateNotification, typeof(WirelessConnectionNotification));
        this.RegisterServiceNotification(WiFiClientMessageTypes.Ids.WirelessSignalChanged, typeof(WirelessNetworkInfo));
    }

    /// <InheritDoc/>
    public override byte[] GetProtoMessageFile()
    {
        return this.GenerateProtoFile("AhsokaWiFiClient", typeof(WiFiClientMessageTypes.Ids));
    }

    /// <InheritDoc/>
    protected override void OnGetParameters(out string group, List<ParameterData> values, PackageInformation info)
    {
        group = WiFiClientService.Name;

        values.Add(new() { Name = Available, DefaultValue = false, ValueType = ParameterValueTypes.Boolean });
        values.Add(new() { Name = NetworkState, DefaultValue = (int)WirelessConnectionState.ClientReady, ValueType = ParameterValueTypes.UnsignedInteger, Enumerations = EnumExtensions.EnumToDictionary<WirelessConnectionState>() });
        values.Add(new() { Name = IPAddress, DefaultValue = "0.0.0.0", ValueType = ParameterValueTypes.String });
        values.Add(new() { Name = ClientNetworkName, ValueType = ParameterValueTypes.String });
    }
}


