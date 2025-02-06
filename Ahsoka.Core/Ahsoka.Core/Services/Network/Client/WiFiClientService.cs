#pragma warning disable CS1591
using Ahsoka.Core;
using Ahsoka.Core.Utility;
using Ahsoka.Services.Network.Client.Implementation;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.Network;

[AhsokaService(Name)]
public class WiFiClientService : AhsokaServiceBase<WiFiClientMessageTypes.Ids>
{
    #region Fields
    readonly ImplementationBase networkInterface;
    public const string Name = "WiFiClientService";
    #endregion

    #region Methods
    internal static ImplementationBase GetInterop()
    {
        return SystemInfo.CurrentPlatform switch
        {
            PlatformFamily.Windows64 => new WindowsImplementation(),
            PlatformFamily.OpenViewLinux => new STImplementation(),
            PlatformFamily.OpenViewLinuxPro => new DesktopImplementation(),
            PlatformFamily.Ubuntu64 => new DesktopImplementation(),
            PlatformFamily.MacOSArm64 => new DesktopImplementation(),
            _ => throw new NotImplementedException(),
        };
    }

    public WiFiClientService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    public WiFiClientService(ServiceConfiguration config) :
        base(config, new WiFiClientServiceMessages())
    {
        using var stopwatch = new AhsokaStopwatch("Create NetworkService");

        networkInterface = GetInterop();
        stopwatch.ResetTiming("Interop Loaded");

        networkInterface.Init(this);
        stopwatch.StopTiming("Network Init Completed");

        UpdateCacheValue(WiFiClientServiceMessages.Available, true);
    }

    protected override void OnHandleServiceRequest(AhsokaServiceRequest request)
    {
        switch (request.TransportId)
        {
            case WiFiClientMessageTypes.Ids.GetAdapters:
                GetAdapterRequest(request);
                break;

            case WiFiClientMessageTypes.Ids.GetNetworkConfiguration:
                GetNetworkConfiguration(request, request.Message as AdapterRequest);
                break;

            case WiFiClientMessageTypes.Ids.SetNetworkConfiguration:
                SetNetworkConfiguration(request, request.Message as AdapterConfiguration);
                break;

            case WiFiClientMessageTypes.Ids.DiscoverNetworks:
                Discover(request, request.Message as AdapterRequest);
                break;

            case WiFiClientMessageTypes.Ids.GetNetworkProfiles:
                GetNetworkProfiles(request, request.Message as ProfileRequest);
                break;

            case WiFiClientMessageTypes.Ids.SetNetworkProfiles:
                SetNetworkProfiles(request, request.Message as NetworkProfilesList);
                break;

            case WiFiClientMessageTypes.Ids.ConnectToNetwork:
                ConnectToNetwork(request, request.Message as WirelessConnectionRequest);
                break;

            case WiFiClientMessageTypes.Ids.DisconnectNetwork:
                Disconnect(request, request.Message as AdapterRequest);
                break;

            case WiFiClientMessageTypes.Ids.GetNetworkInfo:
                GetIpInfo(request, request.Message as AdapterRequest);
                break;

            case WiFiClientMessageTypes.Ids.AttemptAutoConnect:
                AttemptAutoConnect(request, request.Message as AutoConnectRequest);
                break;
        }
    }

    private void GetAdapterRequest(AhsokaServiceRequest request)
    {
        // Send Empty Response to Ack.
        var result = networkInterface.GetAdapters();
        SendResponse(request, result);
    }

    private void AttemptAutoConnect(AhsokaServiceRequest messageHeader, AutoConnectRequest adapterRequest)
    {
        networkInterface.AttemptAutoConnect(messageHeader, adapterRequest);
        SendResponse(messageHeader, new EmptyNotification());
    }

    private void GetIpInfo(AhsokaServiceRequest messageHeader, AdapterRequest adapterRequest)
    {
        var response = ImplementationBase.GetIPInfo(adapterRequest);

        // Update IPAddress 
        this.UpdateCacheValue(WiFiClientServiceMessages.IPAddress, response.IpPAddress);

        SendResponse(messageHeader, response);
    }

    [ExcludeFromCodeCoverage]
    private void ConnectToNetwork(AhsokaServiceRequest messageHeader, WirelessConnectionRequest connectionRequest)
    {
        networkInterface.Connect(messageHeader, connectionRequest);
        SendResponse(messageHeader);
    }

    [ExcludeFromCodeCoverage]
    private void Disconnect(AhsokaServiceRequest messageHeader, AdapterRequest request)
    {
        networkInterface.Disconnect(messageHeader, request.AdapterId);
        SendResponse(messageHeader);
    }

    private void GetNetworkProfiles(AhsokaServiceRequest messageHeader, ProfileRequest profileRequest)
    {
        var response = networkInterface.GetProfiles(profileRequest);
        SendResponse(messageHeader, response);
    }

    private void SetNetworkProfiles(AhsokaServiceRequest messageHeader, NetworkProfilesList profileRequest)
    {
        networkInterface.SetProfiles(profileRequest);
        SendResponse(messageHeader);
    }

    private void GetNetworkConfiguration(AhsokaServiceRequest messageHeader, AdapterRequest request)
    {
        var response = networkInterface.GetAdapterConfiguration(request.AdapterId);
        SendResponse(messageHeader, response);
    }

    private void SetNetworkConfiguration(AhsokaServiceRequest messageHeader, AdapterConfiguration adapterConfiguration)
    {
        var result = networkInterface.UpdateConfiguration(adapterConfiguration);
        SendResponse(messageHeader, result);
    }

    private void Discover(AhsokaServiceRequest mesageHeader, AdapterRequest request)
    {
        var networks = networkInterface.Discover(request.AdapterId);
        SendResponse(mesageHeader, networks);
    }

    [ExcludeFromCodeCoverage]
    internal void SendWirelessNetworkInfo(AhsokaServiceRequest messageHeader, WirelessNetworkInfo info)
    {
        SendNotification(WiFiClientMessageTypes.Ids.WirelessSignalChanged, info);
    }

    [ExcludeFromCodeCoverage]
    internal void SendConnectionStateNotification(AhsokaServiceRequest messageHeader, WirelessConnectionState connectionResult, string adapterId, string ssid)
    {
        var response = new WirelessConnectionNotification()
        {
            AdapterId = adapterId,
            State = connectionResult,
            Ssid = ssid,

        };

        var header = new AhsokaMessageHeader()
        {
            TransportId = WiFiClientMessageTypes.Ids.ConnectionStateNotification.EnumToInt()
        };

        SendNotification(WiFiClientMessageTypes.Ids.ConnectionStateNotification, response);

        if (connectionResult is WirelessConnectionState.ClientConnected or WirelessConnectionState.ClientConnectionFailed)
            SendResponse(messageHeader, new WirelessConnectionResponse() { AdapterId = adapterId, Ssid = ssid, State = connectionResult });

        //Notify Data Service of State
        UpdateCacheValue(WiFiClientServiceMessages.NetworkState, connectionResult.ToString());

        if (connectionResult is WirelessConnectionState.ClientConnected)
            UpdateCacheValue(WiFiClientServiceMessages.ClientNetworkName, ssid);
        else if (connectionResult is WirelessConnectionState.ClientConnectionFailed or WirelessConnectionState.ClientDisconnected)
        {
            UpdateCacheValue(WiFiClientServiceMessages.ClientNetworkName, string.Empty);
            UpdateCacheValue(WiFiClientServiceMessages.IPAddress, string.Empty);
        }

    }
    #endregion
}
