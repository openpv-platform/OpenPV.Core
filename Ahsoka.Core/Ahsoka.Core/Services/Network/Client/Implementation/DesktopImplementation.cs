using Ahsoka.Services.Network.Platform;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Ahsoka.Services.Network.Client.Implementation;

[ExcludeFromCodeCoverage]
internal class DesktopImplementation : ImplementationBase
{
    readonly CancellationTokenSource tokenSource = new();
    readonly AutoResetEvent disconnectListenerCompleted = new(false);
    string adapterId = Guid.NewGuid().ToString();

    protected override void OnStartNetwork(out AdapterConfiguration defaultConfiguration, out AdapterRequest defaultRequest)
    {
        // No Configuration Needed on Windows
        defaultConfiguration = new AdapterConfiguration() { AdapterId = adapterId, ClientConfiguration = new IPConfiguration() { ConnectionType = IPAddressConfiguration.Dhcp } };
        defaultRequest = new AdapterRequest() { AdapterId = adapterId };
    }

    protected override void OnUpdateConfiguration(AdapterConfiguration adapterConfiguration)
    {

    }

    protected override WirelessNetworkInfoList OnGetDiscoveredNetworks(string adapterId)
    {
        var returnValue = new WirelessNetworkInfoList()
        {
            AdapterId = adapterId
        };

        return returnValue;
    }

    protected override NetworkProfilesList OnGetProfiles(ProfileRequest profileRequest)
    {
        return NetworkProfileHelper.GetNetworkProfiles(profileRequest.AdapterId);
    }

    protected override void OnSetProfiles(NetworkProfilesList profilesList)
    {
        NetworkProfileHelper.SetNetworkProfiles(profilesList);
    }

    protected override void OnConnect(WiFiClientService.AhsokaServiceRequest messageHeader, WirelessConnectionRequest connectionRequest)
    {
        NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientConnecting, connectionRequest.AdapterId, "");
    }

    protected override void OnDisconnect(WiFiClientService.AhsokaServiceRequest header, string adapterId)
    {
        NetworkService.SendConnectionStateNotification(header, WirelessConnectionState.ClientDisconnected, adapterId, "");
    }

    protected override bool WaitForTimeSync(WiFiClientService.AhsokaServiceRequest messageHeader, WirelessConnectionRequest connectionRequest)
    {
        NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientWaitingForTime, connectionRequest.AdapterId, connectionRequest.Ssid);
        return true;
    }
}
