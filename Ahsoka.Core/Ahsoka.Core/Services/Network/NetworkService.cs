#pragma warning disable CS1591
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Network.Platform;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.Network;

[AhsokaService(Name)]
public class NetworkService : AhsokaServiceBase<NetworkMessageTypes.Ids>
{
    #region Fields
    readonly NetworkPlatformImplementationBase networkInterface;
    public const string Name = "NetworkService";
    #endregion

    #region Methods
    internal static NetworkPlatformImplementationBase GetInterop()
    {
        return SystemInfo.CurrentPlatform switch
        {
            PlatformFamily.Windows64 => new WindowsNetworkImplementation(),
            PlatformFamily.OpenViewLinux => new STNetworkImplementation(),
            PlatformFamily.OpenViewLinuxPro => new UbuntuNetworkImplementation(), // Temp
            PlatformFamily.Ubuntu64 => new UbuntuNetworkImplementation(),
            PlatformFamily.MacosArm64 => new MacOSNetworkImplementation(),
            _ => throw new NotImplementedException(),
        };
    }

    public NetworkService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    public NetworkService(ServiceConfiguration config) :
        base(config, new NetworkServiceMessages())
    {
        using var stopwatch = new AhsokaStopwatch("Create NetworkService");

        networkInterface = GetInterop();
        stopwatch.ResetTiming("Interop Loaded");

        networkInterface.Init(this);
        stopwatch.StopTiming("Network Init Completed");

        UpdateCacheValue(NetworkServiceMessages.Available, true);
    }

    protected override void OnHandleServiceRequest(AhsokaServiceRequest request)
    {
        switch (request.TransportId)
        {
            case NetworkMessageTypes.Ids.GetAdapters:
                GetAdapters(request);
                break;

            case NetworkMessageTypes.Ids.GetAdapterConfiguration:
                GetAdapterConfiguration(request, request.Message as AdapterRequest);
                break;

            case NetworkMessageTypes.Ids.SetAdapterConfiguration:
                SetAdapterConfiguration(request, request.Message as AdapterConfiguration);
                break;

            case NetworkMessageTypes.Ids.EnableAdapter:
                EnableAdapter(request, request.Message as AdapterRequest);
                break;

            case NetworkMessageTypes.Ids.DisableAdapter:
                DisableAdapter(request, request.Message as AdapterRequest);
                break;

            case NetworkMessageTypes.Ids.DiscoverNetworks:
                Discover(request, request.Message as AdapterRequest);
                break;

            case NetworkMessageTypes.Ids.GetNetworkProfiles:
                GetNetworkProfiles(request, request.Message as ProfileRequest);
                break;

            case NetworkMessageTypes.Ids.SetNetworkProfiles:
                SetNetworkProfiles(request, request.Message as NetworkProfilesList);
                break;

            case NetworkMessageTypes.Ids.ConnectToNetwork:
                ConnectToNetwork(request, request.Message as WirelessConnectionRequest);
                break;

            case NetworkMessageTypes.Ids.DisconnectNetwork:
                Disconnect(request, request.Message as AdapterRequest);
                break;

            case NetworkMessageTypes.Ids.GetNetworkInfo:
                GetIpInfo(request, request.Message as AdapterRequest);
                break;

            case NetworkMessageTypes.Ids.AttemptAutoConnect:
                AttemptAutoConnect(request, request.Message as AutoConnectRequest);
                break;
        }
    }

    private void AttemptAutoConnect(AhsokaServiceRequest messageHeader, AutoConnectRequest adapterRequest)
    {
        networkInterface.AttemptAutoConnect(messageHeader, adapterRequest);
        SendResponse(messageHeader, new EmptyNotification());
    }

    private void GetIpInfo(AhsokaServiceRequest messageHeader, AdapterRequest adapterRequest)
    {
        var response = networkInterface.GetIPInfo(adapterRequest);
        SendResponse(messageHeader, response);
    }

    private void EnableAdapter(AhsokaServiceRequest messageHeader, AdapterRequest adapterRequest)
    {
        // Send Empty Response to Ack.
        SendResponse(messageHeader);

        // Execute Enable and Block
        var response = networkInterface.EnableAdapter(adapterRequest);

        // Send Async Ready Message
        SendNotification(NetworkMessageTypes.Ids.NetworkAdapterReady, response);
    }

    private void DisableAdapter(AhsokaServiceRequest messageHeader, AdapterRequest adapterRequest)
    {
        var response = networkInterface.DisableAdapter(adapterRequest);
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

    private void GetAdapters(AhsokaServiceRequest messageHeader)
    {
        var response = networkInterface.GetAdapters();
        SendResponse(messageHeader, response);
    }

    private void GetAdapterConfiguration(AhsokaServiceRequest messageHeader, AdapterRequest request)
    {
        var response = networkInterface.GetAdapterConfiguration(request.AdapterId);
        SendResponse(messageHeader, response);
    }

    private void SetAdapterConfiguration(AhsokaServiceRequest messageHeader, AdapterConfiguration adapterConfiguration)
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
        SendNotification(NetworkMessageTypes.Ids.WirelessSignalChanged, info);
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
            TransportId = NetworkMessageTypes.Ids.ConnectionStateNotification.EnumToInt()
        };

        SendNotification(NetworkMessageTypes.Ids.ConnectionStateNotification, response);

        if (connectionResult is WirelessConnectionState.ClientConnected or WirelessConnectionState.ClientConnectionFailed)
            SendResponse(messageHeader, new WirelessConnectionResponse() { AdapterId = adapterId, Ssid = ssid, State = connectionResult });

        //Notify Data Service of State
        UpdateCacheValue(NetworkServiceMessages.NetworkState, connectionResult.ToString());

        if (connectionResult is WirelessConnectionState.ClientConnected)
            UpdateCacheValue(NetworkServiceMessages.ClientNetworkName, ssid);
        else if (connectionResult is WirelessConnectionState.ClientConnectionFailed or WirelessConnectionState.ClientDisconnected)
        {
            UpdateCacheValue(NetworkServiceMessages.ClientNetworkName, string.Empty);
            UpdateCacheValue(NetworkServiceMessages.IPAddress, string.Empty);
        }

    }
    #endregion
}
