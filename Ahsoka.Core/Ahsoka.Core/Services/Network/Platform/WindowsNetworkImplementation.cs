using Ahsoka.ServiceFramework;
using SimpleWifi.Win32;
using SimpleWifi.Win32.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Ahsoka.Services.Network.Platform;

// Exclude since we can't use wifi hardware on build servers.
[ExcludeFromCodeCoverage]
internal class WindowsNetworkImplementation : NetworkPlatformImplementationBase
{
    WlanClient client;
    readonly Dictionary<Guid, WlanInterface> interfaceList = new();
    readonly AutoResetEvent connectionListenerCompleted = new(false);

    protected override List<AdapterInfo> GetAdapterList()
    {
        var adapterList = new List<AdapterInfo>();

        client = new WlanClient();
        if (client.Interfaces != null)
            foreach (var item in client.Interfaces)
            {
                interfaceList[item.InterfaceGuid] = item;

                if (item != null)
                {
                    adapterList.Add(new AdapterInfo()
                    {
                        AdapterType = NetworkAdapterType.WifiClient,
                        Description = item.InterfaceDescription,
                        AdapterId = item.InterfaceGuid.ToString()
                    });
                }
            }

        return adapterList;
    }

    protected override void OnEnableAdapter(AdapterInfo adapter, AdapterConfiguration configuration)
    {
        adapter.CurrentState = AdapterState.Enabled;

        Thread.Sleep(5000); // Simulate Slow Adapter Enable
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
        var wifi = interfaceList[Guid.Parse(adapterId)];

        Dictionary<string, WirelessNetworkInfo> networkInfoList = new();
        wifi.Scan();
        foreach (var item in wifi.GetAvailableNetworkList(0))
        {
            var net = new WirelessNetworkInfo
            {
                Ssid = UTF8Encoding.UTF8.GetString(item.dot11Ssid.SSID).Trim('\0'),
                SignalLevel = (int)item.wlanSignalQuality,
                State = item.flags.HasFlag(WlanAvailableNetworkFlags.Connected) ?
                                        WirelessConnectionState.ClientConnected :
                                        WirelessConnectionState.ClientDisconnected,
                SecurityType = item.dot11DefaultAuthAlgorithm switch
                {
                    SimpleWifi.Win32.Interop.Dot11AuthAlgorithm.IEEE80211_Open => SecurityType.NoSecurity,
                    SimpleWifi.Win32.Interop.Dot11AuthAlgorithm.RSNA_PSK or SimpleWifi.Win32.Interop.Dot11AuthAlgorithm.WPA_PSK => SecurityType.Wpa2,
                    SimpleWifi.Win32.Interop.Dot11AuthAlgorithm.WPA => SecurityType.Wpa2,
                    _ => SecurityType.NotSupportedType,
                }
            };
            if (!String.IsNullOrEmpty(net.Ssid))
            {
                if (!networkInfoList.ContainsKey(net.Ssid))
                    networkInfoList[net.Ssid] = net;
                else if (net.State == WirelessConnectionState.ClientConnected) // Update for Mesh Networks
                {
                    networkInfoList[net.Ssid].State = WirelessConnectionState.ClientConnected;
                }
            }
        }
        var returnValue = new WirelessNetworkInfoList()
        {
            AdapterId = adapterId
        };

        returnValue.Networks.AddRange(networkInfoList.Values);

        return returnValue;
    }

    protected override NetworkProfilesList OnGetProfiles(ProfileRequest profileRequest)
    {
        NetworkProfilesList networkList = new() { AdapterId = profileRequest.AdapterId };

        if (!profileRequest.UseOSProfiles)
            return NetworkProfileHelper.GetNetworkProfiles(profileRequest.AdapterId);

        // Windows Profiles..if needed.
        var connection = interfaceList[Guid.Parse(profileRequest.AdapterId)];
        if (connection != null)
        {
            // Setup Profile List
            foreach (var profile in connection.GetProfiles())
            {
                string info = connection.GetProfileXml(profile.profileName);
                var authentication = XDocument.Parse(info).Root.Descendants().First(x => x.Name.LocalName == "authentication").Value;
                var encryption = XDocument.Parse(info).Root.Descendants().First(x => x.Name.LocalName == "encryption").Value;

                networkList.NetworkProfiles.Add(new WirelessConnectionRequest()
                {
                    Remember = true,
                    Ssid = profile.profileName,
                    AdapterId = profileRequest.AdapterId,
                    PassKey = String.Empty,
                    Encrypted = false
                });
            }
        }

        return networkList;
    }

