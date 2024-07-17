using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace Ahsoka.Services.Network;

internal abstract class NetworkPlatformImplementationBase
{
    #region Fields
    bool autoConnectInProgress = false;
    readonly Dictionary<string, AdapterConfiguration> networkConfiguration = new();
    readonly Dictionary<string, AdapterInfo> networkAdapters = new();
    #endregion

    #region Properties
    public NetworkService NetworkService { get; private set; }
    #endregion

    #region Methods
    public NetworkPlatformImplementationBase()
    {
        foreach (AdapterInfo item in GetAdapterList())
        {
            // Load Adapter Configurations and Adapter List
            CreateAdapterConfiguration(item);
        }
    }

    public void Init(NetworkService service)
    {
        this.NetworkService = service;
    }

    private void CreateAdapterConfiguration(AdapterInfo item)
    {
        using var stopwatch = new AhsokaStopwatch();

        var config = new AdapterConfiguration()
        {
            AdapterId = item.AdapterId,
            Description = item.Description,
            AdapterMode = item.AdapterType == NetworkAdapterType.WifiAccessPoint ? AdapterMode.AccessPoint :
                          item.AdapterType == NetworkAdapterType.WifiClient ? AdapterMode.Client : AdapterMode.NoMode, // Ethernet
        };

        OnCreateAdapterConfiguration(config, item);

        // Update Configuration with Runtime Values;
        item.Description = config.Description;
        item.AdapterId = config.AdapterId;

        networkConfiguration[config.AdapterId] = config;
        networkAdapters[item.AdapterId] = item;
    }

    internal AvailableAdapters GetAdapters()
    {
        var retVal = new AvailableAdapters();
        retVal.Adapters.AddRange(networkAdapters.Values);
        return retVal;
    }

    internal AdapterConfiguration GetAdapterConfiguration(string hardwareId)
    {
        return networkConfiguration[hardwareId];
    }

    internal AdapterInfo EnableAdapter(AdapterRequest adapterRequest)
    {
        if (networkAdapters.TryGetValue(adapterRequest.AdapterId, out var adapter))
        {
            OnEnableAdapter(adapter, networkConfiguration[adapterRequest.AdapterId]);
            return adapter;
        }
        else
            return null;

    }

    internal AdapterInfo DisableAdapter(AdapterRequest adapterRequest)
    {
        if (networkAdapters.TryGetValue(adapterRequest.AdapterId, out var adapter))
        {
            OnDisableAdapter(adapter);
            return adapter;
        }
        else
            return null;
    }

    internal WirelessNetworkInfoList Discover(string adapterId)
    {
        var info = networkAdapters[adapterId];

        if (!autoConnectInProgress && info.AdapterType == NetworkAdapterType.WifiClient)
            return OnGetDiscoveredNetworks(adapterId);
        else
            return new WirelessNetworkInfoList();
    }

    internal AdapterConfigurationResult UpdateConfiguration(AdapterConfiguration adapterConfiguration)
    {
        bool enabled = networkAdapters[adapterConfiguration.AdapterId].CurrentState == AdapterState.Enabled;
        if (enabled)
            return new AdapterConfigurationResult() { AdapterId = adapterConfiguration.AdapterId, ConfigurationSuccessful = false, ErrorDescription = "Adapter can only be configured in a disabled state" };

        var result = OnValidateConfiguration(adapterConfiguration);

        if (!result.ConfigurationSuccessful)
            return result;

        networkConfiguration[adapterConfiguration.AdapterId] = adapterConfiguration;

        return result;
    }

    internal NetworkProfilesList GetProfiles(ProfileRequest adapterId)
    {
        var info = networkAdapters[adapterId.AdapterId];
        if (info.AdapterType == NetworkAdapterType.WifiClient)
            return OnGetProfiles(adapterId);
        else
            return new NetworkProfilesList();
    }

    internal void SetProfiles(NetworkProfilesList profilesList)
    {
        var info = networkAdapters[profilesList.AdapterId];
        if (info.AdapterType == NetworkAdapterType.WifiClient)
            OnSetProfiles(profilesList);
    }

    internal void Connect(AhsokaMessageHeader messageHeader, WirelessConnectionRequest connectionRequest)
    {
        var info = networkAdapters[connectionRequest.AdapterId];
        if (!autoConnectInProgress && info.AdapterType == NetworkAdapterType.WifiClient)
            OnConnect(messageHeader, connectionRequest);
    }

    internal void Disconnect(AhsokaMessageHeader messageHeader, string adapterId)
    {
        var info = networkAdapters[adapterId];
        if (!autoConnectInProgress && info.AdapterType == NetworkAdapterType.WifiClient)
            OnDisconnect(messageHeader, adapterId);
    }

    internal IPInfo GetIPInfo(AdapterRequest adapterRequest)
    {
        IPInfo ipInfo = new();
        foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (netif.Id.ToLower().Contains(adapterRequest.AdapterId.ToLower())
                && netif.OperationalStatus == OperationalStatus.Up)
            {
                ipInfo.AdapterId = netif.Id.ToString();
                ipInfo.MacAddress = netif.GetPhysicalAddress().ToString();

                IPInterfaceProperties properties = netif.GetIPProperties();

                foreach (var ipAddress in properties.DnsAddresses)
                {
                    if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                        ipInfo.Dns.Add(ipAddress.ToString());
                }

                foreach (IPAddressInformation ipAddress in properties.UnicastAddresses)
                {
                    if (ipAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                        ipInfo.IPAddress = ipAddress.Address.ToString();
                }
            }

        }

        // Update IPAddress 
        this.NetworkService.UpdateCacheValue(NetworkServiceParameters.IPAddress, ipInfo.IPAddress);

        return ipInfo;
    }

