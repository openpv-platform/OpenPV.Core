using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.Network.ESPWifi;

[ExcludeFromCodeCoverage]
internal class ESPTest
{
    const string FAIL_MAC = "77:88:99:aa:bb:cc";
    const string GOOD_MAC = "76:88:99:aa:bb:cc";
    const string AP_MAC = "76:88:99:aa:bb:cd";

    public static void RunTest()
    {
        Console.WriteLine("\nStarting Ahsoka.Services.Wifi.ESPhosted.Test");

        ESPInterop wifi = new();

        //TestModeSwitch(wifi, WifiMode.WIFI_MODE_AP);

        // Leave in dual AP/Station mode for further testing
        //TestModeSwitch(wifi, WifiMode.WIFI_MODE_STA);

        wifi.WifiSetMode(WifiMode.WIFI_MODE_STA);
        WifiMode mode = wifi.WifiGetMode();
        if (mode != WifiMode.WIFI_MODE_STA)
            Console.WriteLine($"ERROR: Mode = {mode}");

        string mac = wifi.WifiGetMAC(WifiMode.WIFI_MODE_STA);
        Console.WriteLine($"CURRENT MAC IS = {mac}");

        //TestMACAddress(wifi);

        //TestPowerSaveMode(wifi);

        //TestTransmitPower(wifi);

        TestConfigureAP(wifi);

        //TestGetAPConfig(wifi);

        // Scan available networks (and have user select ssid)
        Console.WriteLine("\nScanning for APs");
        List<ESPHostedWifiScanList> apList = wifi.WifiStartScanning();

        String ssid = ChooseAP(apList);

        if (ssid != "")
        {
            Console.WriteLine($"Enter password for {ssid}:");
            String password;
            if ((password = Console.ReadLine()) == null)
                password = "";

            Console.WriteLine($"Connecting to {ssid}");
            ConnectToAP(wifi, ssid, password);

            string adapter = "ethsta0";
            LinuxNetworkUtilities.StopAdapter(adapter);
            //LinuxNetworkUtilities.AssignMACAddress(adapter, mac);
            LinuxNetworkUtilities.StartAdapter(adapter);

            LinuxNetworkUtilities.AdapterRequestDHCPAddress(adapter);

            DisplayConnectionStatus(wifi);

            Console.WriteLine("\nYou should now be able to access the Internet from another shell.");
        }

        Console.Write("\nHit <enter> to disconnect and end program.");
        Console.ReadLine();

        List<ESPHostedWifiConnectedStationsList> stationList = wifi.WifiConnectedStations();
        DisplayConnectedStations(stationList);

        LinuxNetworkUtilities.StopAdapter("ethsta0");
        wifi.WifiDisconnect();

        Console.WriteLine("\nEnd of Program");
        return;
    }

    // Leave in Dual AP/Station mode for further testing
    private static void TestModeSwitch(ESPInterop wifi, WifiMode newMode)
    {
        Console.WriteLine("\nTest Mode Switch");
        Console.WriteLine("================");
        // Get current mode
        WifiMode mode = wifi.WifiGetMode();
        Console.WriteLine($"Current Mode = {mode}");

        // Set mode
        Console.WriteLine($"Setting mode to {newMode}.");
        wifi.WifiSetMode(newMode);
        mode = wifi.WifiGetMode();
        if (mode != newMode)
            Console.WriteLine($"ERROR: Mode = {mode}");

        Console.WriteLine($"Current Mode = {mode}");
    }

    private static void TestMACAddress(ESPInterop wifi)
    {
        Console.WriteLine("\nTest Setting MAC Address");
        Console.WriteLine("========================");
        var mode = WifiMode.WIFI_MODE_STA;
        // Read the station mac address
        string mac = wifi.WifiGetMAC(mode);
        Console.WriteLine($"Original MAC Addr of Wifi Module is {mac}");

        // Set an invalid MAC address (documentation says bit 0 of first byte cannot = 1
        Console.WriteLine($"Trying to set invalid MAC to {FAIL_MAC}");
        wifi.WifiSetMAC(mode, FAIL_MAC);

        // Set a valid MAC address and confirm
        Console.WriteLine($"Setting valid MAC to {GOOD_MAC}");
        wifi.WifiSetMAC(mode, GOOD_MAC);
        mac = wifi.WifiGetMAC(mode);
        Console.WriteLine($"Current MAC Addr is {mac}");

        // Set a valid AP MAC address and confirm
        Console.WriteLine($"Setting AP MAC to {AP_MAC}");
        mode = WifiMode.WIFI_MODE_AP;
        wifi.WifiSetMAC(mode, AP_MAC);
        mac = wifi.WifiGetMAC(mode);
        Console.WriteLine($"Current AP MAC Addr is {mac}");
    }

