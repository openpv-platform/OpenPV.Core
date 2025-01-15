using Ahsoka.Dispatch;
using Ahsoka.Installer.InstallEngine;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device;
using Ahsoka.Services.Network;
using Gst.Base;
using Mono.Unix.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Ahsoka.Utility.WiFi;

/// <summary>
/// WiFi Network Manager which will handle network management including 
/// enumerating available networks, connectiong to networks, maintaining profiles
/// and sending events.  Supports Dispatcher if passed to constructor.
/// </summary>
// This can't currently be tested on Windows Build Server
[ExcludeFromCodeCoverage]
public sealed class WiFiNetworkManager
{
    #region Fields
    DeviceServiceClient deviceClient;
    WiFiClientServiceClient wifiClient;
    Dispatcher dispatcher;
    AdapterRequest defaultAdapterRequest;
    readonly List<WirelessConnectionRequest> networkProfilesList = new();
    int selectedIndex;
    #endregion

    #region Events
    /// <summary>
    /// The List of Networks has been Updated
    /// </summary>
    public event EventHandler AdapterEnabledChanged;

    /// <summary>
    /// The List of Networks has been Updated
    /// </summary>
    public event EventHandler NetworkListChanged;

    /// <summary>
    /// The Connection State has Changed
    /// </summary>
    public event EventHandler<WirelessConnectionState> ConnectionStateChanged;

    /// <summary>
    /// Selected Network In List Changed
    /// </summary>
    public event EventHandler<WirelessNetworkInfo> SelectedNetworkChanged;
    #endregion

    #region Properties
    /// <summary>
    /// Indicates if the Adapter is Enabled
    /// </summary>
    public bool AdapterEnabled
    {
        get; private set;
    }

    /// <summary>
    /// Current Connection State
    /// </summary>
    public WirelessConnectionState ConnectionState
    {
        get; private set;
    } = WirelessConnectionState.ClientDisconnected;

    /// <summary>
    /// Connected Network Information
    /// </summary>
    public WirelessNetworkInfo ConnectedNetwork
    {
        get; private set;
    }

    /// <summary>
    /// Selected Network to Connect
    /// </summary>
    public WirelessNetworkInfo SelectedNetwork
    {
        get
        {
            if (selectedIndex >= 0 && selectedIndex < AvailableNetworks.Count)
            {
                return AvailableNetworks[selectedIndex];
            }
            else if (AvailableNetworks.Count > 0)
            {
                selectedIndex = 0;
                return AvailableNetworks.First();
            }

            return new WirelessNetworkInfo() { Ssid = "No Networks Found", SignalLevel = 0 };
        }
        set
        {
            if (value != null)
            {
                selectedIndex = AvailableNetworks.IndexOf(value);
                SelectedNetworkChanged?.Invoke(this, SelectedNetwork);
            }
        }
    }

    /// <summary>
    /// Currently Selected Network Profile
    /// </summary>
    public WirelessConnectionRequest SelectedNetworkProfile
    {
        get
        {
            return networkProfilesList.FirstOrDefault(x => x.Ssid == SelectedNetwork?.Ssid);
        }
    }

    /// <summary>
    /// List of Avaialble Networks discovered in Last Refresh 
    /// </summary>
    public List<WirelessNetworkInfo> AvailableNetworks { get; private set; } = new();
    #endregion

    /// <summary>
    /// Network Manager
    /// </summary>
    public WiFiNetworkManager()
    {

    }

    /// <summary>
    /// Initialize the WiFiManager
    /// </summary>
    /// <param name="wifiClient"></param>
    /// <param name="deviceClient"></param>
    /// <param name="dispatcher"></param>
    public void Start(DeviceServiceClient deviceClient = null, WiFiClientServiceClient wifiClient = null, Dispatcher dispatcher = null)
    {
        if (wifiClient == null)
            wifiClient = new WiFiClientServiceClient();

        this.wifiClient = wifiClient;

        if (deviceClient == null)
            deviceClient = new DeviceServiceClient();

        this.deviceClient = deviceClient;
        this.deviceClient.NotificationReceived += DeviceClient_NotificationReceived;

        this.dispatcher = dispatcher;

        if (dispatcher != null)
            Dispatcher.Default.RegisterCallback(HandleDataMessage, this.wifiClient); // Route Network Manager
        else
            this.wifiClient.NotificationReceived += HandleDataMessage; // use Normal Message Routing

    }

