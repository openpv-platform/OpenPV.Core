using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Ahsoka.Services.Network.Platform;

[ExcludeFromCodeCoverage]
internal class NetworkServiceHelper
{
    const string matchSection = "Match";
    const string matchName = "Name";

    const string linkSection = "Link";
    const string linkMacKey = "MacAddress";

    const string networkSection = "Network";
    const string dhcpEnabledName = "DHCP";
    const string dnsName = "DNS";
    const string ipAddressName = "Address";
    const string gatewayName = "Gateway";
    const string dhcpServer = "DHCPServer";

    const string miscSection = "Misc";
    const string poolOffset = "PoolOffset";
    const string poolSize = "PoolSize";
              
    const string dhcpServerSection = "DHCPServer";
    const string ssid = "SSID";
    const string channelNumber = "ChannelNumber";
    const string passKey = "PassKey";


    public static void WriteClientConfiguration(AdapterConfiguration updateConfiguration)
    {
        // Paths
        GetPaths(updateConfiguration.AdapterId, out string pathToUserLinuxNetDFile, out string pathToSystemLinuxNetDFile);

        string pathToLoad;

        if (updateConfiguration.SaveAsDefault)
        {
            // Load System ONLY
            pathToLoad = pathToSystemLinuxNetDFile;

            // Remove any Runtime file.
            if (File.Exists(pathToUserLinuxNetDFile))
                File.Delete(pathToUserLinuxNetDFile);
        }
        else
        {
            // Save Runtime Folder
            pathToLoad = pathToUserLinuxNetDFile;
        }

        string fileContents;
        if (updateConfiguration.ClientConfiguration.ConnectionType == IPAddressConfiguration.StaticIP)
        {
            fileContents = Properties.Resources.WirelessClientStatic;
            fileContents = fileContents.Replace("<ADAPTERID>", updateConfiguration.AdapterId);
            fileContents = fileContents.Replace("<MAC>", updateConfiguration.OverrideMacAddress);
            fileContents = fileContents.Replace("<ADDRESS>", updateConfiguration.ClientConfiguration.IPAddress);
            fileContents = fileContents.Replace("<DNS>", updateConfiguration.ClientConfiguration.Dns);
            fileContents = fileContents.Replace("<GATEWAY>", updateConfiguration.ClientConfiguration.GatewayIP);
        }
        else
        {
            fileContents = Properties.Resources.WirelessClientDHCP;
            fileContents = fileContents.Replace("<ADAPTERID>", updateConfiguration.AdapterId);
            fileContents = fileContents.Replace("<MAC>", updateConfiguration.OverrideMacAddress);
        }

        // Do I need to reload?
        string directoryPath = Path.GetDirectoryName(pathToLoad);
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Network Configuration for {updateConfiguration.AdapterId} to {pathToLoad}");
        File.WriteAllText(pathToLoad, fileContents);

        // Restart Network Service in SystemD
        RestartNetworkServices();
    }

    public static void ReadClientConfiguration(AdapterConfiguration updateConfiguration, bool readDefault = false)
    {
        // Paths
        GetPaths(updateConfiguration.AdapterId, out string pathToUserLinuxNetDFile, out string pathToSystemLinuxNetDFile);

        // Determine Path
        string pathToLoad = pathToUserLinuxNetDFile;
        if (!File.Exists(pathToUserLinuxNetDFile) || readDefault)
            pathToLoad = pathToSystemLinuxNetDFile;

        Dictionary<string, Dictionary<string, string>> networkUnitData = ParseNetworkFile(pathToLoad);

        updateConfiguration.ClientConfiguration = new IPConfiguration()
        {
            ConnectionType = (networkUnitData[networkSection][dhcpEnabledName] == "NO") ? IPAddressConfiguration.StaticIP : IPAddressConfiguration.Dhcp,
            IPAddress = networkUnitData[networkSection][ipAddressName],
            GatewayIP = networkUnitData[networkSection][gatewayName],
            Dns = networkUnitData[networkSection][dnsName],
        };
    }

