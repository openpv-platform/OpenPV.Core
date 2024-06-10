using Ahsoka.ServiceFramework;
using Ahsoka.System;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ahsoka.Services.Bluetooth.Platform;

internal class BluetoothServiceTests
{
    private static int _deviceCount = 0;
    private static readonly List<BluetoothDeviceDiscoveryInfo> _devices = new();

    public static void TestBluetoothService()
    {
        string userInput = "";

        Console.WriteLine($"Starting BluetoothService Test");

        // Start client for BluetoothService
        var client = new BluetoothServiceClient();
        client.NotificationReceived += BTClient_NotificationReceived;
        var clientRuntime = AhsokaRuntime.CreateBuilder()
                     .AddClients(client)
                     .StartWithInternalServices();

        BluetoothConfiguration configuration = client.GetAdapterConfiguration();
        BluetoothAdapterSetup adapterConfig = configuration.AdapterConfigurations.First();

        int adapterIndex = adapterConfig.AdapterIndex;

        Console.WriteLine();
        Console.WriteLine($"Adapter Configuration:");
        Console.WriteLine($"    Stack             : [{adapterConfig.Stack}]");
        Console.WriteLine($"    Index             : [{adapterConfig.AdapterIndex}]");
        Console.WriteLine($"    Path              : [{adapterConfig.DeviceInterfacePath}]");
        Console.WriteLine($"    Name              : [{adapterConfig.DeviceDiscoveryName}]");
        Console.WriteLine($"    MAC Addr          : [{adapterConfig.DeviceMacAddress}]");
        Console.WriteLine($"    Major             : [{adapterConfig.MajorDeviceClass}]");

        if (adapterConfig.MajorDeviceClass == BluetoothMajorDeviceClass.AudioVideo)
            Console.WriteLine($"    Minor             : [{(AudioVideoMinorDeviceClass)adapterConfig.MinorDeviceClass}]");
        else if (adapterConfig.MajorDeviceClass == BluetoothMajorDeviceClass.Phone)
            Console.WriteLine($"    Minor             : [{(PhoneMinorDeviceClass)adapterConfig.MinorDeviceClass}]");
        else
            Console.WriteLine($"    Minor             : [{adapterConfig.MinorDeviceClass}]");

        Console.WriteLine($"    Service           : [{adapterConfig.ServiceClass}]");
        Console.WriteLine($"    Discovery Timeout : [{adapterConfig.DiscoveryTimeout}]");
        Console.WriteLine($"    Connection Timeout: [{adapterConfig.ConnectionTimeout}]");
        Console.WriteLine($"    Pairing Timeout   : [{adapterConfig.PairingTimeout}]");
        Console.WriteLine();
        Console.Out.Flush();

        // Enable adapter
        client.EnableAdapter(adapterIndex);

        // Set Discoverable
        client.SetAdapterState(adapterIndex, true, true);

        // Get and display adapter state
        BluetoothAdapterState response = client.RequestAdapterState(adapterIndex);

        if (response != null)
        {
            Console.WriteLine($"Adapter State:");
            Console.WriteLine($"    IsEnabled         : [{response.IsEnabled}]");
            Console.WriteLine($"    IsDiscoverable    : [{response.IsDiscoverable}]");
            Console.WriteLine($"    IsConnectable     : [{response.IsConnectable}]");
            Console.WriteLine($"    IsConnected       : [{response.IsConnected}]");
            Console.WriteLine($"    IsPolling         : [{response.IsPolling}]");
            Console.WriteLine();
            Console.Out.Flush();
        }
        else
        {
            Console.WriteLine($"Adapter State response was null.");
            Console.Out.Flush();
        }

        // Do device discovery scan
        _deviceCount = 0;
        _devices.Clear();

        Console.WriteLine($"Scanning for {adapterConfig.DiscoveryTimeout / 1000} sec.");
        Console.WriteLine(); Console.Out.Flush();

        // Send Discovery Request which will wait for scan to complete
        client.StartDiscovery(adapterIndex);

        Console.WriteLine();
        Console.WriteLine($"Scan complete. {_deviceCount} device(s) found.");
        Console.WriteLine(); Console.Out.Flush();

        BluetoothDeviceDiscoveryInfo deviceInfo;
        BluetoothPairedDevices pairedDevices;

        PrintHelp();

        bool done = false;

        while (!done)
        {
            // Prompt
            PrintPrompt();

            userInput = Console.ReadLine();

            if ((userInput == null) || (userInput.Length == 0))
                continue;

            string command = userInput.ToLower();
            string[] tokens = command.Split(new char[] { ' ' }).ToArray();
            string primaryCommand = tokens.First();

            if (tokens.Length > 1)
                tokens = tokens.Skip(1).ToArray();

            switch (primaryCommand)
            {
                case "?":
                case "h":
                case "help":
                    PrintHelp();
                    break;

                case "q":
                case "quit":
                case "exit":
                    done = true;
                    break;

                case "c":
                case "connect":
                    deviceInfo = GetDeviceInfo(tokens);

                    if (deviceInfo == null)
                    {
                        PrintHelp();
                        continue;
                    }

                    // Connect the device
                    ProfileConnectionState connectionState = client.RequestConnect(adapterIndex, deviceInfo.DeviceMacAddress, BluetoothProfiles.Default); // Automatic/generic connect request

                    if (connectionState.IsConnected)
                        Console.WriteLine($"Successfully connected to device [{connectionState.DeviceMacAddress}]");
                    else
                        Console.WriteLine($"Failed to connect to Device [{connectionState.DeviceMacAddress}]");
                    break;

                case "d":
                case "disconnect":
                    deviceInfo = GetDeviceInfo(tokens);

                    if (deviceInfo == null)
                    {
                        PrintHelp();
                        continue;
                    }

                    Console.WriteLine($"Sending disconnect for [{deviceInfo.DeviceMacAddress}] [{deviceInfo.DeviceName}] ");

                    // Disconnect the device
                    client.RequestDisconnect(adapterIndex, deviceInfo.DeviceMacAddress, BluetoothProfiles.Default);

                    break;

                case "l":
                case "list":
                    int i = 0;
                    foreach (var devInfo in _devices)
                    {
                        Console.WriteLine($"Device:[{++i,3}] -  {devInfo.DeviceMacAddress} RSSI: {devInfo.DeviceRssi,4} [{devInfo.DeviceName}]");
                    }
                    break;

                case "lp":
                case "listpaired":
                    int pairedCount = 0;

                    // Request a list of paired devices
                    pairedDevices = client.RequestPairedDevices(adapterIndex);

                    Console.WriteLine("");
                    Console.WriteLine("Paired devices:");

                    foreach (BluetoothDevicePairedInfo pairedDeviceInfo in pairedDevices.PairedDeviceLists)
                    {
                        int deviceIndex;

                        pairedCount++;
                        deviceIndex = _devices.FindIndex(x => x.DeviceMacAddress == pairedDeviceInfo.DeviceMacAddress);
                        Console.WriteLine($"Device:[{deviceIndex + 1,2}] -  {pairedDeviceInfo.DeviceMacAddress} RSSI: {pairedDeviceInfo.DeviceRssi,4} [{pairedDeviceInfo.DeviceName}]");
                    }

                    Console.WriteLine($"[{pairedCount}] paired devices found");
                    Console.WriteLine("");
                    break;

                case "s":
                case "scan":
                    _deviceCount = 0;
                    _devices.Clear();

                    Console.WriteLine($"Scanning for {adapterConfig.DiscoveryTimeout / 1000} sec.");
                    Console.WriteLine(); Console.Out.Flush();

                    // Send Discovery Request which will wait for scan to complete
                    client.StartDiscovery(adapterIndex);

                    Console.WriteLine();
                    Console.WriteLine($"Scan complete. {_deviceCount} device(s) found.");
                    Console.WriteLine(); Console.Out.Flush();
                    break;

                case "u":
                case "unpair":
                    deviceInfo = GetDeviceInfo(tokens);

                    if (deviceInfo == null)
                    {
                        PrintHelp();
                        continue;
                    }

                    Console.WriteLine($"Sending request to unpair for [{deviceInfo.DeviceMacAddress}] [{deviceInfo.DeviceName}] ");

                    // Unpair the device
                    client.RequestToUnpair(adapterIndex, deviceInfo.DeviceMacAddress);
                    break;

                case "p":
                case "pair":
                    deviceInfo = GetDeviceInfo(tokens);

                    if (deviceInfo == null)
                    {
                        PrintHelp();
                        continue;
                    }

                    // Pair the device
                    BluetoothDevicePairedInfo pairingState = client.RequestToPair(adapterIndex, deviceInfo.DeviceMacAddress);

                    if (pairingState.Paired)
                        Console.WriteLine($"Successfully paired to device [{pairingState.DeviceMacAddress}]");
                    else
                        Console.WriteLine($"Failed to pair to Device [{pairingState.DeviceMacAddress}]");
                    break;

                default:
                    Console.WriteLine($"Unknown command [{primaryCommand}]");
                    Console.Out.Flush();
                    break;

            } // end of switch(primaryCommand)

        } // end of while (!done)

        // Stop the Runtimes
        clientRuntime.RequestShutdown();
        AhsokaRuntime.ShutdownAll();

        Console.WriteLine();
        Console.WriteLine("BluetoothService Test done"); Console.Out.Flush();

    } // end of method TestBluetoothService()

