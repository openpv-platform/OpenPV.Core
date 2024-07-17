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

        UpdateCacheValue(NetworkServiceParameters.Available, true);
    }

    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<NetworkMessageTypes.Ids>())
        {
            case NetworkMessageTypes.Ids.GetAdapters:
                GetAdapters(messageHeader);
                break;

            case NetworkMessageTypes.Ids.GetAdapterConfiguration:
                GetAdapterConfiguration(messageHeader, message as AdapterRequest);
                break;

            case NetworkMessageTypes.Ids.SetAdapterConfiguration:
                SetAdapterConfiguration(messageHeader, message as AdapterConfiguration);
                break;

            case NetworkMessageTypes.Ids.EnableAdapter:
                EnableAdapter(messageHeader, message as AdapterRequest);
                break;

            case NetworkMessageTypes.Ids.DisableAdapter:
                DisableAdapter(messageHeader, message as AdapterRequest);
                break;

            case NetworkMessageTypes.Ids.DiscoverNetworks:
                Discover(messageHeader, message as AdapterRequest);
                break;

            case NetworkMessageTypes.Ids.GetNetworkProfiles:
                GetNetworkProfiles(messageHeader, message as ProfileRequest);
                break;

            case NetworkMessageTypes.Ids.SetNetworkProfiles:
                SetNetworkProfiles(messageHeader, message as NetworkProfilesList);
                break;

            case NetworkMessageTypes.Ids.ConnectToNetwork:
                ConnectToNetwork(messageHeader, message as WirelessConnectionRequest);
                break;

            case NetworkMessageTypes.Ids.DisconnectNetwork:
                Disconnect(messageHeader, message as AdapterRequest);
                break;

            case NetworkMessageTypes.Ids.GetNetworkInfo:
                GetIpInfo(messageHeader, message as AdapterRequest);
                break;

            case NetworkMessageTypes.Ids.AttemptAutoConnect:
                AttemptAutoConnect(messageHeader, message as AutoConnectRequest);
                break;
        }
    }

    private void AttemptAutoConnect(AhsokaMessageHeader messageHeader, AutoConnectRequest adapterRequest)
    {
        networkInterface.AttemptAutoConnect(messageHeader, adapterRequest);
        SendMessageInternal(messageHeader, new EmptyNotification());
    }

    private void GetIpInfo(AhsokaMessageHeader messageHeader, AdapterRequest adapterRequest)
    {
        var response = networkInterface.GetIPInfo(adapterRequest);
        SendMessageInternal(messageHeader, response);
    }

    private void EnableAdapter(AhsokaMessageHeader messageHeader, AdapterRequest adapterRequest)
    {
        // Send Empty Response to Ack.
        SendMessageInternal(messageHeader);

        // Execute Enable and Block
        var response = networkInterface.EnableAdapter(adapterRequest);

        // Send Async Ready Message
        var header = new AhsokaMessageHeader()
        {
            TransportId = NetworkMessageTypes.Ids.NetworkAdapterReady.EnumToInt()
        };

        SendMessageInternal(header, response, true);
    }

    private void DisableAdapter(AhsokaMessageHeader messageHeader, AdapterRequest adapterRequest)
    {
        var response = networkInterface.DisableAdapter(adapterRequest);
        SendMessageInternal(messageHeader, response);
    }

    [ExcludeFromCodeCoverage]
    private void ConnectToNetwork(AhsokaMessageHeader messageHeader, WirelessConnectionRequest connectionRequest)
    {
        networkInterface.Connect(messageHeader, connectionRequest);
        SendMessageInternal(messageHeader);
    }

    [ExcludeFromCodeCoverage]
    private void Disconnect(AhsokaMessageHeader messageHeader, AdapterRequest request)
    {
        networkInterface.Disconnect(messageHeader, request.AdapterId);
        SendMessageInternal(messageHeader);
    }

    private void GetNetworkProfiles(AhsokaMessageHeader messageHeader, ProfileRequest profileRequest)
    {
        var response = networkInterface.GetProfiles(profileRequest);
        SendMessageInternal(messageHeader, response);
    }

    private void SetNetworkProfiles(AhsokaMessageHeader messageHeader, NetworkProfilesList profileRequest)
    {
        networkInterface.SetProfiles(profileRequest);
        SendMessageInternal(messageHeader);
    }

    private void GetAdapters(AhsokaMessageHeader messageHeader)
    {
        var response = networkInterface.GetAdapters();
        SendMessageInternal(messageHeader, response);
    }

    private void GetAdapterConfiguration(AhsokaMessageHeader messageHeader, AdapterRequest request)
    {
        var response = networkInterface.GetAdapterConfiguration(request.AdapterId);
        SendMessageInternal(messageHeader, response);
    }

    private void SetAdapterConfiguration(AhsokaMessageHeader messageHeader, AdapterConfiguration adapterConfiguration)
    {
        var result = networkInterface.UpdateConfiguration(adapterConfiguration);
        SendMessageInternal(messageHeader, result);
    }

    private void Discover(AhsokaMessageHeader mesageHeader, AdapterRequest request)
    {
        var networks = networkInterface.Discover(request.AdapterId);
        SendMessageInternal(mesageHeader, networks);
    }

    [ExcludeFromCodeCoverage]
    internal void SendWirelessNetworkInfo(AhsokaMessageHeader messageHeader, WirelessNetworkInfo info)
    {
        var header = new AhsokaMessageHeader()
        {
            TransportId = NetworkMessageTypes.Ids.WirelessSignalChanged.EnumToInt()
        };

        SendMessageInternal(header, info, true);
    }

    [ExcludeFromCodeCoverage]
    internal void SendConnectionStateNotification(AhsokaMessageHeader messageHeader, WirelessConnectionState connectionResult, string adapterId, string ssid)
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

        SendMessageInternal(header, response, true);

        if (connectionResult is WirelessConnectionState.ClientConnected or WirelessConnectionState.ClientConnectionFailed)
            SendMessageInternal(messageHeader, new WirelessConnectionResponse() { AdapterId = adapterId, Ssid = ssid, State = connectionResult });

        //Notify Data Service of State
        UpdateCacheValue(NetworkServiceParameters.NetworkState, connectionResult.ToString());

        if (connectionResult is WirelessConnectionState.ClientConnected)
            UpdateCacheValue(NetworkServiceParameters.ClientNetworkName, ssid);
        else if (connectionResult is WirelessConnectionState.ClientConnectionFailed or WirelessConnectionState.ClientDisconnected)
        {
            UpdateCacheValue(NetworkServiceParameters.ClientNetworkName, string.Empty);
            UpdateCacheValue(NetworkServiceParameters.IPAddress, string.Empty);
        }

    }
    #endregion
}