    public static void WriteAccessPointConfiguration(AccessPointServiceConfiguration updateConfiguration)
    {
        // Paths
        GetPaths(updateConfiguration.AdapterId, out string pathToUserLinuxNetDFile, out string pathToSystemLinuxNetDFile);
        string pathToLoad;

        if (updateConfiguration.SaveAsDefault)
        {
            // Load System ONLY
            pathToLoad = pathToSystemLinuxNetDFile;

            // Remove any Runtime file.
            if (File.Exists(pathToUserLinuxNetDFile))
                File.Delete(pathToUserLinuxNetDFile);
        }
        else
        {
            // Save Runtime Folder
            pathToLoad = pathToUserLinuxNetDFile;
        }


        string fileContents;
        if (updateConfiguration.DhcpServerConfiguration != null)
        {
            fileContents = Properties.Resources.WirelessAP;
            fileContents = fileContents.Replace("<ADAPTERID>", updateConfiguration.AdapterId);
            fileContents = fileContents.Replace("<MAC>", updateConfiguration.OverrideMacAddress);
            fileContents = fileContents.Replace("<DHCPServer>", "YES");
            fileContents = fileContents.Replace("<ADDRESS>", updateConfiguration.DhcpServerConfiguration.DhcpIpBaseAddress);
            fileContents = fileContents.Replace("<DNS>", updateConfiguration.DhcpServerConfiguration.Dns);
            fileContents = fileContents.Replace("<START_OF_POOL>", updateConfiguration.DhcpServerConfiguration.DhcpBaseOffset.ToString());
            fileContents = fileContents.Replace("<POOL_SIZE>", updateConfiguration.DhcpServerConfiguration.DhcpBasePoolSize.ToString());

            fileContents = fileContents.Replace("<SSID>", updateConfiguration.AccessPointConfiguration.Ssid);
            fileContents = fileContents.Replace("<CHANNEL_NUMBER>", updateConfiguration.AccessPointConfiguration.ChannelNumber.ToString());
            fileContents = fileContents.Replace("<SECURITY_TYPE>", updateConfiguration.AccessPointConfiguration.SecurityType.ToString());
            fileContents = fileContents.Replace("<PASS_KEY>", updateConfiguration.AccessPointConfiguration.EncryptionPassKey);
        }
        else
        {
            fileContents = Properties.Resources.WirelessAPNoDHCP;
            fileContents = fileContents.Replace("<ADAPTERID>", updateConfiguration.AdapterId);
            fileContents = fileContents.Replace("<MAC>", updateConfiguration.OverrideMacAddress);

            fileContents = fileContents.Replace("<SSID>", updateConfiguration.AccessPointConfiguration.Ssid);
            fileContents = fileContents.Replace("<CHANNEL_NUMBER>", updateConfiguration.AccessPointConfiguration.ChannelNumber.ToString());
            fileContents = fileContents.Replace("<SECURITY_TYPE>", updateConfiguration.AccessPointConfiguration.SecurityType.ToString());
            fileContents = fileContents.Replace("<PASS_KEY>", updateConfiguration.AccessPointConfiguration.EncryptionPassKey);
        }

        // Do I need to reload?
        string directoryPath = Path.GetDirectoryName(pathToLoad);
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network Configuration for {updateConfiguration.AdapterId} written to {pathToLoad}");
        File.WriteAllText(pathToLoad, fileContents);

        // Restart Network Service in SystemD
        RestartNetworkServices();
    }