    protected bool WaitForIPAddress(AhsokaMessageHeader messageHeader, WirelessConnectionRequest connectionRequest)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Waiting for IPAddress for {connectionRequest.WaitForIPAddressSeconds}");

        bool ipAddressFound = connectionRequest.WaitForIPAddressSeconds == 0;
        if (!ipAddressFound)
        {
            NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientWaitingForIp, connectionRequest.AdapterId, connectionRequest.Ssid);

            int timeout = connectionRequest.WaitForIPAddressSeconds; // Wait X Seconds before Freaking Out
            while (timeout > 0 && !ipAddressFound)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Waiting for IPAddress - {timeout} Remaining");
                var info = GetIPInfo(new AdapterRequest() { AdapterId = connectionRequest.AdapterId });

                ipAddressFound = !String.IsNullOrEmpty(info.IPAddress);

                if (!ipAddressFound)
                    Thread.Sleep(1000);

                NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientWaitingForIp, connectionRequest.AdapterId, connectionRequest.Ssid);

                timeout--;
            }
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Returning from Wait For WiFi {ipAddressFound}");

        return ipAddressFound;
    }

    internal void AttemptAutoConnect(AhsokaMessageHeader messageHeader, AutoConnectRequest adapterRequest)
    {
        var info = networkAdapters[adapterRequest.AdapterId];
        if (autoConnectInProgress || info.AdapterType == NetworkAdapterType.WifiClient)
            return;
            
        if (networkAdapters.TryGetValue(adapterRequest.AdapterId, out var adapter))
        {
            if (adapter.CurrentState == AdapterState.Enabled)
            {
                var profileList = GetProfiles(new ProfileRequest() { AdapterId = adapterRequest.AdapterId, UseOSProfiles = OperatingSystem.IsWindows() });
                if (profileList.NetworkProfiles.Count == 0)
                    return;

                autoConnectInProgress = true;

                try
                {
                    var networks = OnGetDiscoveredNetworks(adapterRequest.AdapterId);
                    foreach (var profile in profileList.NetworkProfiles)
                    {
                        if (networks.Networks.Any(x => x.Ssid == profile.Ssid))
                        {
                            profile.WaitForConnectionSeconds = adapterRequest.WaitForConnectionSeconds;
                            profile.WaitForTimeSyncSeconds = adapterRequest.WaitForTimeSyncSeconds;
                            profile.WaitForIPAddressSeconds = adapterRequest.WaitForIPAddressSeconds;

                            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Attempting to Auto Connect - {profile.Ssid}");

                            // Attempt Connection
                            OnConnect(messageHeader, profile);
                        }
                    }
                }
                finally
                {
                    autoConnectInProgress = false;
                }
            }
        }
    }

    protected virtual AdapterConfigurationResult OnValidateConfiguration(AdapterConfiguration adapterConfiguration)
    {
        AdapterConfigurationResult result = new() { AdapterId = adapterConfiguration.AdapterId, ConfigurationSuccessful = true };

        if (adapterConfiguration.AdapterMode == AdapterMode.AccessPoint)
        {
            if (adapterConfiguration.AccessPointConfiguration == null)
            {
                result.ConfigurationSuccessful = false;
                result.ErrorDescription = "Access Point Configuration not fount";
                return result;
            }

            if (adapterConfiguration.AccessPointConfiguration.Ssid.Length < 1 || adapterConfiguration.AccessPointConfiguration.Ssid.Length > 31 )
            {
                result.ConfigurationSuccessful = false;
                result.ErrorDescription = "Valid SSID's must contain between 1 and 31 Characters";
                return result;
            }

            if (adapterConfiguration.AccessPointConfiguration.EncryptionPassKey.Length < 8 || adapterConfiguration.AccessPointConfiguration.Ssid.Length > 63)
            {
                result.ConfigurationSuccessful = false;
                result.ErrorDescription = "Valid PassKeys must contain between 8 and 63 Characters";
                return result;
            }


            if (adapterConfiguration.AccessPointConfiguration.ChannelNumber < 1 || adapterConfiguration.AccessPointConfiguration.ChannelNumber > 11)
            {
                result.ConfigurationSuccessful = false;
                result.ErrorDescription = "Please Specify a Channel Number between 1 and 11";
                return result;
            }
        }

        return result;
    }

    #endregion

    #region Abstract Calls
    protected abstract bool WaitForTimeSync(AhsokaMessageHeader messageHeader, WirelessConnectionRequest connectionRequest);

    protected abstract List<AdapterInfo> GetAdapterList();

    protected abstract void OnEnableAdapter(AdapterInfo adapter, AdapterConfiguration configuration);

    protected abstract void OnDisableAdapter(AdapterInfo adapter);

    protected abstract void OnCreateAdapterConfiguration(AdapterConfiguration retVal, AdapterInfo item);

    protected abstract WirelessNetworkInfoList OnGetDiscoveredNetworks(string adapterId);

    protected abstract NetworkProfilesList OnGetProfiles(ProfileRequest profileRequest);

    protected abstract void OnSetProfiles(NetworkProfilesList profilesList);

    protected abstract void OnConnect(AhsokaMessageHeader messageHeader, WirelessConnectionRequest connectionRequest);

    protected abstract void OnDisconnect(AhsokaMessageHeader messageHeader, string adapterId);

 
    #endregion
}
