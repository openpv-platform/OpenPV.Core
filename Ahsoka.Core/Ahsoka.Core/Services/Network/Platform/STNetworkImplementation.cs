using Ahsoka.ServiceFramework;
using Ahsoka.Services.Network.ESPWifi;
using Ahsoka.System;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Network.Platform;

[ExcludeFromCodeCoverage]
internal class STNetworkImplementation : NetworkPlatformImplementationBase
{
    static readonly InteropList interopList = new();

    CancellationTokenSource tokenSource = null;
    readonly AutoResetEvent disconnectListenerCompleted = new(false);
    INetworkFirmwareBase firmware;

    protected override List<AdapterInfo> GetAdapterList()
    {
        // All Configurable Adapters are Statically Defined 
        return SystemInfo.HardwareInfo.NetworkInfo.AdapterInfoList;
    }

    protected override void OnEnableAdapter(AdapterInfo adapter, AdapterConfiguration config)
    {
        // Don't create interop for Ethernet Devices
        if (adapter.AdapterType == NetworkAdapterType.WifiClient || 
            adapter.AdapterType == NetworkAdapterType.WifiAccessPoint)
        {
            //Set MAC, Bring UP
            using var connection = interopList.AquireInterop(adapter.AdapterId);

            if (connection != null)
            {
                if (adapter.Module is ModuleType.Esp32 or
                    ModuleType.None) // Handle Old ST Units.
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "STNetImp: Initializing ESP Firmware");
                    firmware = new ESPNetworkFirmware();
                    firmware.Init();
                }

                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "STNetImp: Booting ESP Module");
                firmware?.Boot();

                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "STNetImp: Configuring ESP Module");
                ConfigureAdapter(connection, config);

                if (config.AdapterMode == AdapterMode.Client)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "STNetImp: Starting Network Adapter Client");
                    LinuxNetworkUtilities.StartAdapter(adapter.AdapterId);
                }
            }
        }

        // Set Adapter State
        adapter.CurrentState = AdapterState.Enabled;
    }

    protected override void OnDisableAdapter(AdapterInfo adapter)
    {
        if (adapter.AdapterType == NetworkAdapterType.WifiClient ||
            adapter.AdapterType == NetworkAdapterType.WifiAccessPoint)
        {
            //Set MAC, Bring UP
            using var connection = interopList.AquireInterop(adapter.AdapterId);

            if (connection != null)
            {
                // Bring Down
                LinuxNetworkUtilities.StopAdapter(adapter.AdapterId);
                firmware?.Shutdown();
            }
        }

        // Set Adapter State
        adapter.CurrentState = AdapterState.Disabled;
    }

    protected override void OnCreateAdapterConfiguration(AdapterConfiguration config, AdapterInfo item)
    {
        // Don't create interop for Ethernet Devices
        if (config.AdapterMode == AdapterMode.NoMode)
            return;

        interopList.Add(item);

        // Configure Adapter 
        using var connection = interopList.AquireInterop(config.AdapterId);

        if (connection != null)
        {
            // Read Configurations if Already Saved Somewhere.
            if (config.AdapterMode == AdapterMode.Client)
            {
                // Look for Network File for Client
                NetworkServiceHelper.ReadClientConfiguration(config);
            }
            else
            {
                // Look for Network File for Client
                NetworkServiceHelper.ReadAPConfiguration(config);
            }
        }
    }

    void ConfigureAdapter(InteropLock connection, AdapterConfiguration updateConfiguration)
    {
        if (connection != null)
        {
            var value = WifiMode.WIFI_MODE_STA;

            if (updateConfiguration.AdapterMode == AdapterMode.AccessPoint)
                value = WifiMode.WIFI_MODE_AP;

            string mac = connection.ESPInterop.WifiGetMAC(value);
            int tries = 0;
            while (String.IsNullOrEmpty(mac))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"STNetImp: Failed to Read Module - Retrying {tries}");
                mac = connection.ESPInterop.WifiGetMAC(value);
                Thread.Sleep(100);
                tries++;

                if (tries > 25) // 2.5s
                    throw new ApplicationException("STNetImp: Could not initialize WiFi Module");
            }

            updateConfiguration.OverrideMacAddress = mac;
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"STNetImp: Using built in MAC {mac}");

            // Set new Mode.
            var mode = (AdapterMode)connection.ESPInterop.WifiGetMode();
            if (mode != updateConfiguration.AdapterMode)
                connection.ESPInterop.WifiSetMode((WifiMode)updateConfiguration.AdapterMode);

            // Configure Network SystemD for our System
            if (updateConfiguration.AdapterMode.HasFlag(AdapterMode.Client))
                if (updateConfiguration.ClientConfiguration != null)
                {
                    // Enable Station Mode
                    CommandResult result = connection.ESPInterop.WifiSetMode(WifiMode.WIFI_MODE_STA);
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"STNetImp: Set WiFi mode to STA ({result})");

                    NetworkServiceHelper.WriteClientConfiguration(updateConfiguration);
                }

            // Client is configured during Connection Initiation.
            if (updateConfiguration.AdapterMode.HasFlag(AdapterMode.AccessPoint))
                if (updateConfiguration.AccessPointConfiguration != null)
                {
                    // Enable AP Mode
                    CommandResult result = connection.ESPInterop.WifiSetMode(WifiMode.WIFI_MODE_AP);
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"STNetImp: Set WiFi mode to AP ({result})");

                    ESPHostedSoftAPConfig config = new ESPHostedSoftAPConfig()
                    {
                        ssidHidden = false,
                        MaxConnections = 10,
                        bandwidth = WifiBandwidth.WIFI_BANDWIDTH_HT20,
                        ssid = updateConfiguration.AccessPointConfiguration.Ssid,
                        encryptionMode = (int)WifiSupportedEncryptionMode.WIFI_AP_AUTH_WPA_WPA2_PSK,
                        password = updateConfiguration.AccessPointConfiguration.EncryptionPassKey,
                        channel = updateConfiguration.AccessPointConfiguration.ChannelNumber
                    };

                    connection.ESPInterop.WifiSetAPConfiguration(config);
                    connection.ESPInterop.WifiGetAPConfiguration(ref config);
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium,"Current AP Config:");
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"  SSID: {config.ssid}");
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"  SSID Hidden? {config.ssidHidden}");
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"  Password: {config.password}");
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"  Channel: {config.channel}");
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"  Bandwidth: {config.bandwidth}");
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"  Encryption Mode: {config.encryptionMode}");
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium,"");
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Low, config.ToString());

                    NetworkServiceHelper.WriteAccessPointConfiguration(updateConfiguration);
                }
        }
    }

    protected override WirelessNetworkInfoList OnGetDiscoveredNetworks(string adapterId)
    {
        Dictionary<string, WirelessNetworkInfo> networkInfoList = new();
        ESPHostedStationConfig config = new();

        List<ESPHostedWifiScanList> list = null;
        using (var wifi = interopList.AquireInterop(adapterId))
        {
            var result = wifi.ESPInterop.WifiGetConnectionStatus(ref config);
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"STNetImp: Start scanning for networks after InteropLock");
            list = wifi.ESPInterop.WifiStartScanning();
        }
        foreach (var item in list)
        {
            if (!String.IsNullOrEmpty(item.ssid))
            {
                var mode = (WifiEncryptionMode)item.encryptionMode;
                var securityType = mode switch
                {
                    WifiEncryptionMode.WIFI_AUTH_WPA2_PSK or WifiEncryptionMode.WIFI_AUTH_WPA_WPA2_PSK or WifiEncryptionMode.WIFI_AUTH_WPA3_PSK or WifiEncryptionMode.WIFI_AUTH_WPA2_WPA3_PSK => SecurityType.Wpa2,
                    _ => SecurityType.NoSecurity,
                };

                var net = new WirelessNetworkInfo()
                {
                    Ssid = item.ssid,
                    SignalLevel = GetQuality(item.rssi), // Translate from RSSI to "Quality" 
                    SecurityType = securityType,
                };

                if (networkInfoList.TryGetValue(net.Ssid, out WirelessNetworkInfo netItem))
                {
                    netItem.SignalLevel = netItem.SignalLevel < net.SignalLevel ? net.SignalLevel : netItem.SignalLevel;
                }
                else
                {
                    networkInfoList[net.Ssid] = net;
                }
            }
        }

        // Update Connected Status if Connected
        if (config.status == "success")
        {
            // Trim Garbage
            string connectedSSID = new string(config.ssid.Where(c => !char.IsControl(c)).ToArray());
            if (networkInfoList.TryGetValue(connectedSSID, out WirelessNetworkInfo netItem))
                netItem.State = WirelessConnectionState.ClientConnected;
        }

        var returnValue = new WirelessNetworkInfoList()
        {
            AdapterId = adapterId
        };

        returnValue.Networks.AddRange(networkInfoList.Values);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"STNetImp: Finished scanning for networks");
        return returnValue;
    }

    private static int GetQuality(int rssi)
    {
        return 2 * (rssi + 100);
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

    protected override void OnConnect(NetworkService.AhsokaServiceRequest messageHeader, WirelessConnectionRequest connectionRequest)
    {
        // Get Configuration and Check that we are a Client
        var configuration = GetAdapterConfiguration(connectionRequest.AdapterId);
        if (configuration.AdapterMode == AdapterMode.AccessPoint)
        {
            NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientConnectionFailed, connectionRequest.AdapterId, connectionRequest.Ssid);
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"STNetImp: OnConnect Failed - AdapterMode is in AccessPoint mode");
        }

        // Get Adapter
        using var adapter = interopList.AquireInterop(connectionRequest.AdapterId);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"STNetImp: Starting OnConnect after InteropLock - {connectionRequest.Ssid}");

        // Check if Remembered Network...if so always use that one.
        var existingRequest = NetworkProfileHelper.GetNetworkProfiles(connectionRequest.AdapterId).NetworkProfiles.FirstOrDefault(x=>x.Ssid == connectionRequest.Ssid);
        if (existingRequest != null)
        {
            connectionRequest = existingRequest;
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"STNetImp: Logging in with Cached Profile for Network - {connectionRequest.Ssid}");
        }

        var networkDecrypted = NetworkProfileHelper.DecryptNetwork(connectionRequest);

        // Build Connection
        ESPHostedStationConfig apConfig = new()
        {
            ssid = networkDecrypted.Ssid,
            password = networkDecrypted.PassKey,
            listenInterval = 5, // ???
            wpa3Supported = false // ???
        };

        // Notify Client
        NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientConnecting, connectionRequest.AdapterId, connectionRequest.Ssid);

        // Issue Connect
        ConnectResult result = adapter.ESPInterop.WifiConnect(apConfig);
        if (result == ConnectResult.Success)
        {
            // Notify Authenticating
            NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientIdentifying, connectionRequest.AdapterId, connectionRequest.Ssid);

            // Save Profile
            if (connectionRequest.Remember)
                NetworkProfileHelper.AddNetworkProfile(connectionRequest);

            LinuxNetworkUtilities.AdapterRequestDHCPAddress(connectionRequest.AdapterId);

            bool ipAddressFound = WaitForIPAddress(messageHeader, connectionRequest);

            // Did we Get an IP?
            if (ipAddressFound)
            {
                // Wait for Time Sync
                bool timeSync = WaitForTimeSync(messageHeader, connectionRequest);
                if (timeSync)
                    NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientConnected, connectionRequest.AdapterId, connectionRequest.Ssid);
                else
                {
                    Disconnect(messageHeader, connectionRequest.AdapterId);
                    NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientConnectionFailed, connectionRequest.AdapterId, connectionRequest.Ssid);
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"STNetImp: OnConnect Failed - WaitForTimeSync Failed");
                }

                // Listen Indefinitely for Disconnect
                ListenForDisconnect(messageHeader, connectionRequest);
            }
            else
            {
                Disconnect(messageHeader, connectionRequest.AdapterId);
                NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientConnectionFailed, connectionRequest.AdapterId, connectionRequest.Ssid);
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"STNetImp: OnConnect Failed - WaitForIPAddress Failed");
            }
        }
        else
        {
            NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientConnectionFailed, connectionRequest.AdapterId, connectionRequest.Ssid);
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"STNetImp: OnConnect Failed - ESPInterop.WifiConnect Failed - {result}");
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"STNetImp: Finished OnConnect - {connectionRequest.Ssid}");
    }

    protected override void OnDisconnect(NetworkService.AhsokaServiceRequest header, string adapterId)
    {
        using var adapter = interopList.AquireInterop(adapterId);

        // Cancel Listener and Wait for exit
        if (tokenSource != null)
        {
            tokenSource.Cancel();
            disconnectListenerCompleted.WaitOne();
        }
        CommandResult result = adapter.ESPInterop.WifiDisconnect();
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"OnDisconnect: Disconnect {result}");
        if (result == CommandResult.Success)
            NetworkService.SendConnectionStateNotification(header, WirelessConnectionState.ClientDisconnected, adapterId, "");
        else
        {
            NetworkService.SendConnectionStateNotification(header, WirelessConnectionState.ClientConnectionFailed, adapterId, "");
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"STNetImp: OnDisconnect Failed - WifiDisconnect() {result}");
        }
    }

    protected override bool WaitForTimeSync(NetworkService.AhsokaServiceRequest messageHeader, WirelessConnectionRequest connectionRequest)
    {
        NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientWaitingForTime, connectionRequest.AdapterId, connectionRequest.Ssid);

        return LinuxNetworkUtilities.WaitForTimeSync(connectionRequest.WaitForTimeSyncSeconds);
    }

    #region Private Methods
    private void ListenForDisconnect(NetworkService.AhsokaServiceRequest header, WirelessConnectionRequest connectionRequest)
    {
        // Create New TokenSource
        tokenSource = new CancellationTokenSource();

        // Run Task to Listen to Network Settings
        var task = Task.Run(() =>
        {
            ESPHostedStationConfig config = new();

            while (tokenSource != null && !tokenSource.IsCancellationRequested)
            {
                CommandResult result = CommandResult.Failure;
                using (var connection = interopList.AquireInterop(connectionRequest.AdapterId))
                    result = connection.ESPInterop.WifiGetConnectionStatus(ref config);

                if (result == CommandResult.Success)
                {
                    NetworkService.SendWirelessNetworkInfo(header, new WirelessNetworkInfo()
                    {
                        Ssid = connectionRequest.Ssid,
                        State = WirelessConnectionState.ClientConnectionSignalChanged,
                        SignalLevel = GetQuality(config.rssi),
                    });

                    if (config.status != "success")
                    {
                        NetworkService.SendConnectionStateNotification(header, WirelessConnectionState.ClientDisconnected, connectionRequest.AdapterId, connectionRequest.Ssid);
                        break;
                    }
                }
                else
                {
                    if (config.status == "not_connected") // too far from AP or AP shut off
                    {
                        // Send Notification.
                        NetworkService.SendConnectionStateNotification(header, WirelessConnectionState.ClientDisconnected, connectionRequest.AdapterId, connectionRequest.Ssid);
                    }
                    else
                    {
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "ESP Comms Error");
                    }
                    break; // Break if Error
                }

                Thread.Sleep(1000);
            }

            tokenSource = null;

            // Shutdown Completed
            disconnectListenerCompleted.Set();

        }, tokenSource.Token);

    }
    #endregion

    private class InteropData
    {
        internal ESPInterop ESPInterop { get; set; }
        protected Mutex Mutex { get; set; }
        protected string AdapterID { get; set; }

        protected InteropData() { }

        internal InteropData(string adapterId)
        {
            ESPInterop = new ESPInterop();
            AdapterID = adapterId;
            Mutex = new Mutex(false, $"ESPMutex_{AdapterID}");
        }

        public InteropLock CreateLock()
        {
            return new InteropLock()
            {
                AdapterID = AdapterID,
                ESPInterop = ESPInterop,
                Mutex = Mutex
            };
        }
    }

    private class InteropLock : InteropData, IDisposable
    {
        internal void Lock()
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"STNetImp: Acquiring Lock on {AdapterID}");
            Mutex.WaitOne();
        }

        void IDisposable.Dispose()
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"STNetImp: Releasing Lock on {AdapterID}");
            Mutex.ReleaseMutex();
        }

    }

    private class InteropList
    {
        static readonly Dictionary<string, InteropData> interfaceList = new();

        internal void Add(AdapterInfo item)
        {
            interfaceList[item.AdapterId] = new(item.AdapterId);
        }

        internal InteropLock AquireInterop(string adapterId)
        {
            var connection = interfaceList[adapterId];
            if (connection != null)
            {
                var interopLock = connection.CreateLock();
                interopLock.Lock();
                return interopLock;
            }

            return null;
        }
    }

}