    public static void ReadAccessPointConfiguration(AccessPointServiceConfiguration updateConfiguration, bool readDefault = false)
    {
        // Paths
        GetPaths(updateConfiguration.AdapterId, out string pathToUserLinuxNetDFile, out string pathToSystemLinuxNetDFile);

        // Determine Path
        string pathToLoad = pathToUserLinuxNetDFile;
        if (!File.Exists(pathToUserLinuxNetDFile) || readDefault)
            pathToLoad = pathToSystemLinuxNetDFile;

        Dictionary<string, Dictionary<string, string>> networkUnitData = ParseNetworkFile(pathToLoad);
        try
        {
            bool isDhcpEnabled = networkUnitData[networkSection][dhcpServer] == "YES" ? true : false;

            if (isDhcpEnabled)
                updateConfiguration.DhcpServerConfiguration = new()
                {
                    DhcpIpBaseAddress = networkUnitData[networkSection][ipAddressName],
                    DhcpBaseOffset = int.Parse(networkUnitData[dhcpServerSection][poolOffset]),
                    DhcpBasePoolSize = int.Parse(networkUnitData[dhcpServerSection][poolSize]),
                    Dns = networkUnitData[dhcpServerSection][dnsName]
                };

            updateConfiguration.AccessPointConfiguration = new()
            {
                Ssid = networkUnitData[miscSection][ssid],
                ChannelNumber = int.Parse(networkUnitData[miscSection][channelNumber]),
                EncryptionPassKey = networkUnitData[miscSection][passKey],
                SecurityType = SecurityType.Wpa2
            };

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    public static bool RestartNetworkServices()
    {
        // Network Services are only managed with SystemD on Linux.
        if (!OperatingSystem.IsLinux())
            return true;

        try
        {
            ProcessUtility.RunProcess("/bin/systemctl", "restart systemd-networkd", null, out string log, out string error);
            if (error != "")
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Error Restarting Network: {error}");
                return false;
            }

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Network Services Restarted");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void GetPaths(string adapterID, out string pathToUserLinuxNetDFile, out string pathToSystemLinuxNetDFile)
    {
        pathToUserLinuxNetDFile = $"{LinuxPathInfo.CreateSystemManagerRuntimePath("network")}/10-{adapterID}-Client.network";
        pathToSystemLinuxNetDFile = $"{LinuxPathInfo.CreateSystemManagerPath("network")}/20-{adapterID}-Client.network";
    }

    private static Dictionary<string, Dictionary<string, string>> ParseNetworkFile(string pathToLoad)
    {
        Dictionary<string, Dictionary<string, string>> results = CreateDefaultValues();

        Dictionary<string, string> currentSection = null;
        if (File.Exists(pathToLoad))
            foreach (var line in File.ReadAllLines(pathToLoad))
            {
                if (line.StartsWith("["))
                {
                    string currentSectionKey = line.Substring(1).Trim().TrimEnd(']');
                    results.TryGetValue(currentSectionKey, out currentSection);
                }
                else if (line.Contains('=') && currentSection != null)
                {
                    string key = line.Substring(0, line.IndexOf("=")).Trim();
                    string value = line.Substring(line.IndexOf("=") + 1).Trim();
                    if (currentSection.ContainsKey(key))
                        currentSection[key] = value;
                }
            }

        return results;
    }

    private static Dictionary<string, Dictionary<string, string>> CreateDefaultValues()
    {
        Dictionary<string, Dictionary<string, string>> results = new(StringComparer.InvariantCultureIgnoreCase)
        {
            {
                matchSection,
                new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { matchName, String.Empty }
                }
            },

            {
                linkSection,
                new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { linkMacKey, String.Empty }
                }
            },
            {
                networkSection,
                new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { dhcpEnabledName, String.Empty },
                    { dnsName, String.Empty },
                    { ipAddressName, String.Empty },
                    { gatewayName, String.Empty },
                    { dhcpServer, String.Empty },
                }
            },
            {
                dhcpServerSection,
                new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { poolSize, String.Empty },
                    { poolOffset, String.Empty },
                    { dnsName, String.Empty },
                }
            },
            {
                miscSection,
                new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                {
                    { passKey, String.Empty },
                    { ssid, String.Empty },
                    { channelNumber, "0" },
                }
            }
        };

        return results;
    }

}