    protected override void OnSetProfiles(NetworkProfilesList profilesList)
    {
        NetworkProfileHelper.SetNetworkProfiles(profilesList);
    }

    protected override void OnConnect(NetworkService.AhsokaServiceRequest header, WirelessConnectionRequest connectionRequest)
    {
        var adapter = interfaceList[Guid.Parse(connectionRequest.AdapterId)];

        var profiles = adapter.GetProfiles();
        if (Array.Exists<WlanProfileInfo>(profiles, x => x.profileName == connectionRequest.Ssid))
            ManageConnection(header, adapter, connectionRequest);
    }

    protected override void OnDisconnect(NetworkService.AhsokaServiceRequest header, string adapterId)
    {
        var adapter = interfaceList[Guid.Parse(adapterId)];

        adapter.Disconnect();

        NetworkService.SendConnectionStateNotification(header, WirelessConnectionState.ClientDisconnected, adapterId, String.Empty);
    }

    private void ManageConnection(NetworkService.AhsokaServiceRequest header, WlanInterface adapter, WirelessConnectionRequest connectionRequest)
    {
        // Wire up a connection Listener
        adapter.WlanConnectionNotification -= ConnectionListener;

        // Connection Listener.  Listens while connected to the requested network
        void ConnectionListener(WlanNotificationData notifyData, WlanConnectionNotificationData connNotifyData)
        {
            var state = GetNotificationState(notifyData, connNotifyData);
            if (state != WirelessConnectionState.ClientConnected)
                NetworkService.SendConnectionStateNotification(header, state, connectionRequest.AdapterId, connectionRequest.Ssid);
        };

        // Connect and 
        adapter.WlanConnectionNotification += ConnectionListener;
        bool result = adapter.ConnectSynchronously(WlanConnectionMode.Profile, Dot11BssType.Any, connectionRequest.Ssid, connectionRequest.WaitForConnectionSeconds * 1000);

        // Add Profile After Connection Succeeds
        if (result)
        {
            if (connectionRequest.Remember)
                NetworkProfileHelper.AddNetworkProfile(connectionRequest);

            bool ipResult = WaitForIPAddress(header, connectionRequest);

            bool timeSyncResult = WaitForTimeSync(header, connectionRequest);
        }

        NetworkService.SendConnectionStateNotification(header, result ? WirelessConnectionState.ClientConnected : WirelessConnectionState.ClientConnectionFailed, connectionRequest.AdapterId, connectionRequest.Ssid);
    }

    private WirelessConnectionState GetNotificationState(WlanNotificationData notifyData, WlanConnectionNotificationData connNotifyData)
    {
        var state = WirelessConnectionState.ClientReady;

        if (notifyData.NotificationCode is WlanNotificationCodeMsm msmCode)
        {
            switch (msmCode)
            {
                case WlanNotificationCodeMsm.Authenticating:
                    state = WirelessConnectionState.ClientIdentifying;
                    break;

                case WlanNotificationCodeMsm.Connected:
                case WlanNotificationCodeMsm.RoamingStart:
                    state = WirelessConnectionState.ClientConnected;
                    break;

                case WlanNotificationCodeMsm.Disconnected:
                    state = WirelessConnectionState.ClientDisconnected;
                    break;

                default:
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"   MSM:{notifyData.NotificationCode}");
                    break;
            }
        }
        if (notifyData.NotificationCode is WlanNotificationCodeAcm asmCode)
        {
            switch (asmCode)
            {
                case WlanNotificationCodeAcm.ConnectionStart:
                    state = WirelessConnectionState.ClientConnecting;
                    break;

                case WlanNotificationCodeAcm.ConnectionComplete:
                    state = WirelessConnectionState.ClientConnected;
                    break;

                case WlanNotificationCodeAcm.ConnectionAttemptFail:
                    state = WirelessConnectionState.ClientConnectionFailed;
                    break;

                case WlanNotificationCodeAcm.Disconnected:
                    state = WirelessConnectionState.ClientDisconnected;
                    break;

                case WlanNotificationCodeAcm.Disconnecting:
                    state = WirelessConnectionState.ClientDisconnecting;
                    break;

                default:
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"   ACM:{notifyData.NotificationCode}");
                    break;
            }
        }

        Debug.WriteLine($"     GNS:{state}");

        return state;

    }

    protected override bool WaitForTimeSync(NetworkService.AhsokaServiceRequest messageHeader, WirelessConnectionRequest connectionRequest)
    {
        NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientWaitingForTime, connectionRequest.AdapterId, connectionRequest.Ssid);
        return true;
    }
}