    private static void BTClient_NotificationReceived(object sender, NotificationEventArgs<BluetoothMessageTypes.Ids> e)
    {
        if (e.TransportId == BluetoothMessageTypes.Ids.DiscoveryDeviceNotification)
        {
            _deviceCount++;

            if (e.NotificationObject is not BluetoothDeviceDiscoveryInfo device)
                Console.WriteLine($"NULL DEVICE for Discovery Notification !!!!!!!!!");
            else
            {
                Console.WriteLine($"Device Found:[{_deviceCount,3}] -  {device.DeviceMacAddress} RSSI: {device.DeviceRssi,4} [{device.DeviceName}]");
                _devices.Add(device);
            }
        } // end of if (e.TransportId == BluetoothMessageTypes.Ids.DiscoveryDeviceNotification)

        if (e.TransportId == BluetoothMessageTypes.Ids.PairedDeviceNotification)
        {
            if (e.NotificationObject is not BluetoothDevicePairedInfo pairedInfo)
            {
                Console.WriteLine($"NULL Pairing Information for Paired Device Notification !!!!!!!!!");
            }
            else
            {
                if (pairedInfo.Paired)
                    if (pairedInfo.DeviceName != "")
                        Console.WriteLine($"Device [{pairedInfo.DeviceMacAddress}] [{pairedInfo.DeviceName}] paired");
                    else
                        Console.WriteLine($"Device [{pairedInfo.DeviceMacAddress}] paired");
                else
                    if (pairedInfo.DeviceName != "")
                    Console.WriteLine($"Device [{pairedInfo.DeviceMacAddress}] [{pairedInfo.DeviceName}] not paired");
                else
                    Console.WriteLine($"Device [{pairedInfo.DeviceMacAddress}] not paired");
            }
        } // end of if (e.TransportId == BluetoothMessageTypes.Ids.ProfileConnectionStateChanged)

        if (e.TransportId == BluetoothMessageTypes.Ids.ProfileConnectionStateChanged)
        {
            if (e.NotificationObject is not ProfileConnectionState connectionState)
            {
                Console.WriteLine($"NULL Profile Connection State for Connection State Changed Notification !!!!!!!!!");
            }
            else
            {
                if (connectionState.IsConnected)
                    Console.WriteLine($"Device [{connectionState.DeviceMacAddress}] connected");
                else
                    Console.WriteLine($"Device [{connectionState.DeviceMacAddress}] disconnected");
            }
        } // end of if (e.TransportId == BluetoothMessageTypes.Ids.ProfileConnectionStateChanged)

    } // end of method BTClient_NotificationReceived()

