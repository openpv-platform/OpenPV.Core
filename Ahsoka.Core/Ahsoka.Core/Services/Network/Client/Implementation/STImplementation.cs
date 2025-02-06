using Ahsoka.Core;
using Ahsoka.Services.Network.ESPWifi;
using Ahsoka.Services.Network.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Network.Client.Implementation;

[ExcludeFromCodeCoverage]
internal class STImplementation : ImplementationBase
{
    static readonly InteropList interopList = new();

    CancellationTokenSource tokenSource = null;
    readonly AutoResetEvent disconnectListenerCompleted = new(false);
    string adapterId = "ethsta0";

    protected override void OnStartNetwork(out AdapterConfiguration defaultConfiguration, out AdapterRequest defaultRequest)
    {
        defaultRequest = new AdapterRequest() { AdapterId = adapterId };
        defaultConfiguration = new() { AdapterId = adapterId, ClientConfiguration = new IPConfiguration() { ConnectionType = IPAddressConfiguration.Dhcp } };

        // Configure Adapter 
        interopList.Add(adapterId);
        using var connection = interopList.AquireInterop(adapterId);
        if (connection != null)
        {
            // Read Configurations if Already Saved Somewhere.
            // Look for Network File for Client
            NetworkServiceHelper.ReadClientConfiguration(defaultConfiguration);

            // Configure and Write Configuratoin
            ConfigureAdapter(connection, defaultConfiguration);
        }
    }

    void ConfigureAdapter(InteropLock connection, AdapterConfiguration updateConfiguration)
    {
        if (connection != null)
        {
            var value = WifiMode.WIFI_MODE_STA;

            string mac = connection.ESPInterop.WifiGetMAC(value);

            int tries = 0;
            while (string.IsNullOrEmpty(mac))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"WiFiClientService: Failed to Read Module - Retrying {tries}");
                mac = connection.ESPInterop.WifiGetMAC(value);
                Thread.Sleep(100);
                tries++;

                if (tries > 25) // 2.5s
                    throw new ApplicationException("WiFiClientService: Could not initialize WiFi Module");
            }

            updateConfiguration.OverrideMacAddress = mac;
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"WiFiClientService: Using built in MAC {mac}");

            // Set to Client Mode
            var mode = connection.ESPInterop.WifiGetMode();
            if (mode != WifiMode.WIFI_MODE_STA)
                connection.ESPInterop.WifiSetMode(WifiMode.WIFI_MODE_STA);

            // Configure Network SystemD for our System
            if (updateConfiguration.ClientConfiguration != null)
                NetworkServiceHelper.WriteClientConfiguration(updateConfiguration);
        }
    }

    protected override void OnUpdateConfiguration(AdapterConfiguration adapterConfiguration)
    {
        NetworkServiceHelper.WriteClientConfiguration(adapterConfiguration);
    }

    protected override WirelessNetworkInfoList OnGetDiscoveredNetworks(string adapterId)
    {
        Dictionary<string, WirelessNetworkInfo> networkInfoList = new();
        ESPHostedStationConfig config = new();

        List<ESPHostedWifiScanList> list = null;
        using (var wifi = interopList.AquireInterop(adapterId))
        {
            var result = wifi.ESPInterop.WifiGetConnectionStatus(ref config);
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"WiFiClientService: Start scanning for networks after InteropLock");
            list = wifi.ESPInterop.WifiStartScanning();
        }

        foreach (var item in list)
        {
            if (!string.IsNullOrEmpty(item.ssid))
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

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"WiFiClientService: Finished scanning for networks");
        return returnValue;
    }

    private static int GetQuality(int rssi)
    {
        return 2 * (rssi + 100);
    }

    protected override NetworkProfilesList OnGetProfiles(ProfileRequest profileRequest)
    {
        if (profileRequest.UseOsProfiles == true)
            throw new NotImplementedException();

        return NetworkProfileHelper.GetNetworkProfiles(profileRequest.AdapterId);
    }

    protected override void OnSetProfiles(NetworkProfilesList profilesList)
    {
        NetworkProfileHelper.SetNetworkProfiles(profilesList);
    }

    protected override void OnConnect(WiFiClientService.AhsokaServiceRequest messageHeader, WirelessConnectionRequest connectionRequest)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"WiFiClientService: Starting OnConnect after InteropLock - {connectionRequest.Ssid}");
        // Get Adapter
        using var adapter = interopList.AquireInterop(connectionRequest.AdapterId);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"WiFiClientService: Starting OnConnect after InteropLock - {connectionRequest.Ssid}");

        // Check if Remembered Network...if so always use that one.
        var existingRequest = NetworkProfileHelper.GetNetworkProfiles(connectionRequest.AdapterId).NetworkProfiles.FirstOrDefault(x => x.Ssid == connectionRequest.Ssid);
        if (existingRequest != null)
        {
            connectionRequest = existingRequest;
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"WiFiClientService: Logging in with Cached Profile for Network - {connectionRequest.Ssid}");
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
                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"WiFiClientService: OnConnect Failed - WaitForTimeSync Failed");
                }

                // Listen Indefinitely for Disconnect
                ListenForDisconnect(messageHeader, connectionRequest);
            }
            else
            {
                Disconnect(messageHeader, connectionRequest.AdapterId);
                NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientConnectionFailed, connectionRequest.AdapterId, connectionRequest.Ssid);
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"WiFiClientService: OnConnect Failed - WaitForIPAddress Failed");
            }
        }
        else
        {
            NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientConnectionFailed, connectionRequest.AdapterId, connectionRequest.Ssid);
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"WiFiClientService: OnConnect Failed - ESPInterop.WifiConnect Failed - {result}");
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"WiFiClientService: Finished OnConnect - {connectionRequest.Ssid}");
    }

    protected override void OnDisconnect(WiFiClientService.AhsokaServiceRequest header, string adapterId)
    {
        using var adapter = interopList.AquireInterop(adapterId);

        // Cancel Listener and Wait for exit
        if (tokenSource != null)
        {
            tokenSource.Cancel();
            disconnectListenerCompleted.WaitOne();
        }
        CommandResult result = adapter.ESPInterop.WifiDisconnect();
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"WiFiClientService: OnDisconnect {result}");
        if (result == CommandResult.Success)
            NetworkService.SendConnectionStateNotification(header, WirelessConnectionState.ClientDisconnected, adapterId, "");
        else
        {
            NetworkService.SendConnectionStateNotification(header, WirelessConnectionState.ClientConnectionFailed, adapterId, "");
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"WiFiClientService: OnDisconnect Failed - WifiDisconnect() {result}");
        }
    }

    protected override bool WaitForTimeSync(WiFiClientService.AhsokaServiceRequest messageHeader, WirelessConnectionRequest connectionRequest)
    {
        NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientWaitingForTime, connectionRequest.AdapterId, connectionRequest.Ssid);

        return LinuxNetworkUtilities.WaitForTimeSync(connectionRequest.WaitForTimeSyncSeconds);
    }

    #region Private Methods
    private void ListenForDisconnect(WiFiClientService.AhsokaServiceRequest header, WirelessConnectionRequest connectionRequest)
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
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"WiFiClientService: Acquiring Lock on {AdapterID}");
            Mutex.WaitOne();
        }

        void IDisposable.Dispose()
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"WiFiClientService: Releasing Lock on {AdapterID}");
            Mutex.ReleaseMutex();
        }

    }

    private class InteropList
    {
        static readonly Dictionary<string, InteropData> interfaceList = new();

        internal void Add(string adapterId)
        {
            interfaceList[adapterId] = new(adapterId);
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