    private static void TestPowerSaveMode(ESPInterop wifi)
    {
        Console.WriteLine("\nTest Power Saving Mode");
        Console.WriteLine("======================");
        WifiPowerSaveMode powerMode = wifi.WifiGetPowerSaveMode();
        Console.WriteLine($"Current power save mode is {powerMode}");

        // Set mode to maximum power save
        Console.WriteLine($"Setting power save mode to {WifiPowerSaveMode.WIFI_PS_MAX_MODEM}.");
        wifi.WifiSetPowerSaveMode(WifiPowerSaveMode.WIFI_PS_MAX_MODEM);

        powerMode = wifi.WifiGetPowerSaveMode();
        if (powerMode != WifiPowerSaveMode.WIFI_PS_MAX_MODEM)
            Console.WriteLine($"ERROR: Mode = {powerMode}");

        // Set mode to normal
        Console.WriteLine($"Setting power save mode to normal, i.e. mode = {WifiPowerSaveMode.WIFI_PS_MIN_MODEM}.");
        wifi.WifiSetPowerSaveMode(WifiPowerSaveMode.WIFI_PS_MIN_MODEM);
        powerMode = wifi.WifiGetPowerSaveMode();
        if (powerMode != WifiPowerSaveMode.WIFI_PS_MIN_MODEM)
            Console.WriteLine($"ERROR: Mode = {powerMode}");

        Console.WriteLine($"Current Mode = {powerMode}");
    }

    private static void TestTransmitPower(ESPInterop wifi)
    {
        Console.WriteLine("\nTest Change Maximum Transmit Power");
        Console.WriteLine("==================================");
        WifiTxPowerLevel txPower = wifi.WifiGetTxPower();
        Console.WriteLine($"Current Tx Power is {txPower}");
        Console.WriteLine("Enter a new Maximum Tx Power. Valid options are:");
        foreach (sbyte i in Enum.GetValues(typeof(WifiTxPowerLevel)))
        {
            Console.WriteLine($"({i}) {Enum.GetName(typeof(WifiTxPowerLevel), i)}");
        }
        Console.WriteLine("Enter the number shown in parenthesis or <enter> to leave as is.");
        String choice;
        while ((choice = Console.ReadLine()) != null &&
                choice != "")
        {
            if (sbyte.TryParse(choice, out sbyte value) &&
                Enum.IsDefined(typeof(WifiTxPowerLevel), value))
            {
                Console.WriteLine($"Setting Max Tx Power to: {(WifiTxPowerLevel)value}");
                wifi.WifiSetTxPower((WifiTxPowerLevel)value);
                WifiTxPowerLevel newPower = wifi.WifiGetTxPower();
                Console.WriteLine($"New Tx Power is {newPower}");
            }
            else
            {
                Console.WriteLine($"\nError: Invalid number ({choice}) entered.");
            }
            Console.WriteLine("\nEnter a new number or <enter> to continue.");
        }
    }

    private static void DisplayConnectionStatus(ESPInterop wifi)
    {
        ESPHostedStationConfig apConfig = new();
        wifi.WifiGetConnectionStatus(ref apConfig);

        Console.WriteLine($"AP's bssid i.e. MAC address {apConfig.bssid}");
        Console.WriteLine($"Is wpa3 supported: {apConfig.wpa3Supported}");
        Console.WriteLine($"AP's rssi: {apConfig.rssi}");
        Console.WriteLine($"AP's channel number {apConfig.channel}");
        Console.WriteLine($"AP's encryption mode {apConfig.encryptionMode}");

        Console.WriteLine($"Status: {apConfig.status}");
    }

    private static void DisplayConnectedStations(List<ESPHostedWifiConnectedStationsList> stationList)
    {
        int i = 1;
        if (stationList.Count > 0)
        {
            Console.WriteLine($"\nFound {stationList.Count} connected stations.");
            foreach (ESPHostedWifiConnectedStationsList station in stationList)
            {
                Console.WriteLine($"\n========({i++})=========");
                Console.WriteLine($"BSSID: {station.bssid}");
                Console.WriteLine($"RSSI: {station.rssi}");
            }
        }
    }

