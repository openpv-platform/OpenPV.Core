using Ahsoka.ServiceFramework;
using Ahsoka.System;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Ahsoka.Services.Network.Platform;

[ExcludeFromCodeCoverage]
internal class MacOSNetworkImplementation : NetworkPlatformImplementationBase
{
    readonly CancellationTokenSource tokenSource = new();
    readonly AutoResetEvent disconnectListenerCompleted = new(false);

    protected override List<AdapterInfo> GetAdapterList()
    {
        // All Configurable Adapters are Statically Defined 
        return SystemInfo.HardwareInfo.NetworkInfo.AdapterInfoList;
    }

    protected override void OnEnableAdapter(AdapterInfo adapter, AdapterConfiguration configuration)
    {
        adapter.CurrentState = AdapterState.Enabled;
    }

    protected override void OnDisableAdapter(AdapterInfo adapter)
    {
        adapter.CurrentState = AdapterState.Disabled;
    }

    protected override void OnCreateAdapterConfiguration(AdapterConfiguration config, AdapterInfo item)
    {
        // No Configuration Needed on Windows
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
        if (profileRequest.UseOSProfiles == true)
            throw new NotImplementedException();

        return NetworkProfileHelper.GetNetworkProfiles(profileRequest.AdapterId);
    }

    protected override void OnSetProfiles(NetworkProfilesList profilesList)
    {
        NetworkProfileHelper.SetNetworkProfiles(profilesList);
    }

    protected override void OnConnect(AhsokaMessageHeader messageHeader, WirelessConnectionRequest connectionRequest)
    {
        NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientConnecting, connectionRequest.AdapterId, "");
    }

    protected override void OnDisconnect(AhsokaMessageHeader header, string adapterId)
    {
        NetworkService.SendConnectionStateNotification(header, WirelessConnectionState.ClientDisconnected, adapterId, "");
    }

    protected override bool WaitForTimeSync(AhsokaMessageHeader messageHeader, WirelessConnectionRequest connectionRequest)
    {
        NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientWaitingForTime, connectionRequest.AdapterId, connectionRequest.Ssid);
        return true;
    }
}