    private static BluetoothDeviceDiscoveryInfo GetDeviceInfo(string[] tokens)
    {
        if ((tokens == null) || (tokens.Length == 0))
            return null;

        if (!int.TryParse(tokens[0], out var deviceIndex))
            return null;

        deviceIndex -= 1;       // zero based index

        if ((deviceIndex < 0) || (deviceIndex > _devices.Count))
            return null;

        return _devices[deviceIndex];

    } // end of method GetDeviceInfo()

    private static void PrintHelp()
    {
        Console.WriteLine(" ?|h|help                   - help");
        Console.WriteLine(" q|quit|exit                - quit");
        Console.WriteLine(" c|connect    <deviceIndex> - connect device");
        Console.WriteLine(" d|disconnect <deviceIndex> - disconnect device");
        Console.WriteLine(" l|list                     - list found devices");
        Console.WriteLine("lp|listpaired               - list paired devices");
        Console.WriteLine(" p|pair       <deviceIndex> - pair device");
        Console.WriteLine(" s|scan                     - scan for devices (discover)");
        Console.WriteLine(" u|unpair     <deviceIndex> - unpair device");
        Console.WriteLine("");
        Console.Out.Flush();
    } // end of method PrintHelp()

    private static void PrintPrompt()
    {
        Console.Write(">> ");
        Console.Out.Flush();
    } // end of method PrintPrompt()

} // end of class BluetoothTests