    /// <summary>
    /// Move Up the Selected Network List
    /// </summary>
    public void MoveUp()
    {
        selectedIndex--;

        if (selectedIndex >= AvailableNetworks.Count)
            selectedIndex = 0;
        else if (selectedIndex < 0)
            selectedIndex = AvailableNetworks.Count - 1;

        SelectedNetworkChanged?.Invoke(this, SelectedNetwork);
    }

    /// <summary>
    /// Move Down the Selected Network List
    /// </summary>
    public void MoveDown()
    {
        selectedIndex++;

        if (selectedIndex >= AvailableNetworks.Count)
            selectedIndex = 0;
        else if (selectedIndex < 0)
            selectedIndex = AvailableNetworks.Count - 1;

        SelectedNetworkChanged?.Invoke(this, SelectedNetwork);
    }

    /// <summary>
    /// Connect to the Selected Network
    /// </summary>
    /// <param name="password"></param>
    /// <param name="waitTime"></param>
    /// <param name="ipWaitTime"></param>
    /// <param name="timeSyncWaitTime"></param>
    public void ConnectNetwork(string password = null, int waitTime = 30, int ipWaitTime = 60, int timeSyncWaitTime = 60)
    {
        WirelessConnectionRequest request;

        // See if we have a profile
        if (password != null && SelectedNetworkProfile == null)
        {
            request = new WirelessConnectionRequest()
            {
                AdapterId = defaultAdapterRequest.AdapterId,
                PassKey = password,
                Remember = true,
                Ssid = SelectedNetwork.Ssid,
                WaitForConnectionSeconds = waitTime,
                WaitForIPAddressSeconds = ipWaitTime,
                WaitForTimeSyncSeconds = timeSyncWaitTime
            };

            networkProfilesList.Add(request);
        }
        else
            request = SelectedNetworkProfile;

        // If So Connect
        lock (wifiClient)
            wifiClient.Connect(request);
    }

    /// <summary>
    /// Disconnect from Current Network
    /// </summary>
    public void Disconnect()
    {
        lock (wifiClient)
            wifiClient.Disconnect(defaultAdapterRequest);
    }

    /// <summary>
    /// Discover Networks Available in your Area
    /// </summary>
    public void DiscoverNetworks()
    {
        if (!AdapterEnabled)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Discover Called with Disabled Adapter");
            return;
        }

        AvailableNetworks.Clear();
        NetworkListChanged?.Invoke(this, EventArgs.Empty);

