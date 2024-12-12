using Ahsoka.ServiceFramework;
using Ahsoka.Services.Network.ESPWifi;
using Ahsoka.Services.Network.Platform;
using Ahsoka.Services.Network.Platform.ESPWifi;
using Newtonsoft.Json.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Ahsoka.Services.Network.AP.Implementation;

[ExcludeFromCodeCoverage]
internal partial class STImplementation : ImplementationBase
{
    AccessPointServiceConfiguration currentConfiguration;
  
    protected override void OnInit(out AccessPointServiceConfiguration defaultConfiguration, out AdapterRequest defaultRequest)
    {
        string adapterId = "ethap0";

        defaultRequest = new AdapterRequest() { AdapterId = adapterId };
        this.currentConfiguration = new() { AdapterId = adapterId };

        NetworkServiceHelper.ReadAccessPointConfiguration(currentConfiguration);

        defaultConfiguration = currentConfiguration;
    }


    protected override AdapterResult OnStartAccessPoint(AdapterRequest adapterRequest)
    {
        // Configure Adapter 
        InteropList.Add(adapterRequest.AdapterId);
        using var connection = InteropList.AquireInterop(adapterRequest.AdapterId);

        // Configure and Write Configuratoin
        ConfigureAdapter(connection, currentConfiguration);

        return new AdapterResult() { AdapterId = currentConfiguration.AdapterId, ConfigurationSuccessful = true};
    }


    void ConfigureAdapter(InteropLock connection, AccessPointServiceConfiguration updateConfiguration)
    {
        if (connection != null)
        {
            var value = WifiMode.WIFI_MODE_AP;

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
            if (mode != WifiMode.WIFI_MODE_AP)
                connection.ESPInterop.WifiSetMode(WifiMode.WIFI_MODE_AP);

            // Client is configured during Connection Initiation.
            if (updateConfiguration.AccessPointConfiguration != null)
            {
                // Enable AP Mode
                CommandResult result = connection.ESPInterop.WifiSetMode(WifiMode.WIFI_MODE_AP);
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"STNetImp: Set WiFi mode to AP ({result})");

                ESPHostedSoftAPConfig config = new ESPHostedSoftAPConfig()
                {
                    ssidHidden = !updateConfiguration.AccessPointConfiguration.BroadcastSsid,
                    MaxConnections = 10,
                    bandwidth = WifiBandwidth.WIFI_BANDWIDTH_HT20,
                    ssid = updateConfiguration.AccessPointConfiguration.Ssid,
                    encryptionMode = updateConfiguration.AccessPointConfiguration.SecurityType == SecurityType.NoSecurity ? (int)WifiSupportedEncryptionMode.WIFI_AP_AUTH_OPEN : (int)WifiSupportedEncryptionMode.WIFI_AP_AUTH_WPA_WPA2_PSK,
                    password = updateConfiguration.AccessPointConfiguration.EncryptionPassKey,
                    channel = updateConfiguration.AccessPointConfiguration.ChannelNumber
                };

                connection.ESPInterop.WifiSetAPConfiguration(config);
                connection.ESPInterop.WifiGetAPConfiguration(ref config);
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Current AP Config:");
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"  SSID: {config.ssid}");
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"  SSID Hidden? {config.ssidHidden}");
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"  Password: {config.password}");
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"  Channel: {config.channel}");
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"  Bandwidth: {config.bandwidth}");
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"  Encryption Mode: {config.encryptionMode}");
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "");
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, config.ToString());

                NetworkServiceHelper.WriteAccessPointConfiguration(updateConfiguration);
            }
        }
    }


    protected override AdapterResult OnStopAccessPoint(AdapterRequest adapterRequest)
    {
        using var connection = InteropList.AquireInterop(adapterRequest.AdapterId);
        connection.ESPInterop.WifiSetMode(WifiMode.WIFI_MODE_NONE);
        return new AdapterResult() { AdapterId = adapterRequest.AdapterId, ConfigurationSuccessful = true };
    }

    protected override void OnUpdateConfiguration(AccessPointServiceConfiguration adapterConfiguration)
    {
        currentConfiguration = adapterConfiguration;
        NetworkServiceHelper.WriteAccessPointConfiguration(adapterConfiguration);
    }

    protected override AccessPointClientList OnGetClientList(AdapterRequest adapterRequest)
    {
        using var connection = InteropList.AquireInterop(adapterRequest.AdapterId);

        var info = connection.ESPInterop.WifiConnectedStations();
        var results = new AccessPointClientList() { AdapterId = adapterRequest.AdapterId, ClientCount = info.Count }; // Not Super Usefull Yet.

        foreach (var item in info)
            results.Clients.Add(new AccessPointClient() { MacAddress = item.bssid, Rssi = item.rssi });

        return results;
    }
}