    private static void DisplayAPs(List<ESPHostedWifiScanList> apList)
    {
        int i = 1;
        Console.WriteLine($"\nFound {apList.Count} AP's");
        foreach (ESPHostedWifiScanList ap in apList)
        {
            Console.WriteLine($"\n========({i++})=========");
            Console.WriteLine($"AP SSID: {ap.ssid}");
            Console.WriteLine($"AP BSSID: {ap.bssid}");
            Console.WriteLine($"AP RSSI: {ap.rssi}");
            Console.WriteLine($"AP Channel: {ap.channel}");
            Console.WriteLine($"AP Encryption Mode: {ap.encryptionMode}");
        }
    }

    private static string ChooseAP(List<ESPHostedWifiScanList> apList)
    {
        String chosenSSID = "";
        DisplayAPs(apList);
        Console.WriteLine("Please enter the number of the AP to connect: ");
        String choice = Console.ReadLine();
        if (choice != null && Int32.TryParse(choice, out int apNumber))
        {
            if (apNumber > 0 && apNumber <= apList.Count)
            {
                ESPHostedWifiScanList ap = apList[apNumber - 1];
                chosenSSID = ap.ssid;
            }
        }
        return chosenSSID;
    }

    private static void ConnectToAP(ESPInterop wifi, string ssid, string password)
    {
        ESPHostedStationConfig apConfig = new()
        {
            ssid = ssid,
            password = password,
            listenInterval = 5,
            wpa3Supported = false
        };
        wifi.WifiConnect(apConfig);
    }


    static void TestConfigureAP(ESPInterop wifi)
    {
        ESPHostedSoftAPConfig config = new();
        String input;

        // ssid
        do
        {
            Console.WriteLine("Enter an SSID for the AP (1-31 characters):");
            input = Console.ReadLine();
        }
        while ((input != null) && (input.Length < 1 || input.Length > 31));
        if (input == null)
            return;
        config.ssid = input;

        // Channel
        int value;
        do
        {
            Console.WriteLine("Enter channel number (1-11):");
            input = Console.ReadLine();
        }
        while (!Int32.TryParse(input, out value) || value < 1 || value > 11);
        config.channel = value;

        // Encryption
        Console.WriteLine("Encryption mode, valid options are:");
        foreach (int i in Enum.GetValues(typeof(WifiSupportedEncryptionMode)))
        {
            Console.WriteLine($"  ({i}) {Enum.GetName(typeof(WifiSupportedEncryptionMode), i)}");
        }
        do
        {
            Console.WriteLine("Enter the number in parenthesis: ");
            input = Console.ReadLine();
        }
        while (!Int32.TryParse(input, out value) || !Enum.IsDefined(typeof(WifiSupportedEncryptionMode), value));
        config.encryptionMode = value;

        // password
        if (config.encryptionMode > 0)
        {
            Console.WriteLine($"Enter password for {config.ssid} (8-63 characters):");
            if (((input = Console.ReadLine()) == null) || (input.Length > 63))
                input = "";
            config.password = input;
        }
        else
        {
            config.password = "";
        }

        // Remaining are defaulted
        config.ssidHidden = false;
        config.MaxConnections = 10;
        config.bandwidth = WifiBandwidth.WIFI_BANDWIDTH_HT20;

        wifi.WifiSetAPConfiguration(config);

        /*LinuxNetworkUtilities.WifiEnableAPDevice(AP_MAC);

        DHCPServerConfig dhcpConfig = new()
        {
            address = "192.168.4.1",
            addressMask = 24,
            poolOffset = 100,
            poolSize = 50,
            dns = "8.8.8.8"
        };
        LinuxNetworkUtilities.EnableDHCPServer(dhcpConfig);*/
    }

    static void TestGetAPConfig(ESPInterop wifi)
    {
        ESPHostedSoftAPConfig config = new();
        wifi.WifiGetAPConfiguration(ref config);
        Console.WriteLine("Current AP Config:");
        Console.WriteLine($"  SSID: {config.ssid}");
        Console.WriteLine($"  SSID Hidden? {config.ssidHidden}");
        Console.WriteLine($"  Password: {config.password}");
        Console.WriteLine($"  Channel: {config.channel}");
        Console.WriteLine($"  Bandwidth: {config.bandwidth}");
        Console.WriteLine($"  Encryption Mode: {config.encryptionMode}");
        Console.WriteLine();
        Console.WriteLine(config.ToString());
    }
}
