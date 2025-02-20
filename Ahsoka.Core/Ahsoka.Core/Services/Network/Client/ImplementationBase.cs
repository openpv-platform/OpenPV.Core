using Ahsoka.Core;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace Ahsoka.Services.Network.Client.Implementation;

internal abstract class ImplementationBase
{
    #region Fields
    bool autoConnectInProgress = false;
    AdapterConfiguration networkConfiguration = null;
    AdapterRequest networkAdapter = null;
    #endregion

    #region Properties
    public WiFiClientService NetworkService { get; private set; }
    #endregion

    #region Methods
    public ImplementationBase()
    {

    }

    public void Init(WiFiClientService service)
    {
        this.NetworkService = service;
    }


    internal AdapterRequestList GetAdapters()
    {
        if (networkConfiguration == null)
        {
            // Init
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Starting Wifi Client Networking...");

            // TODO: Find Network Adapter for Service
            OnStartNetwork(out networkConfiguration, out networkAdapter);
        }

        return new AdapterRequestList()
        {
            Adapters = { new AdapterRequest() { AdapterId = networkAdapter?.AdapterId } }
        };
    }

    internal AdapterConfiguration GetAdapterConfiguration(string adapterId)
    {
        return networkConfiguration;
    }

    internal WirelessNetworkInfoList Discover(string adapterId)
    {
        if (!autoConnectInProgress)
            return OnGetDiscoveredNetworks(adapterId);
        else
            return new WirelessNetworkInfoList();
    }

    internal AdapterResult UpdateConfiguration(AdapterConfiguration adapterConfiguration)
    {
        var result = OnValidateConfiguration(adapterConfiguration);

        if (!result.ConfigurationSuccessful)
            return result;

        networkConfiguration = adapterConfiguration;

        OnUpdateConfiguration(adapterConfiguration);

        return result;
    }

    internal NetworkProfilesList GetProfiles(ProfileRequest adapterId)
    {
        return OnGetProfiles(adapterId);
    }

    internal void SetProfiles(NetworkProfilesList profilesList)
    {
        OnSetProfiles(profilesList);
    }

    internal void Connect(WiFiClientService.AhsokaServiceRequest messageHeader, WirelessConnectionRequest connectionRequest)
    {
        if (!autoConnectInProgress)
            OnConnect(messageHeader, connectionRequest);
    }

    internal void Disconnect(WiFiClientService.AhsokaServiceRequest messageHeader, string adapterId)
    {
        if (!autoConnectInProgress)
            OnDisconnect(messageHeader, adapterId);
    }

    internal static IPInfo GetIPInfo(AdapterRequest adapterRequest)
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
                        ipInfo.IpPAddress = ipAddress.Address.ToString();
                }
            }

        }


        return ipInfo;
    }

    protected bool WaitForIPAddress(WiFiClientService.AhsokaServiceRequest messageHeader, WirelessConnectionRequest connectionRequest)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Waiting for IPAddress for {connectionRequest.WaitForIpAddressSeconds}");

        bool ipAddressFound = connectionRequest.WaitForIpAddressSeconds == 0;
        if (!ipAddressFound)
        {
            NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientWaitingForIp, connectionRequest.AdapterId, connectionRequest.Ssid);

            int timeout = connectionRequest.WaitForIpAddressSeconds; // Wait X Seconds before Freaking Out
            while (timeout > 0 && !ipAddressFound)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Waiting for IPAddress - {timeout} Remaining");
                var info = GetIPInfo(new AdapterRequest() { AdapterId = connectionRequest.AdapterId });

                ipAddressFound = !String.IsNullOrEmpty(info.IpPAddress);

                if (ipAddressFound)    // Update IPAddress 
                    this.NetworkService.UpdateCacheValue(WiFiClientServiceMessages.IPAddress, info.IpPAddress);

                if (!ipAddressFound)
                    Thread.Sleep(1000);

                NetworkService.SendConnectionStateNotification(messageHeader, WirelessConnectionState.ClientWaitingForIp, connectionRequest.AdapterId, connectionRequest.Ssid);

                timeout--;
            }




        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Returning from Wait For WiFi {ipAddressFound}");

        return ipAddressFound;
    }

    internal void AttemptAutoConnect(WiFiClientService.AhsokaServiceRequest messageHeader, AutoConnectRequest adapterRequest)
    {
        if (autoConnectInProgress)
            return;

        if (networkAdapter != null)
        {
            var profileList = GetProfiles(new ProfileRequest() { AdapterId = adapterRequest.AdapterId });
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
                        profile.WaitForIpAddressSeconds = adapterRequest.WaitForIpAddressSeconds;

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

    protected virtual AdapterResult OnValidateConfiguration(AdapterConfiguration adapterConfiguration)
    {
        AdapterResult result = new() { AdapterId = adapterConfiguration.AdapterId, ConfigurationSuccessful = true };
        return result;
    }

    #endregion

    #region Abstract Calls
    protected abstract bool WaitForTimeSync(WiFiClientService.AhsokaServiceRequest messageHeader, WirelessConnectionRequest connectionRequest);

    protected abstract void OnStartNetwork(out AdapterConfiguration defaultConfiguration, out AdapterRequest defaultRequest);

    protected abstract WirelessNetworkInfoList OnGetDiscoveredNetworks(string adapterId);

    protected abstract NetworkProfilesList OnGetProfiles(ProfileRequest profileRequest);

    protected abstract void OnUpdateConfiguration(AdapterConfiguration adapterConfiguration);

    protected abstract void OnSetProfiles(NetworkProfilesList profilesList);

    protected abstract void OnConnect(WiFiClientService.AhsokaServiceRequest messageHeader, WirelessConnectionRequest connectionRequest);

    protected abstract void OnDisconnect(WiFiClientService.AhsokaServiceRequest messageHeader, string adapterId);
    #endregion
}