        // Run Discover Networks
        if (dispatcher != null)
        {
            // Invoke Discover in Background then UpdateNetwork List in Dispatcher
            dispatcher.InvokeDispatcherAsync(() =>
            {
                return wifiClient.DiscoverNetworks(defaultAdapterRequest);
            },
            UpdateNetworkList);
        }
        else
        {
            // Run Discover in a thread and Wait for Result (Old Way)
            var task = Task.Run(() =>
            {
                lock (wifiClient)
                {
                    return wifiClient.DiscoverNetworks(defaultAdapterRequest);
                }
            });

            UpdateNetworkList(task.GetAwaiter().GetResult());
        }
    }

    /// <summary>
    /// Get Current IP Address
    /// </summary>
    /// <returns></returns>
    public IPInfo GetIPInfo()
    {
        return wifiClient.GetIPInfo(defaultAdapterRequest);
    }

    /// <summary>
    ///  Receive Messages from Network Service 
    /// </summary>
    /// <param name="model"></param>
    /// <param name="args"></param>
    public void HandleDataMessage(object model, EventArgs args)
    {
        if (args is WiFiClientServiceClient.AhsokaNotificationArgs e)
        {
            if (e.NotificationObject is WirelessConnectionNotification info)
            {
                if (info.State == WirelessConnectionState.ClientConnectionFailed)
                {
                    // Remove Profile since we can't connect to it.  This will force password entry.
                    networkProfilesList.Remove(networkProfilesList.FirstOrDefault(x => x.Ssid == info.Ssid));
                }

                if (info.State != WirelessConnectionState.ClientDisconnected)
                {
                    // Select Correct Network
                    var network = AvailableNetworks.FirstOrDefault(x => x.Ssid == info.Ssid);

                    if (network != null)
                        selectedIndex = AvailableNetworks.IndexOf(network);

                    ConnectedNetwork = network;
                }
                else
                {
                    ConnectedNetwork = null;
                }

                // Set State and Notify 
                ConnectionState = info.State;

                ConnectionStateChanged?.Invoke(this, ConnectionState);
            }
            else if (e.TransportId == WiFiClientMessageTypes.Ids.WirelessSignalChanged)
            {
                WirelessNetworkInfo netInfo = e.NotificationObject as WirelessNetworkInfo;
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Signal: {netInfo.SignalLevel} Network: {netInfo.Ssid}");
            }
          
        }
    }

    private void UpdateNetworkList(WirelessNetworkInfoList list)
    {
        AvailableNetworks.Clear();
        foreach (var item in list.Networks)
        {
            AvailableNetworks.Add(item);

            if (item.State == WirelessConnectionState.ClientConnected)
            {
                ConnectionState = item.State;
                ConnectedNetwork = item;
                selectedIndex = AvailableNetworks.Count - 1;
            }
        }

        NetworkListChanged?.Invoke(this, EventArgs.Empty);
        ConnectionStateChanged?.Invoke(this, ConnectionState);
    }

    private void DeviceClient_NotificationReceived(object sender, AhsokaClientBase<DeviceMessageTypes.Ids>.AhsokaNotificationArgs e)
    {
        if (e.TransportId == DeviceMessageTypes.Ids.OnboardDeviceReady)
        {
            DeviceInfo info = e.NotificationObject as DeviceInfo;
            if (info.DeviceType == DeviceTypes.OnboardWifiModule || info.DeviceType == DeviceTypes.OnboardWirelessComboModule)
            {
                if (!AdapterEnabled && !info.AutoEnable)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"OnboardDeviceReady Ready Receieved, Enabling Adapter if Needed {info.State}");
              
                    if (info.State != DeviceState.Enabled)
                        deviceClient.EnableDevice(info);
                    else if (info.State == DeviceState.Enabled)
                        InitWiFiAdapter(); // Enabled Already
                }
            }
        }
        else if (e.TransportId == DeviceMessageTypes.Ids.DeviceEnabled)
        {
            DeviceInfo info = e.NotificationObject as DeviceInfo;
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"DeviceEnabled Enabled Receieved, Configuring Wireless {info.DeviceType}");
            if (info.DeviceType == DeviceTypes.OnboardWifiModule || info.DeviceType == DeviceTypes.OnboardWirelessComboModule)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"WiFi Enabled Receieved, Configuring Wireless");
                InitWiFiAdapter();
            }
        }
    }

    private void InitWiFiAdapter()
    {
        // Enable WiFi
        lock (wifiClient)
        {
            var adapterList = wifiClient.GetAdapters(4000);
            if (adapterList == null || adapterList.Adapters.Count == 0)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"No Adapter Found Adapter:{adapterList != null} IsAvailable: {adapterList.Adapters.Count == 0}");
                return; // No Working Adapter
            }

            var adapter = adapterList.Adapters[0];
            defaultAdapterRequest = new AdapterRequest() { AdapterId = adapter?.AdapterId };

            AdapterEnabled = !String.IsNullOrEmpty(adapter?.AdapterId);
            AdapterEnabledChanged?.Invoke(this, EventArgs.Empty);

            // Configure for DHCP
            var config = wifiClient.GetNetworkConfiguration(defaultAdapterRequest);
            if (config.ClientConfiguration.ConnectionType == IPAddressConfiguration.StaticIP)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Reconfiguring Network Setup TO DHCP.");
                config.ClientConfiguration = new IPConfiguration() { ConnectionType = IPAddressConfiguration.Dhcp };
                wifiClient.SetNetworkConfiguration(config);
            }

            var list = wifiClient.GetNetworkProfiles(new ProfileRequest() { AdapterId = defaultAdapterRequest.AdapterId, UseOSProfiles = OperatingSystem.IsWindows() });
            foreach (var item in list.NetworkProfiles)
                networkProfilesList.Add(item);

            // Do Initial Discovery
            var networks = wifiClient.DiscoverNetworks(defaultAdapterRequest);
            UpdateNetworkList(networks);

            deviceClient.NotificationReceived -= DeviceClient_NotificationReceived;
        }
    }

    /// <summary>
    /// Attempt to Auto Connect to First Saved Network in Range
    /// </summary>
    /// <param name="waitTime"></param>
    /// <param name="ipWaitTime"></param>
    /// <param name="timeSyncWaitTime"></param>
    public void AttemptAutoConnect(int waitTime = 30, int ipWaitTime = 60, int timeSyncWaitTime = 60)
    {
        // If So Connect
        lock (wifiClient)
            wifiClient.AttemptAutoConnect(new AutoConnectRequest() { AdapterId = defaultAdapterRequest.AdapterId, WaitForConnectionSeconds = waitTime, WaitForIPAddressSeconds = ipWaitTime, WaitForTimeSyncSeconds = timeSyncWaitTime });
    }
}
