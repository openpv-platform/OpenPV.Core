using Linux.Bluetooth;
using Linux.Bluetooth.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Bluetooth.Platform;

// Command line monitor/test for Bluetooth using the Bluez and Linux.Bluetooth packages.
//   This class does not use the Ahsoka messaging framework. The class is an encaptulation
//   of a test program developed for environmental testing for the OpenView display.
//
[ExcludeFromCodeCoverage]
internal class BluetoothDevMon
{
    public static readonly string BTDevMonVersion = "1.0.8";

    public const int SUCCESS = 0;
    public const int ERROR = -1;

    public const int GlobalTimeout = 15;    // seconds

    private static Adapter _monitorDeviceAdapter = null;
    private static Device _monitorDeviceDevice = null;

    private static bool _monitorDeviceFirstTime = true;
    private static bool _interactive = false;

    private static readonly string _defaultCmdFilename = "btdevmon_cmd";
    private static readonly string _defaultResFilename = "btdevmon_res";
    private static readonly string _defaultMacFilename = "btdevmon_mac";

    private static int _preScanDeviceCount;
    private static int _newScanDeviceCount;

    public static int BTDevMon(string[] args)
    {
        bool done = false;
        int retval;

        if ((args == null) || (args.Length == 0))
            _interactive = true;

        if (_interactive == false)
        {
            retval = ProcessCommmandLine(args);
            return retval;
        }

        PrintWelcome(BTDevMonVersion);

        while (!done)
        {
            Prompt();
            string readCommand = Console.ReadLine();

            if ((readCommand != null) && (readCommand.Length != 0))
            {
                string command = readCommand.ToLower();
                string[] tokens = command.Split(new char[] { ' ' }).ToArray();
                string primaryCommand = tokens.First();

                if (tokens.Length > 1)
                    tokens = tokens.Skip(1).ToArray();

                switch (primaryCommand)
                {
                    case "?":
                    case "help":
                        PrintHelp(tokens!);
                        break;
                    case "exit":
                    case "q":
                    case "quit":
                        QuitCmd();
                        done = true;
                        break;
                    case "disconnect":
                        DisConnectCmd(tokens!);
                        break;
                    case "connect":
                        ConnectCmd(tokens!);
                        break;
                    case "list":
                        ListCmd(tokens!);
                        break;
                    case "monitor":
                        MonitorCmd(tokens!, new StreamWriter(Console.OpenStandardOutput()), 30);
                        break;
                    case "observe":
                        ObserveCmd(tokens!);
                        break;
                    case "pair":
                        PairCmd(tokens!);
                        break;
                    case "remove":
                        RemoveCmd(tokens!);
                        break;
                    case "set":
                        SetCmd(tokens!);
                        break;
                    case "show":
                        ShowCmd(tokens!);
                        break;
                    default:
                        Console.WriteLine("Unknown command [" + primaryCommand + "]");
                        break;
                } // end of switch (primaryCommand)

            } // end of if ( (readCommand != null) && (readCommand.Length != 0) )
        } // end of while (!done)

        return SUCCESS;

    } // end of BTDevMon()

    private static readonly IDictionary<string, string> _commandSyntax = new Dictionary<string, string>()
    {
        { "?",         "? [cmd|all]" },
        { "help",      "help [cmd|all]" },
        { "exit",      "exit" },
        { "q",         "q[uit]" },
        { "quit",      "q[uit]" },
        { "connect",   "connect device [adapter_name] <device_address>" },
        { "disconnect","disconnect device [adapter_name] <device_address>" },
        { "list",      "list adapters\n" +
                       "\tlist devices [<adapter_name>] [<scan_timeout>]\n" +
                       "\tlist devicenames [<adapter_name>] [<scan_timeout>]\n" +
                       "\tlist macs [<adapter_name>] [<scan_timeout>]"  },
        { "monitor",   "monitor device [adapter_name] <device_address> [numOfCycles] [cycleTime]" },
        { "observe",   "observe file [<cmd_filename>] [<response_filename>] (defaults to btdevmon_cmd and btdevmon_res)" },
        { "pair",      "pair device [adapter_name] <device_address>" },
        { "remove",    "remove device [adapter_name] <device_address>|all" },
        { "set",       "set adapter [adapter_name] <property> [<value>]\n" +
                       "\t   settable properties:\n" +
                       "\t     alias    | discoverable    | discoverabletimeout | \n" +
                       "\t     pairable | pairabletimeout | powered" },
        { "show",      "show adapter [adapter_name]\n" +
                       "\tshow device  [adapter_name] <device_address>|all\n" +
                       "\tshow version" }
    };

    private static int ConnectCmd(string[] tokens)
    {
        if ((tokens == null) || (tokens.Length == 0))
        {
            PrintConnectHelp();
        }
        else
        {
            switch (tokens[0])
            {
                case "device":
                    // Check the adapter name
                    if (tokens.Length > 2)
                    {
                        ConnectDevice(tokens[1], tokens[2]);
                    }
                    else if (tokens.Length > 1)
                    {
                        ConnectDevice("none", tokens[1]);
                    }
                    else
                    {
                        Console.WriteLine("missing device name");
                    }
                    break;

                default:
                    PrintConnectHelp();
                    break;
            } // end of switch (tokens[0])
        } // end of if (tokens.Length == 0 )

        return SUCCESS;

    } // end of method ConnectCmd()

    private static int DisConnectCmd(string[] tokens)
    {
        if ((tokens == null) || (tokens.Length == 0))
        {
            PrintDisconnectHelp();
        }
        else
        {
            switch (tokens[0])
            {
                case "device":
                    // Check the adapter name
                    if (tokens.Length > 2)
                    {
                        DisconnectDevice(tokens[1], tokens[2]);
                    }
                    else if (tokens.Length > 1)
                    {
                        DisconnectDevice("none", tokens[1]);
                    }
                    else
                    {
                        Console.WriteLine("missing device name");
                    }
                    break;

                default:
                    PrintDisconnectHelp();
                    break;
            } // end of switch (tokens[0])
        } // end of if (tokens.Length == 0 )

        return SUCCESS;

    } // end of method DisconnectCmd()

    private static int ListCmd(string[] tokens)
    {
        if ((tokens == null) || (tokens.Length == 0))
        {
            PrintListHelp();
        }
        else
        {
            string adapterName = "none";
            int timeout = 15;

            switch (tokens[0])
            {
                case "adapters":
                    ListAdapters();
                    Console.Out.Flush();
                    break;

                case "devices":
                    if (tokens.Length == 2)
                    {
                        if (!int.TryParse(tokens[1], out int scanTimeout))
                        {
                            adapterName = tokens[1];
                        }
                        else
                        {
                            timeout = scanTimeout;
                        }
                    }
                    else if (tokens.Length == 3)
                    {
                        adapterName = tokens[1];

                        if (!int.TryParse(tokens[2], out int scanTimeout))
                        {
                            Console.WriteLine($"Bad scan timeout value = [{tokens[2]}]; using default of {timeout} seconds");
                        }
                        else
                        {
                            timeout = scanTimeout;
                        }
                    }

                    ListDevices(adapterName, timeout, true);
                    break;

                case "devicenames":
                    if (tokens.Length == 2)
                    {
                        if (!int.TryParse(tokens[1], out int scanTimeout))
                        {
                            adapterName = tokens[1];
                        }
                        else
                        {
                            timeout = scanTimeout;
                        }
                    }
                    else if (tokens.Length == 3)
                    {
                        adapterName = tokens[1];

                        if (!int.TryParse(tokens[2], out int scanTimeout))
                        {
                            Console.WriteLine($"Bad scan timeout value = [{tokens[2]}]; using default of {timeout} seconds");
                        }
                        else
                        {
                            timeout = scanTimeout;
                        }
                    }

                    ListDevices(adapterName, timeout, false);
                    break;

                case "macs":
                    if (tokens.Length == 2)
                    {
                        if (!int.TryParse(tokens[1], out int scanTimeout))
                        {
                            adapterName = tokens[1];
                        }
                        else
                        {
                            timeout = scanTimeout;
                        }
                    }
                    else if (tokens.Length == 3)
                    {
                        adapterName = tokens[1];

                        if (!int.TryParse(tokens[2], out int scanTimeout))
                        {
                            Console.WriteLine($"Bad scan timeout value = [{tokens[2]}]; using default of {timeout} seconds");
                        }
                        else
                        {
                            timeout = scanTimeout;
                        }
                    }

                    ListMACs(adapterName, timeout).Wait();
                    break;

                default:
                    PrintListHelp();
                    break;
            } // end of switch (tokens[0])
        } // end of if (tokens.Length == 0 )

        return SUCCESS;
    } // end of method ListCmd()

    private static int MonitorCmd(string[] tokens, StreamWriter writer, int defaultNumOfCycles)
    {
        if ((tokens == null) || (tokens.Length == 0))
        {
            PrintMonitorHelp(writer);
        }
        else
        {
            switch (tokens[0])
            {
                case "device":
                    if (tokens.Length >= 5)
                    {
                        MonitorDevice(tokens[1], tokens[2], writer, Int32.Parse(tokens[3]), Int32.Parse(tokens[4]));
                    }
                    else if (tokens.Length > 3)
                    {
                        MonitorDevice(tokens[1], tokens[2], writer, Int32.Parse(tokens[3]), 1);
                    }
                    else if (tokens.Length > 2)
                    {
                        MonitorDevice(tokens[1], tokens[2], writer, defaultNumOfCycles, 1);
                    }
                    else if (tokens.Length > 1)
                    {
                        MonitorDevice("none", tokens[1], writer, defaultNumOfCycles, 1);
                    }
                    else
                    {
                        writer.WriteLine("MonitorCmd: missing device name");
                        writer.Flush();
                    }
                    break;

                default:
                    PrintMonitorHelp(writer);
                    break;
            } // end of switch (tokens[0])
        } // end of if (tokens.Length == 0 )

        return SUCCESS;

    } // end of method MonitorCmd()

    private static int ObserveCmd(string[] tokens)
    {
        if ((tokens == null) || (tokens.Length == 0))
        {
            PrintObserveHelp();
        }
        else
        {
            switch (tokens[0])
            {
                case "file":
                    // Check the filenames
                    if (tokens.Length >= 2)
                    {
                        Console.WriteLine($"Using command file [{tokens[1]}] and results file [{tokens[2]}]");
                        ObserveFile(tokens[1], tokens[2]);
                    }
                    else if (tokens.Length > 1)
                    {
                        Console.WriteLine($"Using command file [{tokens[1]}] and results file [{_defaultResFilename}]");
                        ObserveFile(tokens[1], _defaultResFilename);
                    }
                    else
                    {
                        Console.WriteLine($"Using default command file [{_defaultCmdFilename}] and results file [{_defaultResFilename}]");
                        ObserveFile(_defaultCmdFilename, _defaultResFilename);
                    }
                    break;

                default:
                    PrintObserveHelp();
                    break;
            } // end of switch (tokens[0])
        } // end of if (tokens.Length == 0 )

        return SUCCESS;

    } // end of method ObserveCmd()

    private static int PairCmd(string[] tokens)
    {
        if ((tokens == null) || (tokens.Length == 0))
        {
            PrintPairHelp();
        }
        else
        {
            switch (tokens[0])
            {
                case "device":
                    // Check the adapter name
                    if (tokens.Length > 2)
                    {
                        PairDevice(tokens[1], tokens[2]);
                    }
                    else if (tokens.Length > 1)
                    {
                        PairDevice("none", tokens[1]);
                    }
                    else
                    {
                        Console.WriteLine("missing device name");
                    }
                    break;

                default:
                    PrintPairHelp();
                    break;
            } // end of switch (tokens[0])
        } // end of if (tokens.Length == 0 )

        return SUCCESS;

    } // end of method PairCmd()

    private static int RemoveCmd(string[] tokens)
    {
        if ((tokens == null) || (tokens.Length == 0))
        {
            PrintRemoveHelp();
        }
        else
        {
            switch (tokens[0])
            {
                case "device":
                    // Check the adapter name
                    if (tokens.Length > 2)
                    {
                        RemoveDevice(tokens[1], tokens[2]);
                    }
                    else if (tokens.Length > 1)
                    {
                        RemoveDevice("none", tokens[1]);
                    }
                    else
                    {
                        Console.WriteLine("missing device name");
                    }
                    break;

                default:
                    PrintRemoveHelp();
                    break;
            } // end of switch (tokens[0])
        } // end of if (tokens.Length == 0 )

        return SUCCESS;

    } // end of method RemoveCmd()

    private static int SetCmd(string[] tokens)
    {
        if ((tokens == null) || (tokens.Length == 0))
        {
            PrintSetHelp();
        }
        else
        {
            switch (tokens[0])
            {
                case "adapter":
                    // Check for arguments
                    switch (tokens.Length)
                    {
                        case 1:
                            Console.WriteLine("Error: missing arguments");
                            Console.Out.Flush();
                            break;

                        case 2:
                            if (IsAdapterPropertyBoolean(tokens[1]))
                                SetAdapter("none", tokens[1], "true");
                            else
                                Console.WriteLine("Error: Property requires a value");
                            break;

                        case 3:
                            if (IsAdapterProperty(tokens[1]))
                                SetAdapter("none", tokens[1], tokens[2]);
                            else if (IsAdapterPropertyBoolean(tokens[2]))
                                SetAdapter(tokens[1], tokens[2], "true");
                            else
                                Console.WriteLine("Error: Wrong number/type of arguments");
                            break;

                        case 4:
                            SetAdapter(tokens[1], tokens[2], tokens[3]);
                            break;

                    }
                    Console.Out.Flush();
                    break;


                default:
                    PrintSetHelp();
                    break;
            } // end of switch (tokens[0])
        } // end of if (tokens.Length == 0 )

        return SUCCESS;

    } // end of method SetCmd()

    private static int ShowCmd(string[] tokens)
    {
        if ((tokens == null) || (tokens.Length == 0))
        {
            PrintShowHelp();
        }
        else
        {
            switch (tokens[0])
            {
                case "adapter":
                    string adapterName = "none";

                    // Check for adapter name
                    if (tokens.Length > 1)
                        adapterName = tokens[1];
                    ShowAdapter(adapterName);
                    Console.Out.Flush();
                    break;

                case "device":
                    // Check the adapter name
                    if (tokens.Length > 2)
                    {
                        ShowDevice(tokens[1], tokens[2]);
                    }
                    else if (tokens.Length > 1)
                    {
                        ShowDevice("none", tokens[1]);
                    }
                    else
                    {
                        Console.WriteLine("missing device name");
                    }
                    break;

                case "ver":
                case "version":
                    Console.WriteLine("bt_devmon version " + BTDevMonVersion);
                    Console.Out.Flush();
                    break;

                default:
                    PrintShowHelp();
                    break;
            } // end of switch (tokens[0])
        } // end of if (tokens.Length == 0 )

        return SUCCESS;

    } // end of method ShowCmd()

    private static int PrintHelp(string[] tokens)
    {
        if ((tokens == null) ||
                (tokens.Length == 0) ||
                ((tokens.Length == 1) &&
                ((tokens[0] == "help") || (tokens[0] == "?"))))
        {
            Console.WriteLine("\t?                    - Command help");
            Console.WriteLine("\thelp [cmd|all]       - Command syntax");
            Console.WriteLine("\tq[uit]               - Exit bt_devmon");
            PrintConnectHelp();
            PrintDisconnectHelp();
            PrintListHelp();
            PrintMonitorHelp(new StreamWriter(Console.OpenStandardOutput()));
            PrintObserveHelp();
            PrintPairHelp();
            PrintRemoveHelp();
            PrintSetHelp();
            PrintShowHelp();
        }
        else if ((tokens.Length == 1) && (tokens[0] == "all"))
        {
            foreach (KeyValuePair<string, string> entry in _commandSyntax)
                if (entry.Key != "q")
                    Console.WriteLine("\t" + entry.Value);
        }
        else
        {
            string cmd = tokens[0];

            // Print command syntax
            if (_commandSyntax.ContainsKey(cmd))
            {
                _commandSyntax.TryGetValue(cmd, out string cmdSyntax);
                if (cmdSyntax != null)
                    Console.WriteLine("\t" + cmdSyntax);
            }
            else
            {
                Console.WriteLine("Unknown command [" + cmd + "]");
            }
        } // end of if ( tokens[0] == "" )

        Console.Out.Flush();

        return SUCCESS;
    } // end of method PrintHelp()

    private static void PrintConnectHelp()
    {
        Console.WriteLine("\tconnect device       - Connect to a given device");

    } // end of method PrintConnectHelp()

    private static void PrintDisconnectHelp()
    {
        Console.WriteLine("\tdisconnect device    - Disconnect a given device");

    } // end of method PrintDisconnectHelp()

    private static void PrintListHelp()
    {
        Console.WriteLine("\tlist adapters        - List available adapters");
        Console.WriteLine("\tlist devices         - List available devices (performs scan) with aliases");
        Console.WriteLine("\tlist devicenames     - List available devices (performs scan) with names");
        Console.WriteLine("\tlist macs            - Print available device MAC addresses to file btdevmon_mac");

    } // end of method PrintListHelp()

    private static void PrintMonitorHelp(StreamWriter writer)
    {
        writer.WriteLine("\tmonitor device       - Monitor and report connect status on a given device");
        writer.Flush();

    } // end of method PrintMonitorHelp()

    private static void PrintObserveHelp()
    {
        Console.WriteLine("\tobserve file         - Observe a filesystem interface");

    } // end of method PrintObserveHelp()

    private static void PrintPairHelp()
    {
        Console.WriteLine("\tpair device          - Pair with a given device");

    } // end of method PrintPairHelp()

    private static void PrintRemoveHelp()
    {
        Console.WriteLine("\tremove device        - Remove a given device from adapter");

    } // end of method PrintRemoveHelp()

    private static void PrintSetHelp()
    {
        Console.WriteLine("\tset  adapter         - Set adapter property");

    } // end of method PrintShowHelp()

    private static void PrintShowHelp()
    {
        Console.WriteLine("\tshow adapter         - Show adapter information");
        Console.WriteLine("\tshow device          - Show device  information");
        Console.WriteLine("\tshow version         - Show application version");

    } // end of method PrintShowHelp()

    private static int PrintWelcome(string BTDevMonVersion)
    {
        string[] tokens = { "help" };
        Console.WriteLine("bt_devmon version " + BTDevMonVersion);
        Console.WriteLine("Available commands:");
        PrintHelp(tokens);
        return SUCCESS;
    } // end of method PrintWelcome()

    private static void Prompt()
    {
        Console.Write("bt_devmon>> ");
        Console.Out.Flush();
    } // end of method Prompt()

    private static void QuitCmd()
    {
        Console.WriteLine("Exiting bt_devmon");
        Console.Out.Flush();
        Environment.Exit(0);
    } // end of method QuitCmd()

    private static int ConnectDevice(string cmdAdapterName, string cmdDeviceName)
    {
        cmdDeviceName = cmdDeviceName.ToUpper();

        TimeSpan timeout = TimeSpan.FromSeconds(GlobalTimeout);

        Adapter adapter = FindAdapter(cmdAdapterName);

        if (adapter != null)
        {
            var adapterPath = adapter!.ObjectPath.ToString();
            var adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);

            // Ensure adapter is powered
            Adapter1Properties adapterProperties1 = adapter!.GetAllAsync().GetAwaiter().GetResult();

            if (adapterProperties1.Powered == false)
            {
                Console.WriteLine($"Powering up adapter [{adapterName}]");
                adapter.SetPoweredAsync(true).Wait();
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            Device device = adapter.GetDeviceAsync(cmdDeviceName).GetAwaiter().GetResult();

            if (device == null)
            {
                Console.WriteLine($"Could not find device {cmdDeviceName}");
                Console.WriteLine("Scanning...");
                adapter.StartDiscoveryAsync().Wait();
                Task.Delay(timeout).Wait();
                adapter.StopDiscoveryAsync().Wait();
                Console.WriteLine("Scan completed");

                device = adapter.GetDeviceAsync(cmdDeviceName).GetAwaiter().GetResult();

                if (device == null)
                {
                    Console.WriteLine($"Device {cmdDeviceName} not found!");
                    Console.Out.Flush();
                    return ERROR;
                } // end of if (device == null)

            } // end of if (device == null)

            DeviceProperties deviceProperties = device.GetPropertiesAsync().GetAwaiter().GetResult();

            // Ensure adapter is discoverable
            if (adapterProperties1.Discoverable == false)
            {
                Console.WriteLine($"Setting adapter [{adapterName}] discoverable");
                adapter.SetDiscoverableAsync(true).Wait();
                adapter.SetDiscoverableTimeoutAsync(180).Wait();
            }

            // Ensure adapter is pairable
            if (adapterProperties1.Pairable == false)
            {
                Console.WriteLine($"Setting adapter [{adapterName}] pairable");
                adapter.SetPairableAsync(true).Wait();
                adapter.SetPairableTimeoutAsync(30).Wait();
            }

            device.SetTrustedAsync(true).Wait();

            bool connected = device.GetConnectedAsync().GetAwaiter().GetResult();

            if (connected == false)
            {
                Console.Write("Connecting...");

                try
                {
                    device.ConnectAsync().Wait();
                }
                catch (AggregateException e)
                {
                    if (e.Message.EndsWith("Host is down)"))
                    {
                        Console.WriteLine("failed");
                        Console.WriteLine($"ConnectDevice: device [{deviceProperties.Address}][{deviceProperties.Alias}] is not powered");
                        Console.Out.Flush();
                    }
                    else
                    {
                        Console.WriteLine("failed");
                        Console.WriteLine($"ConnectDevice: Exception [{e.Message}]");
                        Console.Out.Flush();
                    }
                    return ERROR;
                }

                device.WaitForPropertyValueAsync("Connected", value: true, timeout).Wait();
                Console.WriteLine($"\rConnected to [{deviceProperties.Address}][{deviceProperties.Alias}]");
            }
            else
            {
                Console.WriteLine($"Device [{deviceProperties.Address}][{deviceProperties.Alias}] already connected");
            }
        }
        else
        {
            return ERROR;
        } // end of if (adapter != null)

        Console.Out.Flush();
        return SUCCESS;
    } // end of method ConnectDevice()

    private static int DisconnectDevice(string cmdAdapterName, string cmdDeviceName)
    {
        cmdDeviceName = cmdDeviceName.ToUpper();

        TimeSpan timeout = TimeSpan.FromSeconds(GlobalTimeout);

        Adapter adapter = FindAdapter(cmdAdapterName);

        if (adapter != null)
        {
            var adapterPath = adapter!.ObjectPath.ToString();
            var adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);

            // Ensure adapter is powered
            Adapter1Properties adapterProperties1 = adapter!.GetAllAsync().GetAwaiter().GetResult();

            if (adapterProperties1.Powered == false)
            {
                Console.WriteLine($"Powering up adapter [{adapterName}]");
                adapter.SetPoweredAsync(true).Wait();
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            Device device = adapter.GetDeviceAsync(cmdDeviceName).GetAwaiter().GetResult();

            if (device == null)
            {
                Console.WriteLine($"Could not find device {cmdDeviceName}");
                Console.WriteLine("Scanning...");
                adapter.StartDiscoveryAsync().Wait();
                Task.Delay(timeout).Wait();
                adapter.StopDiscoveryAsync().Wait();
                Console.WriteLine("Scan completed");

                device = adapter.GetDeviceAsync(cmdDeviceName).GetAwaiter().GetResult();

                if (device == null)
                {
                    Console.WriteLine($"Device {cmdDeviceName} not found!");
                    Console.Out.Flush();
                    return ERROR;
                } // end of if (device == null)

            } // end of if (device == null)

            DeviceProperties deviceProperties = device.GetPropertiesAsync().GetAwaiter().GetResult();

            device.SetTrustedAsync(true).Wait();

            bool connected = device.GetConnectedAsync().GetAwaiter().GetResult();

            if (connected == false)
            {
                Console.WriteLine($"Device [{deviceProperties.Address}][{deviceProperties.Alias}] already disconnected");
                return SUCCESS;
            }

            device.DisconnectAsync().Wait();
            Console.WriteLine($"Device [{deviceProperties.Address}][{deviceProperties.Alias}] disconnected");
        }
        else
        {
            Console.WriteLine($"Adapter {cmdAdapterName} not found!");
            Console.Out.Flush();
            return ERROR;
        } // end of if (adapter != null)

        Console.Out.Flush();
        return SUCCESS;
    } // end of method DisconnectDevice()

    private static int ListAdapters()
    {
        int i = 1;

        IReadOnlyList<Adapter> adapters = BlueZManager.GetAdaptersAsync().GetAwaiter().GetResult();

        if (adapters.Count == 0)
        {
            Console.WriteLine("No adapters found!");
            Console.Out.Flush();
            return ERROR;
        }
        else
        {
            Console.WriteLine("Available adapters:");

            foreach (Adapter adapter in adapters)
            {
                Console.WriteLine($"[{i++,3}] - {adapter.ObjectPath}");
            } // end of foreach (Adapter adapter in adapters)
        } // end of if ( adapters.Count == 0 )

        Console.Out.Flush();
        return SUCCESS;
    } // end of method ListAdapters()

    private static int ListDevices(string cmdAdapterName, int scanSeconds, bool useAlias)
    {
        Adapter adapter;

        if (cmdAdapterName != "none")
        {
            adapter = BlueZManager.GetAdapterAsync(cmdAdapterName).GetAwaiter().GetResult();
            if (adapter == null)
                return ERROR;
        }
        else
        {
            var adapters = BlueZManager.GetAdaptersAsync().GetAwaiter().GetResult();
            if (adapters.Count == 0)
            {
                Console.WriteLine("No adapters found!");
                Console.Out.Flush();
                return ERROR;
            }

            adapter = adapters[0];
        }

        var adapterPath = adapter.ObjectPath.ToString();
        var adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);
        Console.WriteLine($"Using Bluetooth adapter {adapterName}");

        // Ensure adapter is powered
        Adapter1Properties adapterProperties = adapter!.GetAllAsync().GetAwaiter().GetResult();

        if (adapterProperties.Powered == false)
        {
            Console.WriteLine($"Adapter {adapter!.ObjectPath} is not powered!");
            Console.WriteLine($"Powering up adapter");
            Console.Out.Flush();
            adapter.SetPoweredAsync(true).Wait();
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        _preScanDeviceCount = 0;
        _newScanDeviceCount = 0;

        Console.WriteLine($"Scanning for {scanSeconds} seconds...");
        Console.Out.Flush();

        if (useAlias)
            adapter.DeviceFound += WriteDeviceFoundAsync;
        else
            adapter.DeviceFound += WriteDeviceNameFoundAsync;

        adapter.StartDiscoveryAsync().Wait();
        Task.Delay(TimeSpan.FromSeconds(scanSeconds)).Wait();
        adapter.StopDiscoveryAsync().Wait();

        if (useAlias)
            adapter.DeviceFound -= WriteDeviceFoundAsync;
        else
            adapter.DeviceFound -= WriteDeviceNameFoundAsync;

        Console.WriteLine($"Scan completed. {_preScanDeviceCount + _newScanDeviceCount} device(s) found.");
        Console.Out.Flush();
        return SUCCESS;

    } // end of method ListDevices()

    private static async Task WriteDeviceFoundAsync(Adapter adapter, DeviceFoundEventArgs e)
    {
        try
        {
            int totalCount;
            var device = e.Device;

            var deviceDescription = await GetDeviceDescriptionAsync(device);
            if (e.IsStateChange)
            {
                _newScanDeviceCount++;
                totalCount = _newScanDeviceCount + _preScanDeviceCount;
                Console.WriteLine($"[{totalCount,3}] - [NEW] {deviceDescription}");
                Console.Out.Flush();
            }
            else
            {
                _preScanDeviceCount++;
                totalCount = _newScanDeviceCount + _preScanDeviceCount;
                Console.WriteLine($"[{totalCount,3}] -       {deviceDescription}");
                Console.Out.Flush();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }

    } // end of Task WriteDeviceFoundAsync()

    private static async Task WriteDeviceNameFoundAsync(Adapter adapter, DeviceFoundEventArgs e)
    {
        try
        {
            int totalCount;
            var device = e.Device;

            var deviceDescription = await GetDeviceDescriptionNameAsync(device);
            if (e.IsStateChange)
            {
                _newScanDeviceCount++;
                totalCount = _newScanDeviceCount + _preScanDeviceCount;
                Console.WriteLine($"[{totalCount,3}] - [NEW] {deviceDescription}");
                Console.Out.Flush();
            }
            else
            {
                _preScanDeviceCount++;
                totalCount = _newScanDeviceCount + _preScanDeviceCount;
                Console.WriteLine($"[{totalCount,3}] -       {deviceDescription}");
                Console.Out.Flush();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }

    } // end of Task WriteDeviceNameFoundAsync()

    private static async Task<int> ListMACs(string cmdAdapterName, int scanSeconds)
    {
        int i = 1;
        int preScanCount;
        IAdapter1 adapter;

        FileInfo macFileInfo = new(_defaultMacFilename);

        FileStream macStream = macFileInfo.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);

        StreamWriter writer = new(macStream);

        if (cmdAdapterName != "none")
        {
            adapter = BlueZManager.GetAdapterAsync(cmdAdapterName).GetAwaiter().GetResult();
            if (adapter == null)
                return ERROR;
        }
        else
        {
            var adapters = BlueZManager.GetAdaptersAsync().GetAwaiter().GetResult();
            if (adapters.Count == 0)
            {
                Console.WriteLine("No adapters found!");
                Console.Out.Flush();
                return ERROR;
            }

            adapter = adapters[0];
        }

        var adapterPath = adapter.ObjectPath.ToString();
        var adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);
        Console.WriteLine($"Using Bluetooth adapter {adapterName}");

        // Ensure adapter is powered
        Adapter1Properties adapterProperties = adapter!.GetAllAsync().GetAwaiter().GetResult();

        if (adapterProperties.Powered == false)
        {
            Console.WriteLine($"Adapter {adapter!.ObjectPath} is not powered!");
            Console.WriteLine($"Powering up adapter");
            Console.Out.Flush();
            adapter.SetPoweredAsync(true).Wait();
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        Console.WriteLine($"Using default MAC address file[{_defaultMacFilename}]");

        // Scan for more devices.
        Console.WriteLine($"Scanning for {scanSeconds} seconds...");
        Console.Out.Flush();

        // Print out the devices we already know about.
        var preScanDevices = adapter.GetDevicesAsync().GetAwaiter().GetResult();

        preScanCount = preScanDevices.Count;

        foreach (var device in preScanDevices)
        {
            string deviceDescription = GetDeviceDescriptionAsync(device).GetAwaiter().GetResult();
            Console.WriteLine($"[{i++,3}] -       {deviceDescription}");
            Console.Out.Flush();

            string deviceAddress = device.GetAddressAsync().GetAwaiter().GetResult();
            writer.WriteLine($"{deviceAddress}");
            writer.Flush();
        }

        int newDevices = 0;
        using (await adapter.WatchDevicesAddedAsync(async device =>
        {
            newDevices++;

            // Write a message when we detect new devices during the scan.
            string deviceDescription = await GetDeviceDescriptionAsync(device);
            Console.WriteLine($"[{i++,3}] - [NEW] {deviceDescription}");
            Console.Out.Flush();

            string deviceAddress = await device.GetAddressAsync();
            writer.WriteLine($"{deviceAddress}");
            writer.Flush();
        }))
        {
            adapter.StartDiscoveryAsync().Wait();
            Task.Delay(TimeSpan.FromSeconds(scanSeconds)).Wait();
            adapter.StopDiscoveryAsync().Wait();
        }

        Console.WriteLine($"Scan completed. {preScanCount + newDevices} device(s) found.");
        Console.Out.Flush();

        return SUCCESS;

    } // end of method ListMACs()

    private static int MonitorDevice(string cmdAdapterName, string cmdDeviceName, StreamWriter writer, int numOfCycles, int cycleTime)
    {
        Adapter adapter;
        Device device;

        cmdDeviceName = cmdDeviceName.ToUpper();

        // If this is the first time through, connect to the given device
        if (_monitorDeviceFirstTime == true)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(GlobalTimeout);

            adapter = FindAdapter(cmdAdapterName);

            if (adapter == null)
            {
                Console.WriteLine($"MonitorDevice: Adapter[{cmdAdapterName}] not found!");
                Console.Out.Flush();
                writer.WriteLine($"Adapter [{cmdAdapterName}] not found!");
                writer.Flush();
                return ERROR;
            }

            var adapterPath = adapter!.ObjectPath.ToString();
            var adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);

            // Ensure adapter is powered
            Adapter1Properties adapterProperties1 = adapter!.GetAllAsync().GetAwaiter().GetResult();

            if (adapterProperties1.Powered == false)
            {
                adapter.SetPoweredAsync(true).Wait();
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            // Ensure adapter is discoverable
            if (adapterProperties1.Discoverable == false)
            {
                Console.WriteLine($"Setting adapter [{adapterName}] discoverable");
                Console.Out.Flush();
                adapter.SetDiscoverableAsync(true).Wait();
                adapter.SetDiscoverableTimeoutAsync(180).Wait();
            }

            // Ensure adapter is pairable
            if (adapterProperties1.Pairable == false)
            {
                Console.WriteLine($"Setting adapter [{adapterName}] pairable");
                Console.Out.Flush();
                adapter.SetPairableAsync(true).Wait();
                adapter.SetPairableTimeoutAsync(30).Wait();
            }

            try
            {
                device = adapter.GetDeviceAsync(cmdDeviceName).GetAwaiter().GetResult();
            }
            catch (Tmds.DBus.DBusException e)
            {
                Console.WriteLine($"MonitorDevice: Exception [{e.ErrorMessage}]");
                Console.Out.Flush();
                writer.WriteLine($"MonitorDevice: Exception [{e.ErrorMessage}]");
                writer.Flush();
                return ERROR;
            }

            if (device == null)
            {
                writer.WriteLine("Scanning...");
                writer.Flush();
                adapter.StartDiscoveryAsync().Wait();
                Task.Delay(timeout).Wait();
                adapter.StopDiscoveryAsync().Wait();
                writer.WriteLine("Scan completed");
                writer.Flush();
                try
                {
                    device = adapter.GetDeviceAsync(cmdDeviceName).GetAwaiter().GetResult();
                }
                catch (Tmds.DBus.DBusException e)
                {
                    Console.WriteLine($"MonitorDevice: Exception [{e.ErrorMessage}]");
                    Console.Out.Flush();
                    writer.WriteLine($"MonitorDevice: Exception [{e.ErrorMessage}]");
                    writer.Flush();
                    return ERROR;
                }
            }

            if (device == null)
            {
                Console.WriteLine($"MonitorDevice: Device {cmdDeviceName} not found!");
                Console.Out.Flush();
                writer.WriteLine($"MonitorDevice: Device {cmdDeviceName} not found!");
                writer.Flush();
                return ERROR;
            }

            DeviceProperties deviceProperties = device.GetPropertiesAsync().GetAwaiter().GetResult();

            // Ensure adapter is discoverable
            if (adapterProperties1.Discoverable == false)
            {
                adapter.SetDiscoverableAsync(true).Wait();
                adapter.SetDiscoverableTimeoutAsync(180).Wait();
            }

            // Ensure adapter is pairable
            if (adapterProperties1.Pairable == false)
            {
                adapter.SetPairableAsync(true).Wait();
                adapter.SetPairableTimeoutAsync(30).Wait();
            }

            device.SetTrustedAsync(true).Wait();

            try
            {
                device.ConnectAsync().Wait();
            }
            catch (Tmds.DBus.DBusException e)
            {
                if (e.ErrorMessage == "Host is down")
                {
                    Console.WriteLine($"MonitorDevice: Monitored device [{deviceProperties.Address}][{deviceProperties.Alias}] is not powered");
                    Console.Out.Flush();
                    writer.WriteLine($"MonitorDevice: Monitored device [{deviceProperties.Address}][{deviceProperties.Alias}] is not powered");
                    writer.Flush();
                }
                else
                {
                    Console.WriteLine($"MonitorDevice: Exception [{e.ErrorMessage}]");
                    Console.Out.Flush();
                    writer.WriteLine($"MonitorDevice: Exception [{e.ErrorMessage}]");
                    writer.Flush();
                }
                return ERROR;
            }

            device.WaitForPropertyValueAsync("Connected", value: true, timeout).Wait();
            writer.WriteLine($"\rConnected to [{deviceProperties.Address}][{deviceProperties.Alias}]");
            writer.Flush();

            bool paired = device.GetPairedAsync().GetAwaiter().GetResult();

            if (paired == false)
            {
                writer.Write("Pairing with device [{deviceProperties.Address}][{deviceProperties.Alias}]");
                device.PairAsync().Wait();
                device.WaitForPropertyValueAsync("Paired", value: true, timeout).Wait();
                writer.WriteLine("Pairing completed");
                writer.Flush();
            }

            _monitorDeviceAdapter = adapter;
            _monitorDeviceDevice = device;

            _monitorDeviceFirstTime = false;

        } // end of if ( _monitorDeviceFirstTime == true )

        if (numOfCycles <= 0)
        {
            if (_monitorDeviceDevice == null)
            {
                Console.WriteLine($"MonitorDevice: Device {cmdDeviceName} not found!");
                Console.Out.Flush();
                writer.WriteLine($"MonitorDevice: Device {cmdDeviceName} not found!");
                writer.Flush();
                return ERROR;
            }

            device = _monitorDeviceDevice;

            device.SetTrustedAsync(true).Wait();

            bool connected = device.GetConnectedAsync().GetAwaiter().GetResult();

            if (connected)
                writer.WriteLine("Connected");
            else
                writer.WriteLine("Disconnected");

            writer.Flush();

            // Wait for the given cycle time
            Thread.Sleep(TimeSpan.FromSeconds(cycleTime));
        }
        else
        {
            for (int i = 0; i < numOfCycles; i++)
            {
                if (_monitorDeviceDevice == null)
                {
                    Console.WriteLine($"MonitorDevice: Device {cmdDeviceName} not found!");
                    Console.Out.Flush();
                    writer.WriteLine($"MonitorDevice: Device {cmdDeviceName} not found!");
                    writer.Flush();
                    return ERROR;
                }

                device = _monitorDeviceDevice;

                device.SetTrustedAsync(true).Wait();

                bool connected = device.GetConnectedAsync().GetAwaiter().GetResult();

                if (connected)
                    writer.WriteLine("Connected");
                else
                    writer.WriteLine("Disconnected");

                writer.Flush();

                // Wait for the given cycle time
                Thread.Sleep(TimeSpan.FromSeconds(cycleTime));
            }
        }

        return SUCCESS;
    } // end of method MonitorDevice()

    private static int ObserveFile(string cmdFilename, string resFilename)
    {
        bool done = false;

        string[] tokens = Array.Empty<string>();
        string primaryCommand = null;

        FileInfo cmdFileInfo = new(cmdFilename);
        FileInfo resFileInfo = new(resFilename);

        FileStream cmdStream = cmdFileInfo.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        FileStream resStream = resFileInfo.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);

        StreamReader cmdReader = new(cmdStream);
        StreamWriter resWriter = new(resStream);

        _monitorDeviceFirstTime = true;

        while (!done)
        {
            string readCommand = cmdReader.ReadLine();

            if ((readCommand != null) && (readCommand.Length != 0))
            {
                // remove return and linefeed
                readCommand = readCommand.Trim('\r');
                readCommand = readCommand.Trim('\n');

                string command = readCommand.ToLower();

                tokens = command.Split(new char[] { ' ' }).ToArray();
                primaryCommand = tokens.First();

                if (tokens.Length > 1)
                    tokens = tokens.Skip(1).ToArray();
            }
            else if (primaryCommand == null)
            {
                // Check for a new command every second
                Thread.Sleep(TimeSpan.FromSeconds(1));

                continue;
            }

            switch (primaryCommand)
            {
                case "?":
                case "help":
                    PrintHelp(tokens!);
                    break;
                case "exit":
                case "q":
                case "quit":
                    done = true;
                    cmdReader.Close();
                    resWriter.Close();
                    Console.Out.Flush();
                    QuitCmd();
                    break;
                case "disconnect":
                    DisConnectCmd(tokens!);
                    break;
                case "connect":
                    ConnectCmd(tokens!);
                    break;
                case "list":
                    ListCmd(tokens!);
                    break;
                case "monitor":
                    MonitorCmd(tokens!, resWriter, 0);
                    break;
                case "remove":
                    RemoveCmd(tokens!);
                    break;
                case "set":
                    SetCmd(tokens!);
                    break;
                case "show":
                    ShowCmd(tokens!);
                    break;
                default:
                    resWriter.WriteLine("Unknown command [" + primaryCommand + "]");
                    break;
            } // end of switch (primaryCommand)

            if (primaryCommand != "monitor")
            {
                primaryCommand = null;
                _monitorDeviceFirstTime = true;
            }
        } // end of while (!done)

        Console.Out.Flush();

        return SUCCESS;

    } // end of method ObserveFile()

    private static int PairDevice(string cmdAdapterName, string cmdDeviceName)
    {
        cmdDeviceName = cmdDeviceName.ToUpper();

        TimeSpan timeout = TimeSpan.FromSeconds(GlobalTimeout);

        Adapter adapter = FindAdapter(cmdAdapterName);

        if (adapter != null)
        {
            var adapterPath = adapter!.ObjectPath.ToString();
            var adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);

            // Ensure adapter is powered
            Adapter1Properties adapterProperties1 = adapter!.GetAllAsync().GetAwaiter().GetResult();

            if (adapterProperties1.Powered == false)
            {
                Console.WriteLine($"Powering up adapter [{adapterName}]");
                adapter.SetPoweredAsync(true).Wait();
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            Device device = adapter.GetDeviceAsync(cmdDeviceName).GetAwaiter().GetResult();

            if (device == null)
            {
                Console.WriteLine($"Could not find device {cmdDeviceName}");
                Console.WriteLine("Scanning...");
                adapter.StartDiscoveryAsync().Wait();
                Task.Delay(timeout).Wait();
                adapter.StopDiscoveryAsync().Wait();
                Console.WriteLine("Scan completed");

                device = adapter.GetDeviceAsync(cmdDeviceName).GetAwaiter().GetResult();

                if (device == null)
                {
                    Console.WriteLine($"Device {cmdDeviceName} not found!");
                    Console.Out.Flush();
                    return ERROR;
                } // end of if (device == null)

            } // end of if (device == null)

            DeviceProperties deviceProperties = device.GetPropertiesAsync().GetAwaiter().GetResult();

            // Ensure adapter is discoverable
            if (adapterProperties1.Discoverable == false)
            {
                Console.WriteLine($"Setting adapter [{adapterName}] discoverable");
                adapter.SetDiscoverableAsync(true).Wait();
                adapter.SetDiscoverableTimeoutAsync(180).Wait();
            }

            // Ensure adapter is pairable
            if (adapterProperties1.Pairable == false)
            {
                Console.WriteLine($"Setting adapter [{adapterName}] pairable");
                adapter.SetPairableAsync(true).Wait();
                adapter.SetPairableTimeoutAsync(30).Wait();
            }

            device.SetTrustedAsync(true).Wait();

            bool paired = device.GetPairedAsync().GetAwaiter().GetResult();

            if (paired == false)
            {
                Console.WriteLine($"Pairing with device [{deviceProperties.Address}][{deviceProperties.Alias}]");
                device.PairAsync().Wait();
                device.WaitForPropertyValueAsync("Paired", value: true, timeout).Wait();
                Console.WriteLine("Pairing completed");
            }
            else
            {
                Console.WriteLine($"Device [{deviceProperties.Address}][{deviceProperties.Alias}] already paired");
            }
        }
        else
        {
            return ERROR;
        } // end of if (adapter != null)

        Console.Out.Flush();
        return SUCCESS;
    } // end of method PairDevice()

    private static int ProcessCommmandLine(string[] args)
    {
        if ((args != null) && (args.Length != 0))
        {
            for (int i = 0; i < args.Length; i++)
                args[i] = args[i].ToLower();

            string[] tokens = null;
            string primaryCommand = args.First();

            if (args.Length > 1)
            {
                tokens = args.Skip(1).ToArray();
                foreach (string token in tokens)
                    token.ToLower();
            }

            switch (primaryCommand)
            {
                case "?":
                case "help":
                    PrintHelp(tokens!);
                    break;
                case "q":
                case "quit":
                case "exit":
                    QuitCmd();
                    break;
                case "connect":
                    ConnectCmd(tokens!);
                    break;
                case "disconnect":
                    DisConnectCmd(tokens!);
                    break;
                case "list":
                    ListCmd(tokens!);
                    break;
                case "monitor":
                    MonitorCmd(tokens!, new StreamWriter(Console.OpenStandardOutput()), 30);
                    break;
                case "observe":
                    ObserveCmd(tokens!);
                    break;
                case "pair":
                    PairCmd(tokens!);
                    break;
                case "remove":
                    RemoveCmd(tokens!);
                    break;
                case "set":
                    SetCmd(tokens!);
                    break;
                case "show":
                    ShowCmd(tokens!);
                    break;
                default:
                    Console.WriteLine("Unknown command [" + primaryCommand + "]");
                    break;
            } // end of switch (primaryCommand)
        } // end of if ((args != null) && (args.Length != 0))

        return 0;
    } // end of Task processCommandLine()

    private static int RemoveDevice(string cmdAdapterName, string cmdDeviceName)
    {
        string adapterPath;
        string adapterName;

        Adapter adapter = FindAdapter(cmdAdapterName);

        if (adapter != null)
        {
            adapterPath = adapter!.ObjectPath.ToString();
            adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);
        }
        else
        {
            if (cmdAdapterName == "none")
                Console.WriteLine("No adapters found");
            else
                Console.WriteLine($"Could not find adapter [{cmdAdapterName}]");
            return ERROR;
        } // end of if (adapter != null)

        IList<string> devicePaths = new List<string>();

        if (cmdDeviceName == "all")
        {
            // Get the devices the adapter knows about (have been paired).
            var preScanDevices = adapter.GetDevicesAsync().GetAwaiter().GetResult();

            if (preScanDevices.Count == 0)
            {
                Console.WriteLine($"There are no devices to be removed");
                Console.Out.Flush();
            }

            foreach (Device device in preScanDevices)
            {
                string address = device.GetAddressAsync().GetAwaiter().GetResult();
                string path = adapterPath + "/dev_" + address.Replace(':', '_');
                devicePaths.Add(path);
            }
        }
        else
        {
            // Build the device path
            string path = adapterPath + "/dev_" + cmdDeviceName.Replace(':', '_');
            devicePaths.Add(path);
        }

        // Ensure adapter is powered
        Adapter1Properties adapterProperties1 = adapter!.GetAllAsync().GetAwaiter().GetResult();

        if (adapterProperties1.Powered == false)
        {
            Console.WriteLine($"Powering up adapter [{adapterName}]");
            adapter.SetPoweredAsync(true).Wait();
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        foreach (string devicePath in devicePaths)
        {
            try
            {
                adapter.RemoveDeviceAsync(devicePath).Wait();
            }
            catch (Tmds.DBus.DBusException e)
            {
                if (e.ErrorMessage == "Does Not Exist")
                {
                    Console.WriteLine($"device [{devicePath}] is not known to adapter [{adapterName}]");
                    Console.Out.Flush();
                }
                else
                {
                    Console.WriteLine($"Exception [{e.ErrorMessage}]");
                    Console.Out.Flush();
                }
                return ERROR;
            }
            Console.WriteLine($"Removed device [{devicePath}]");
        }

        Console.Out.Flush();

        return SUCCESS;
    } // end of method RemoveDevice()

    private static int SetAdapter(string cmdAdapterName, string cmdProperty, string cmdValue)
    {
        bool cmdBoolValue = false;

        Adapter adapter = FindAdapter(cmdAdapterName);

        if (adapter != null)
        {
            // Ensure adapter is powered
            Adapter1Properties adapterProperties1 = adapter!.GetAllAsync().GetAwaiter().GetResult();

            if ((adapterProperties1.Powered == false) && (cmdProperty != "powered"))
            {
                Console.WriteLine($"Adapter {adapter!.ObjectPath} is not powered!");
                Console.WriteLine($"Powering up adapter");
                Console.Out.Flush();
                adapter.SetPoweredAsync(true).Wait();
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            if (IsAdapterPropertyBoolean(cmdProperty))
            {
                if (cmdValue is "on" or
                     "yes" or
                     "1" or
                     "enable" or
                     "enabled")
                    cmdBoolValue = true;
                else
                    cmdBoolValue = false;
            }

            switch (cmdProperty)
            {
                case "alias":
                    adapter.SetAliasAsync(cmdValue).Wait();
                    break;

                case "discoverable":
                    adapter.SetDiscoverableAsync(cmdBoolValue).Wait();
                    break;

                case "discoverabletimeout":
                    if (uint.TryParse(cmdValue, out uint dt_value))
                        adapter.SetDiscoverableTimeoutAsync(dt_value).Wait();
                    else
                        Console.WriteLine($"Error: Bad discoverable timeout value [{cmdValue}]");
                    break;

                case "pairable":
                    adapter.SetPairableAsync(cmdBoolValue).Wait();
                    break;

                case "pairabletimeout":
                    if (uint.TryParse(cmdValue, out uint pt_value))
                        adapter.SetPairableTimeoutAsync(pt_value).Wait();
                    else
                        Console.WriteLine($"Error: Bad pairable timeout value [{cmdValue}]");
                    break;

                case "powered":
                    adapter.SetPoweredAsync(cmdBoolValue).Wait();
                    break;

                default:
                    Console.WriteLine($"Error: Unknown property [{cmdProperty}] for adapter {adapter!.ObjectPath}");
                    return ERROR;
            }

            Console.WriteLine($"Adapter - {adapter!.ObjectPath} property {cmdProperty} set to {cmdValue}");
        }

        Console.Out.Flush();

        return SUCCESS;
    } // end of method SetAdapter()

    private static int ShowAdapter(string cmdAdapterName)
    {
        Adapter adapter = FindAdapter(cmdAdapterName);

        if (adapter != null)
        {
            Adapter1Properties adapterProperties = adapter!.GetAllAsync().GetAwaiter().GetResult();

            if (adapterProperties != null)
            {
                DecodeCoD(adapterProperties!.Class, out string serviceClass, out string majorClass, out string minorClass);

                Console.WriteLine($"Adapter - {adapter!.ObjectPath}");

                Console.WriteLine($"\tName        : {adapterProperties!.Name}");
                Console.WriteLine($"\tAlias       : {adapterProperties!.Alias}");
                Console.WriteLine($"\tClass        : 0x{adapterProperties!.Class,0:X6}");
                Console.WriteLine($"\tService Class: {serviceClass}");
                Console.WriteLine($"\tMajor Class  : {majorClass}");
                Console.WriteLine($"\tMinor Class  : {minorClass}");
                Console.WriteLine($"\tAddress      : {adapterProperties!.Address}");
                Console.WriteLine($"\tType         : {adapterProperties.AddressType}");
                Console.WriteLine($"\tDiscoverable : {adapterProperties!.Discoverable,-5} (Timeout={adapterProperties.DiscoverableTimeout} msec)");
                Console.WriteLine($"\tPairable     : {adapterProperties!.Pairable,-5} (Timeout={adapterProperties.PairableTimeout} msec)");
                Console.WriteLine($"\tDiscovering : {adapterProperties!.Discovering}");
                Console.WriteLine($"\tPowered     : {adapterProperties!.Powered}");

                Console.WriteLine($"\tUUIDs        : ");

                if (adapterProperties!.UUIDs.Length > 0)
                {
                    foreach (string uuid in adapterProperties!.UUIDs)
                    {
                        string uuid_type = GetUUIDdescription(uuid);

                        if (string.IsNullOrEmpty(uuid_type))
                            Console.WriteLine($"\t              {uuid}");
                        else
                            Console.WriteLine($"\t               {uuid}  [{uuid_type}]");
                    } // end of foreach (string uuid in adapterProperties!.UUIDs)

                } // end of if (adapterProperties!.UUIDs.Length > 0)
                else
                {
                    Console.WriteLine($"\t               None");
                }
            }
            else
            {
                Console.WriteLine($"Adapter {cmdAdapterName} not found!");
            } // end of if ( adapterProperties != null )
        }
        else
        {
            Console.WriteLine($"Adapter {cmdAdapterName} not found!");
        } // end of if ( adapter != null )

        Console.Out.Flush();

        return SUCCESS;
    } // end of method ShowAdapter()

    private static int ShowDevice(string cmdAdapterName, string cmdDeviceName)
    {
        TimeSpan timeout = TimeSpan.FromSeconds(GlobalTimeout);

        Adapter adapter = FindAdapter(cmdAdapterName);

        if (adapter == null)
            return ERROR;

        var adapterPath = adapter!.ObjectPath.ToString();
        var adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);

        // Ensure adapter is powered
        Adapter1Properties adapterProperties1 = adapter!.GetAllAsync().GetAwaiter().GetResult();

        if (adapterProperties1.Powered == false)
        {
            Console.WriteLine($"Powering up adapter [{adapterName}]");
            adapter.SetPoweredAsync(true).Wait();
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        if (cmdDeviceName == "all")
        {
            Console.WriteLine("Scanning...");
            Console.Out.Flush();
            adapter.StartDiscoveryAsync().Wait();
            Task.Delay(timeout).Wait();
            adapter.StopDiscoveryAsync().Wait();

            IReadOnlyList<Device> devices = adapter.GetDevicesAsync().GetAwaiter().GetResult();

            Console.WriteLine($"Scan completed. {devices.Count} device(s) found");
            Console.Out.Flush();

            if ((devices != null) && (devices.Count > 0))
            {
                foreach (var device in devices)
                {
                    ShowDeviceOutput(device);
                }
            }
            else
            {
                Console.WriteLine($"No devices found!");
                Console.Out.Flush();
            }
        }
        else
        {
            cmdDeviceName = cmdDeviceName.ToUpper();

            Device device = adapter.GetDeviceAsync(cmdDeviceName).GetAwaiter().GetResult();

            if (device == null)
            {
                Console.WriteLine($"Could not find device {cmdDeviceName}");
                Console.WriteLine("Scanning...");
                Console.Out.Flush();
                adapter.StartDiscoveryAsync().Wait();
                Task.Delay(timeout).Wait();
                adapter.StopDiscoveryAsync().Wait();
                Console.WriteLine("Scan completed");
                Console.Out.Flush();
            }

            device = adapter.GetDeviceAsync(cmdDeviceName).GetAwaiter().GetResult();

            if (device == null)
            {
                Console.WriteLine($"Device {cmdDeviceName} not found!");
                Console.Out.Flush();
                return ERROR;
            }

            ShowDeviceOutput(device);

        } // end of if ( cmdDeviceName == "all")

        return SUCCESS;

    } // end of method ShowDevice()

    private static void ShowDeviceOutput(Device device)
    {
        DeviceProperties deviceProperties = device.GetPropertiesAsync().GetAwaiter().GetResult();

        BTAppearance appearance = new(deviceProperties!.Appearance);

        DecodeCoD(deviceProperties!.Class, out string serviceClass, out string majorClass, out string minorClass);

        Console.WriteLine($"Device - {device!.ObjectPath}");

        Console.WriteLine($"\tName            : {deviceProperties!.Name}");
        Console.WriteLine($"\tAlias           : {deviceProperties!.Alias}");
        Console.WriteLine($"\tClass           : 0x{deviceProperties!.Class,0:X6}");
        Console.WriteLine($"\tService Class   : {serviceClass}");
        Console.WriteLine($"\tMajor Class     : {majorClass}");
        Console.WriteLine($"\tMinor Class     : {minorClass}");
        Console.WriteLine($"\tAddress         : {deviceProperties!.Address}");
        Console.WriteLine($"\tAddressType     : {deviceProperties!.AddressType}");
        Console.WriteLine($"\tAppearance      : 0x{deviceProperties!.Appearance,0:X4}");
        Console.WriteLine($"\tCatagory        : {appearance.Catagory}");
        Console.WriteLine($"\tSubcatagory     : {appearance.Subcatagory}");
        Console.WriteLine($"\tIcon            : {deviceProperties!.Icon}");
        Console.WriteLine($"\tModalias        : {deviceProperties!.Modalias}");
        Console.WriteLine($"\tBlocked         : {deviceProperties!.Blocked}");
        Console.WriteLine($"\tTrusted         : {deviceProperties!.Trusted}");
        Console.WriteLine($"\tConnected       : {deviceProperties!.IsConnected}");
        Console.WriteLine($"\tPaired          : {deviceProperties!.Paired}");
        Console.WriteLine($"\tLegacyPairing   : {deviceProperties!.LegacyPairing}");
        Console.WriteLine($"\tRSSI            : {deviceProperties!.Rssi}");
        Console.WriteLine($"\tTxPower         : {deviceProperties!.TxPower}");
        Console.WriteLine($"\tServicesResolved: {deviceProperties!.ServicesResolved}");

        Console.WriteLine("\tUUIDs:");

        var uuids = deviceProperties!.UUIDs;

        if ((uuids == null) || (uuids.Length == 0))
        {
            Console.WriteLine("\t    None");
        }
        else
        {
            foreach (string uuid in uuids)
            {
                string uuid_type = GetUUIDdescription(uuid);

                if (string.IsNullOrEmpty(uuid_type))
                    Console.WriteLine($"\t              {uuid}");
                else
                    Console.WriteLine($"\t              {uuid}  [{uuid_type}]");

            } // end of foreach (string uuid in uuids)
        } // end of if ((uuids == null) || (uuids.Length == 0) )

        Console.Out.Flush();

    } // end of method ShowDeviceOutput()

    //********************************
    //      Utility Functions
    //********************************

    private static void DecodeCoD(uint codValue, out string serviceClass, out string majorClass, out string minorClass)
    {
        serviceClass = string.Empty;
        majorClass = string.Empty;
        minorClass = string.Empty;

        if ((codValue & 0x00000003) == 0)  // Format type 00
        {
            uint serviceValue = ((codValue & 0x00FFE000) >> 13);
            uint majorValue = ((codValue & 0x00001F00) >> 8);
            uint minorValue = ((codValue & 0x000000FC) >> 2);

            // Interpret the Major Service Class
            if ((serviceValue & 0x0001) == 0x0001)
                serviceClass += "Limited Discoverable Mode ";

            if ((serviceValue & 0x0002) == 0x0002)
                serviceClass += "reserved ";

            if ((serviceValue & 0x0004) == 0x0004)
                serviceClass += "reserved ";

            if ((serviceValue & 0x0008) == 0x0008)
                serviceClass += "Positioning ";

            if ((serviceValue & 0x0010) == 0x0010)
                serviceClass += "Networking ";

            if ((serviceValue & 0x0020) == 0x0020)
                serviceClass += "Rendering ";

            if ((serviceValue & 0x0040) == 0x0040)
                serviceClass += "Capturing ";

            if ((serviceValue & 0x0080) == 0x0080)
                serviceClass += "ObjectTransfer ";

            if ((serviceValue & 0x0100) == 0x0100)
                serviceClass += "Audio ";

            if ((serviceValue & 0x0200) == 0x0200)
                serviceClass += "Telephony ";

            if ((serviceValue & 0x0400) == 0x0400)
                serviceClass += "Information ";

            // Interpret the Major Device and Minor Device Classes
            switch (majorValue)
            {
                case 0:
                    majorClass = "Miscellaneous";
                    break;

                case 1:
                    majorClass = "Computer";

                    minorClass = minorValue switch
                    {
                        0 => "Uncategorized",
                        1 => "Desktop workstation",
                        2 => "Server-class computer",
                        3 => "Laptop",
                        4 => "Handheld PC/PDA (clam shell)",
                        5 => "Palm sized PC/PDA",
                        6 => "Wearable computer (Watch sized)",
                        _ => "Reserved",
                    };
                    break;

                case 2:
                    majorClass = "Phone";

                    minorClass = minorValue switch
                    {
                        0 => "Uncategorized",
                        1 => "Cellular",
                        2 => "Cordless",
                        3 => "Smart phone",
                        4 => "Wired modem or voice gateway",
                        5 => "Common ISDN access",
                        _ => "Reserved",
                    };
                    break;

                case 3:
                    majorClass = "LAN/Network Access Point";

                    minorClass = minorValue switch
                    {
                        0 => "Fully available",
                        8 => "1-17% utilized",
                        16 => "17-33% utilized",
                        24 => "33-50% utilized",
                        32 => "50-67% utilized",
                        40 => "67-83% utilized",
                        48 => "83-99% utilized",
                        56 => "No service available",
                        _ => "Uncategorized",
                    };
                    break;

                case 4:
                    majorClass = "Audio/Video";

                    minorClass = minorValue switch
                    {
                        0 => "Uncategorized",
                        1 => "Wearable Headset Device",
                        2 => "Hands-free Device",
                        3 => "Reserved",
                        4 => "Microphone",
                        5 => "Loudspeaker",
                        6 => "Headphones",
                        7 => "Portable Audio",
                        8 => "Car Audio",
                        9 => "Set-top box",
                        10 => "HiFi Audio Device",
                        11 => "VCR",
                        12 => "Video Camera",
                        13 => "Camcorder",
                        14 => "Video Monitor",
                        15 => "Video Display and Loudspeaker",
                        16 => "Video Conferencing",
                        17 => "Reserved",
                        18 => "Gaming/Toy",
                        _ => "Reserved",
                    };
                    break;

                case 5:
                    majorClass = "Peripheral";

                    switch (minorValue >> 4)
                    {
                        case 1:
                            minorClass = "Keyboard";
                            break;

                        case 2:
                            minorClass = "Pointing Device";
                            break;

                        case 3:
                            minorClass = "Combo keyboard/pointing device";
                            break;

                    } // end of switch(minorValue)

                    if ((minorClass.Length != 0) && ((minorValue & 0xF) != 0))
                        minorClass += " + ";

                    switch ((minorValue & 0xF))
                    {
                        case 0:
                            minorClass = "Uncategorized";
                            break;

                        case 1:
                            minorClass += "Joystick";
                            break;

                        case 2:
                            minorClass += "Gamepad";
                            break;

                        case 3:
                            minorClass += "Remote control";
                            break;

                        case 4:
                            minorClass += "Sensing device";
                            break;

                        case 5:
                            minorClass += "Digitizer tablet";
                            break;

                        case 6:
                            minorClass += "Card reader";
                            break;

                        default:
                            minorClass = "Reserved";
                            break;

                    } // end of switch(minorValue)
                    break;

                case 6:
                    majorClass = "Imaging";

                    if (minorValue == 0)                // Minor classes can be combined (e.g., camera+scanner)
                        minorClass = "Uncategorized";
                    if ((minorValue & 0x0004) == 0x0004)
                        minorClass += "Display ";
                    if ((minorValue & 0x0008) == 0x0008)
                        minorClass += "Camera ";
                    if ((minorValue & 0x0010) == 0x0010)
                        minorClass += "Scanner ";
                    if ((minorValue & 0x0020) == 0x0020)
                        minorClass += "Printer ";

                    if (minorClass.Length == 0)
                        minorClass = "Reserved";
                    break;

                case 7:
                    majorClass = "Wearable";

                    minorClass = minorValue switch
                    {
                        1 => "Wrist Watch",
                        2 => "Pager",
                        3 => "Jacket",
                        4 => "Helmet",
                        5 => "Glasses",
                        _ => "Reserved",
                    };
                    break;

                case 8:
                    majorClass = "Toy";

                    minorClass = minorValue switch
                    {
                        1 => "Robot",
                        2 => "Vehicle",
                        3 => "Doll/Action Figure",
                        4 => "Controller",
                        5 => "Game",
                        _ => "Reserved",
                    };
                    break;

                case 9:
                    majorClass = "Health";

                    minorClass = minorValue switch
                    {
                        0 => "Undefined",
                        1 => "Blood Pressure Monitor",
                        2 => "Thermometer",
                        3 => "Weighing Scale",
                        4 => "Glucose Meter",
                        5 => "Pulse Oximeter",
                        6 => "Heart/Pulse Rate Montior",
                        7 => "Health Data Display",
                        _ => "Reserved",
                    };
                    break;

                default:
                    majorClass = "Uncategorized";
                    break;
            } // end of switch(majorValue)

        } // end of if ( (codValue & 0x00000003) == 0)
    } // end of method DecodeCoD()

    private static Adapter FindAdapter(string cmdAdapterName)
    {
        bool found = false;

        Adapter1Properties adapterProperties;
        Adapter adapter1 = null;

        IReadOnlyList<Adapter> adapters = BlueZManager.GetAdaptersAsync().GetAwaiter().GetResult();

        if (adapters.Count == 0)
        {
            Console.WriteLine("FindAdapter: No adapters found!");
            Console.Out.Flush();
            return null;
        }

        if (cmdAdapterName == "none")
        {
            found = true;
            adapter1 = adapters[0];
        }
        else
        {
            foreach (Adapter Xadapter in adapters)
            {

                var adapterPath = Xadapter.ObjectPath.ToString();
                var adapterName = adapterPath.Substring(adapterPath.LastIndexOf("/") + 1);

                adapterProperties = Xadapter.GetAllAsync().GetAwaiter().GetResult();

                if ((adapterPath == cmdAdapterName) || (adapterName == cmdAdapterName) ||
                        (adapterProperties.Name == cmdAdapterName) || (adapterProperties.Alias == cmdAdapterName))
                {
                    found = true;
                    adapter1 = Xadapter;
                    break;
                } // end of if ( (adapterPath == cmdAdapterName) ...
            } // end of foreach (Adapter adapter in adapters)
        } // end of if ( cmdAdapterName == "none" )

        if ((!found) || (adapter1 == null))
        {
            Console.WriteLine($"Adapter {cmdAdapterName} not found!");
            Console.Out.Flush();
            return null;
        }

        return adapter1;
    } // end of method FindAdapter()

    private static async Task<string> GetDeviceDescriptionAsync(IDevice1 device)
    {
        var deviceProperties = await device.GetAllAsync();
        return $"{deviceProperties.Address} RSSI:{deviceProperties.RSSI,4} [{deviceProperties.Alias}]";
    }

    private static async Task<string> GetDeviceDescriptionNameAsync(IDevice1 device)
    {
        var deviceProperties = await device.GetAllAsync();
        return $"{deviceProperties.Address} RSSI:{deviceProperties.RSSI,4} [{deviceProperties.Name}]";
    }

    static string GetUUIDdescription(string uuid)
    {
        string uuid_type = null;

        string[] tokens = uuid.Split(new char[] { '-' }).ToArray();

        if (tokens.Length == 5)
        {
            UInt64 uuid_field0 = Convert.ToUInt64(tokens[0], 16);
            UInt32 uuid_field1 = Convert.ToUInt32(tokens[1], 16);
            UInt32 uuid_field2 = Convert.ToUInt32(tokens[2], 16);
            UInt32 uuid_field3 = Convert.ToUInt32(tokens[3], 16);
            UInt64 uuid_field4 = Convert.ToUInt64(tokens[4], 16);

            // BT SIG’s base UUID 00000000-0000-1000-8000-00805F9B34FB
            if ((uuid_field4 == 0x805F9B34FB) &&
                 (uuid_field3 == 0x8000) &&
                 (uuid_field2 == 0x1000) &&
                 (uuid_field1 == 0) &&
                 (uuid_field0 < 0xFFFF))
            {
                if (uuid_field0 < 0x1000)
                {
                    // Protocol Identifier
                    BTProtocolID protocol = new((ushort)uuid_field0);
                    uuid_type = protocol.Description;

                }
                else if (uuid_field0 < 0x1800)
                {
                    // Service Classes and Profiles
                    BTSrvClassProfile profile = new((ushort)uuid_field0);
                    uuid_type = profile.Description;
                }
                else if (uuid_field0 < 0x2700)
                {
                    // Service Classes and Profiles
                    BTGATTService profile = new((ushort)uuid_field0);
                    uuid_type = profile.Description;
                }
                else
                {
                    // Not implemented
                }
            } // if ( (uuid_field4 == 551359821051) && ...
        } // end of if ( tokens.Length == 5 )

        return uuid_type;
    } // end of method getUUIDdesription()

    private static bool IsAdapterProperty(string value)
    {
        return value switch
        {
            "name" or "alias" or "discoverable" or "discoverabletimeout" or "pairable" or "pairabletimeout" or "powered" => true,
            _ => false,
        };
    } // end of function IsAdapterProperty()

    private static bool IsAdapterPropertyBoolean(string value)
    {
        return value switch
        {
            "discoverable" or "pairable" or "discovering" or "powered" => true,
            _ => false,
        };
    } // end of function IsAdapterPropertyBoolean()

    public class BTAppearance
    {
        public BTAppearance() { }

        public BTAppearance(ushort appearanceValue)
        {

            const ushort CATAGORY_MASK = 0xFFC0;

            Value = appearanceValue;

            _btAppearanceCatagoryDict.TryGetValue((ushort)(appearanceValue & CATAGORY_MASK), out string catagory);

            if (catagory != null)
                Catagory = catagory;
            else
                Catagory = "Undefined";

            _btAppearanceSubcatagoryDict.TryGetValue(appearanceValue, out string subcatagory);

            if (catagory != null)
                Subcatagory = subcatagory;
            else
                Subcatagory = "Undefined";

        } // end of initializer

        public ushort Value { get; set; }
        public string Catagory { get; }
        public string Subcatagory { get; }

        private static readonly IDictionary<ushort, string> _btAppearanceCatagoryDict = new Dictionary<ushort, string>()
        {
            // 0x0000 to 0x003F - Unknown
            [0x0000] = "Unknown",

            // 0x0040 to 0x007F - Phone
            [0x0040] = "Phone",

            // 0x0080 to 0x00BF Computer
            [0x0080] = "Computer",

            // 0x00C0 to 0x00FF Watch
            [0x00C0] = "Watch",

            // 0x0100 to 0x013F Clock
            [0x0100] = "Clock",

            // 0x0140 to 0x017F Display
            [0x0140] = "Display",

            // 0x0180 to 0x01BF Remote Control
            [0x0180] = "Remote Control",

            // 0x01C0 to 0x01FF Eye - glasses
            [0x01C0] = "Eye-glasses",

            // 0x0200 to 0x023F Tag
            [0x0200] = "Tag",

            // 0x0240 to 0x027F Keyring
            [0x0240] = "Keyring",

            // 0x0280 to 0x02BF Media Player
            [0x0280] = "Media Player",

            // 0x02C0 to 0x02FF Barcode Scanner
            [0x02C0] = "Barcode Scanner",

            // 0x0300 to 0x033F Thermometer
            [0x0300] = "Thermometer",

            // 0x0340 to 0x037F Heart Rate Sensor
            [0x0340] = "Heart Rate Sensor",

            // 0x0380 to 0x03BF Blood Pressure
            [0x0380] = "Blood Pressure",

            // 0x03C0 to 0x03FF Human Interface Device
            [0x03C0] = "Human Interface Device",

            // 0x0400 to 0x043F Glucose Meter
            [0x0400] = "Glucose Meter",

            // 0x0440 to 0x047F Running Walking Sensor
            [0x0440] = "Running Walking Sensor",

            // 0x0480 to 0x04BF Cycling
            [0x0480] = "Cycling",

            // 0x04C0 to 0x04FF Control Device
            [0x04C0] = "Control Device",

            // 0x0500 to 0x053F Network Device
            [0x0500] = "Network Device",

            // 0x0540 to 0x057F Sensor
            [0x0540] = "Sensor",

            // 0x0580 to 0x05BF Light Fixtures
            [0x0580] = "Light Fixtures",

            // 0x05C0 to 0x05FF Fan
            [0x05C0] = "Fan",

            // 0x0600 to 0x063F HVAC
            [0x0600] = "HVAC",

            // 0x0640 to 0x067F Air Conditioning
            [0x0640] = "Air Conditioning",

            // 0x0680 to 0x06BF Humidifier
            [0x0680] = "Humidifier",

            // 0x06C0 to 0x06FF Heating
            [0x06C0] = "Heating",

            // 0x0700 to 0x073F Access Control
            [0x0700] = "Access Control",

            // 0x0740 to 0x077F Motorized Device
            [0x0740] = "Motorized Device",

            // 0x0780 to 0x07BF Power Device
            [0x0780] = "Power Device",

            // 0x07C0 to 0x07FF Light Source
            [0x07C0] = "Light Source",

            // 0x0800 to 0x083F Window Covering
            [0x0800] = "Window Covering",

            // 0x0840 to 0x087F Audio Sink
            [0x0840] = "Audio Sink",

            // 0x0880 to 0x08BF Audio Source
            [0x0880] = "Audio Source",

            // 0x08C0 to 0x08FF Motorized Vehicle
            [0x08C0] = "Motorized Vehicle",

            // 0x0900 to 0x093F Domestic Appliance
            [0x0900] = "Domestic Appliance",

            // 0x0940 to 0x097F Wearable Audio Device
            [0x0940] = "Wearable Audio Device",

            // 0x0980 to 0x09BF Aircraft
            [0x0980] = "Aircraft",

            // 0x09C0 to 0x09FF AV Equipment
            [0x09C0] = "AV Equipment",

            // 0x0A00 to 0x0A3F Display Equipment
            [0x0A00] = "Display Equipment",

            // 0x0A40 to 0x0A7F Hearing aid
            [0x0A40] = "Hearing aid",

            // 0x0A80 to 0x0ABF Gaming
            [0x0A80] = "Gaming",

            // 0x0AC0 to 0x0AFF Signage
            [0x0AC0] = "Signage",

            // 0x0C40 to 0x0C7F Pulse Oximeter
            [0x0C40] = "Pulse Oximeter",

            // 0x0C80 to 0x0CBF Weight Scale
            [0x0C80] = "Weight Scale",

            // 0x0CC0 to 0x0CFF Personal Mobility Device
            [0x0CC0] = "Personal Mobility Device",

            // 0x0D00 to 0x0D3F Continuous Glucose Monitor
            [0x0D00] = "Continuous Glucose Monitor",

            // 0x0D40 to 0x0D7F Insulin Pump
            [0x0D40] = "Insulin Pump",

            // 0x0D80 to 0x0DBF Medication Delivery
            [0x0D80] = "Medication Delivery",

            // 0x1440 to 0x147F Outdoor Sports Activity
            [0x1440] = "Outdoor Sports Activity"

        }; // end of Dictionary _btAppearanceCatagoryDict<>

        private static readonly IDictionary<ushort, string> _btAppearanceSubcatagoryDict = new Dictionary<ushort, string>()
        {
            // 0x0000 to 0x003F - Unknown
            [0x0000] = "Generic Unknown",

            // 0x0040 to 0x007F - Phone
            [0x0040] = "Generic Phone",

            // 0x0080 to 0x00BF Computer
            [0x0080] = "Generic Computer",
            [0x0081] = "Desktop Workstation",
            [0x0082] = "Server-class Computer",
            [0x0083] = "Laptop",
            [0x0084] = "Handheld PC/PDA(clamshell)",
            [0x0085] = "Palm-size PC/PDA",
            [0x0086] = "Wearable computer(watch size)",
            [0x0087] = "Tablet",
            [0x0088] = "Docking Station",
            [0x0089] = "All in One",
            [0x008A] = "Blade Server",
            [0x008B] = "Convertible",
            [0x008C] = "Detachable",
            [0x008D] = "IoT Gateway",
            [0x008E] = "Mini PC",
            [0x008F] = "Stick PC",

            // 0x00C0 to 0x00FF Watch
            [0x00C0] = "Generic Watch",
            [0x00C1] = "Sports Watch",
            [0x00C2] = "Smartwatch",

            // 0x0100 to 0x013F Clock
            [0x0100] = "Generic Clock",

            // 0x0140 to 0x017F Display
            [0x0140] = "Generic Display",

            // 0x0180 to 0x01BF Remote Control
            [0x0180] = "Generic Remote Control",

            // 0x01C0 to 0x01FF Eye - glasses
            [0x01C0] = "Generic Eye-glasses",

            // 0x0200 to 0x023F Tag
            [0x0200] = "Generic Tag",

            // 0x0240 to 0x027F Keyring
            [0x0240] = "Generic Keyring",

            // 0x0280 to 0x02BF Media Player
            [0x0280] = "Generic Media Player",

            // 0x02C0 to 0x02FF Barcode Scanner
            [0x02C0] = "Generic Barcode Scanner",

            // 0x0300 to 0x033F Thermometer
            [0x0300] = "Generic Thermometer",
            [0x0301] = "Ear Thermometer",

            // 0x0340 to 0x037F Heart Rate Sensor
            [0x0340] = "Generic Heart Rate Sensor",
            [0x0341] = "Heart Rate Belt",

            // 0x0380 to 0x03BF Blood Pressure
            [0x0380] = "Generic Blood Pressure",
            [0x0381] = "Arm Blood Pressure",
            [0x0382] = "Wrist Blood Pressure",

            // 0x03C0 to 0x03FF Human Interface Device
            [0x03C0] = "Generic Human Interface Device",
            [0x03C1] = "Keyboard",
            [0x03C2] = "Mouse",
            [0x03C3] = "Joystick",
            [0x03C4] = "Gamepad",
            [0x03C5] = "Digitizer Tablet",
            [0x03C6] = "Card Reader",
            [0x03C7] = "Digital Pen",
            [0x03C8] = "Barcode Scanner",
            [0x03C9] = "Touchpad",
            [0x03CA] = "Presentation Remote",

            // 0x0400 to 0x043F Glucose Meter
            [0x0400] = "Generic Glucose Meter",

            // 0x0440 to 0x047F Running Walking Sensor
            [0x0440] = "Generic Running Walking Sensor",
            [0x0441] = "In-Shoe Running Walking Sensor",
            [0x0442] = "On-Shoe Running Walking Sensor",
            [0x0443] = "On-Hip Running Walking Sensor",

            // 0x0480 to 0x04BF Cycling
            [0x0480] = "Generic Cycling",
            [0x0481] = "Cycling Computer",
            [0x0482] = "Speed Sensor",
            [0x0483] = "Cadence Sensor",
            [0x0484] = "Power Sensor",
            [0x0485] = "Speed and Cadence Sensor",

            // 0x04C0 to 0x04FF Control Device
            [0x04C0] = "Generic Control Device",
            [0x04C1] = "Switch",
            [0x04C2] = "Multi-switch",
            [0x04C3] = "Button",
            [0x04C4] = "Slider",
            [0x04C5] = "Rotary Switch",
            [0x04C6] = "Touch Panel",
            [0x04C7] = "Single Switch",
            [0x04C8] = "Double Switch",
            [0x04C9] = "Triple Switch",
            [0x04CA] = "Battery Switch",
            [0x04CB] = "Energy Harvesting Switch",
            [0x04CC] = "Push Button",

            // 0x0500 to 0x053F Network Device
            [0x0500] = "Generic Network Device",
            [0x0501] = "Access Point",
            [0x0502] = "Mesh Device",
            [0x0503] = "Mesh Network Proxy",

            // 0x0540 to 0x057F Sensor
            [0x0540] = "Generic Sensor",
            [0x0541] = "Motion Sensor",
            [0x0542] = "Air quality Sensor",
            [0x0543] = "Temperature Sensor",
            [0x0544] = "Humidity Sensor",
            [0x0545] = "Leak Sensor",
            [0x0546] = "Smoke Sensor",
            [0x0547] = "Occupancy Sensor",
            [0x0548] = "Contact Sensor",
            [0x0549] = "Carbon Monoxide Sensor",
            [0x054A] = "Carbon Dioxide Sensor",
            [0x054B] = "Ambient Light Sensor",
            [0x054C] = "Energy Sensor",
            [0x054D] = "Color Light Sensor",
            [0x054E] = "Rain Sensor",
            [0x054F] = "Fire Sensor",
            [0x0550] = "Wind Sensor",
            [0x0551] = "Proximity Sensor",
            [0x0552] = "Multi-Sensor",
            [0x0553] = "Flush mounted sensor",
            [0x0554] = "Ceiling mounted sensor",
            [0x0555] = "Wall mounted sensor",
            [0x0556] = "Multisensor",
            [0x0557] = "Energy Meter",
            [0x0558] = "Flame Detector",
            [0x0559] = "Vehicle Tire Pressure Sensor",

            // 0x0580 to 0x05BF Light Fixtures
            [0x0580] = "Generic Light Fixtures",
            [0x0581] = "Wall Light",
            [0x0582] = "Ceiling Light",
            [0x0583] = "Floor Light",
            [0x0584] = "Cabinet Light",
            [0x0585] = "Desk Light",
            [0x0586] = "Troffer Light",
            [0x0587] = "Pendant Light",
            [0x0588] = "In-ground Light",
            [0x0589] = "Flood Light",
            [0x058A] = "Underwater Light",
            [0x058B] = "Bollard with Light",
            [0x058C] = "Pathway Light",
            [0x058D] = "Garden Light",
            [0x058E] = "Pole-top Light",
            [0x058F] = "Spotlight",
            [0x0590] = "Linear Light",
            [0x0591] = "Street Light",
            [0x0592] = "Shelves Light",
            [0x0593] = "Bay Light",
            [0x0594] = "Emergency Exit Light",
            [0x0595] = "Light controller",
            [0x0596] = "Light driver",
            [0x0597] = "Bulb",
            [0x0598] = "Low-bay Light",
            [0x0599] = "High-bay Light",

            // 0x05C0 to 0x05FF Fan
            [0x05C0] = "Generic Fan",
            [0x05C1] = "Ceiling Fan",
            [0x05C2] = "Axial Fan",
            [0x05C3] = "Exhaust Fan",
            [0x05C4] = "Pedestal Fan",
            //[0x05C5] = "Desk Fan",
            [0x05C5] = "Wall Fan",

            // 0x0600 to 0x063F HVAC
            [0x0600] = "Generic HVAC",
            [0x0601] = "Thermostat",
            [0x0602] = "Humidifier",
            [0x0603] = "De-humidifier",
            [0x0604] = "Heater",
            [0x0605] = "Radiator",
            [0x0606] = "Boiler",
            [0x0607] = "Heat pump",
            [0x0608] = "Infrared heater",
            [0x0609] = "Radiant panel heater",
            [0x060A] = "Fan heater",
            [0x060B] = "Air curtain",

            // 0x0640 to 0x067F Air Conditioning
            [0x0640] = "Generic Air Conditioning",

            // 0x0680 to 0x06BF Humidifier
            [0x0680] = "Generic Humidifier",

            // 0x06C0 to 0x06FF Heating
            [0x06C0] = "Generic Heating",
            [0x06C1] = "Radiator",
            [0x06C2] = "Boiler",
            [0x06C3] = "Heat Pump",
            [0x06C4] = "Infrared Heater",
            [0x06C5] = "Radiant Panel Heater",
            [0x06C6] = "Fan Heater",
            [0x06C7] = "Air Curtain",

            // 0x0700 to 0x073F Access Control
            [0x0700] = "Generic Access Control",
            [0x0701] = "Access Door",
            [0x0702] = "Garage Door",
            [0x0703] = "Emergency Exit Door",
            [0x0704] = "Access Lock",
            [0x0705] = "Elevator",
            [0x0706] = "Window",
            [0x0707] = "Entrance Gate",
            [0x0708] = "Door Lock",
            [0x0709] = "Locker",

            // 0x0740 to 0x077F Motorized Device
            [0x0740] = "Generic Motorized Device",
            [0x0741] = "Motorized Gate",
            [0x0742] = "Awning",
            [0x0743] = "Blinds or Shades",
            [0x0744] = "Curtains",
            [0x0745] = "Screen",

            // 0x0780 to 0x07BF Power Device
            [0x0780] = "Generic Power Device",
            [0x0781] = "Power Outlet",
            [0x0782] = "Power Strip",
            [0x0783] = "Plug",
            [0x0784] = "Power Supply",
            [0x0785] = "LED Driver",
            [0x0786] = "Fluorescent Lamp Gear",
            [0x0787] = "HID Lamp Gear",
            [0x0788] = "Charge Case",
            [0x0789] = "Power Bank",

            // 0x07C0 to 0x07FF Light Source
            [0x07C0] = "Generic Light Source",
            [0x07C1] = "Incandescent Light Bulb",
            [0x07C2] = "LED Lamp",
            [0x07C3] = "HID Lamp",
            [0x07C4] = "Fluorescent Lamp",
            [0x07C5] = "LED Array",
            [0x07C6] = "Multi-Color LED Array",
            [0x07C7] = "Low voltage halogen",
            [0x07C8] = "Organic light emitting diode (OLED)",

            // 0x0800 to 0x083F Window Covering
            [0x0800] = "Generic Window Covering",
            [0x0801] = "Window Shades",
            [0x0802] = "Window Blinds",
            [0x0803] = "Window Awning",
            [0x0804] = "Window Curtain",
            [0x0805] = "Exterior Shutter",
            [0x0806] = "Exterior Screen",

            // 0x0840 to 0x087F Audio Sink
            [0x0840] = "Generic Audio Sink",
            [0x0841] = "Standalone Speaker",
            [0x0842] = "Soundbar",
            [0x0843] = "Bookshelf Speaker",
            [0x0844] = "Standmounted Speaker",
            [0x0845] = "Speakerphone",

            // 0x0880 to 0x08BF Audio Source
            [0x0880] = "Generic Audio Source",
            [0x0881] = "Microphone",
            [0x0882] = "Alarm",
            [0x0883] = "Bell",
            [0x0884] = "Horn",
            [0x0885] = "Broadcasting Device",
            [0x0886] = "Service Desk",
            [0x0887] = "Kiosk",
            [0x0888] = "Broadcasting Room",
            [0x0889] = "Auditorium",

            // 0x08C0 to 0x08FF Motorized Vehicle
            [0x08C0] = "Generic Motorized Vehicle",
            [0x08C1] = "Car",
            [0x08C2] = "Large Goods Vehicle",
            [0x08C3] = "2-Wheeled Vehicle",
            [0x08C4] = "Motorbike",
            [0x08C5] = "Scooter",
            [0x08C6] = "Moped",
            [0x08C7] = "3-Wheeled Vehicle",
            [0x08C8] = "Light Vehicle",
            [0x08C9] = "Quad Bike",
            [0x08CA] = "Minibus",
            [0x08CB] = "Bus",
            [0x08CC] = "Trolley",
            [0x08CD] = "Agricultural Vehicle",
            [0x08CE] = "Camper/Caravan",
            [0x08CF] = "Recreational Vehicle/Motor Home",

            // 0x0900 to 0x093F Domestic Appliance
            [0x0900] = "Generic Domestic Appliance",
            [0x0901] = "Refrigerator",
            [0x0902] = "Freezer",
            [0x0903] = "Oven",
            [0x0904] = "Microwave",
            [0x0905] = "Toaster",
            [0x0906] = "Washing Machine",
            [0x0907] = "Dryer",
            [0x0908] = "Coffee maker",
            [0x0909] = "Clothes iron",
            [0x090A] = "Curling iron",
            [0x090B] = "Hair dryer",
            [0x090C] = "Vacuum cleaner",
            [0x090D] = "Robotic vacuum cleaner",
            [0x090E] = "Rice cooker",
            [0x090F] = "Clothes steamer",

            // 0x0940 to 0x097F Wearable Audio Device
            [0x0940] = "Generic Wearable Audio Device",
            [0x0941] = "Earbud",
            [0x0942] = "Headset",
            [0x0943] = "Headphones",
            [0x0944] = "Neck Band",

            // 0x0980 to 0x09BF Aircraft
            [0x0980] = "Generic Aircraft",
            [0x0981] = "Light Aircraft",
            [0x0982] = "Microlight",
            [0x0983] = "Paraglider",
            [0x0984] = "Large Passenger Aircraft",

            // 0x09C0 to 0x09FF AV Equipment
            [0x09C0] = "Generic AV Equipment",
            [0x09C1] = "Amplifier",
            [0x09C2] = "Receiver",
            [0x09C3] = "Radio",
            [0x09C4] = "Tuner",
            [0x09C5] = "Turntable",
            [0x09C6] = "CD Player",
            [0x09C7] = "DVD Player",
            [0x09C8] = "Bluray Player",
            [0x09C9] = "Optical Disc Player",
            [0x09CA] = "Set-Top Box",

            // 0x0A00 to 0x0A3F Display Equipment
            [0x0A00] = "Generic Display Equipment",
            [0x0A01] = "Television",
            [0x0A02] = "Monitor",
            [0x0A03] = "Projector",

            // 0x0A40 to 0x0A7F Hearing aid
            [0x0A40] = "Generic Hearing aid",
            [0x0A41] = "In-ear hearing aid",
            [0x0A42] = "Behind-ear hearing aid",
            [0x0A43] = "Cochlear Implant",

            // 0x0A80 to 0x0ABF Gaming
            [0x0A80] = "Generic Gaming",
            [0x0A81] = "Home Video Game Console",
            [0x0A82] = "Portable handheld console",

            // 0x0AC0 to 0x0AFF Signage
            [0x0AC0] = "Generic Signage",
            [0x0AC1] = "Digital Signage",
            [0x0AC2] = "Electronic Label",

            // 0x0C40 to 0x0C7F Pulse Oximeter
            [0x0C40] = "Generic Pulse Oximeter",
            [0x0C41] = "Fingertip Pulse Oximeter",
            [0x0C42] = "Wrist Worn Pulse Oximeter",

            // 0x0C80 to 0x0CBF Weight Scale
            [0x0C80] = "Generic Weight Scale",

            // 0x0CC0 to 0x0CFF Personal Mobility Device
            [0x0CC0] = "Generic Personal Mobility Device",
            [0x0CC1] = "Powered Wheelchair",
            [0x0CC2] = "Mobility Scooter",

            // 0x0D00 to 0x0D3F Continuous Glucose Monitor
            [0x0D00] = "Generic Continuous Glucose Monitor",

            // 0x0D40 to 0x0D7F Insulin Pump
            [0x0D40] = "Generic Insulin Pump",
            [0x0D41] = "Insulin Pump, durable pump",
            [0x0D44] = "Insulin Pump, patch pump",
            [0x0D48] = "Insulin Pen",

            // 0x0D80 to 0x0DBF Medication Delivery
            [0x0D80] = "Generic Medication Delivery",

            // 0x1440 to 0x147F Outdoor Sports Activity
            [0x1440] = "Generic Outdoor Sports Activity",
            [0x1441] = "Location Display",
            [0x1442] = "Location and Navigation Display",
            [0x1443] = "Location Pod",
            [0x1444] = "Location and Navigation"

        }; // end of Dictionary _btAppearanceSubcatagoryDict<>

    } // end of class BTAppearance

    public class BTProtocolID
    {
        public BTProtocolID() { }

        public BTProtocolID(ushort protocolIdValue)
        {

            Value = protocolIdValue;

            _btProtocolIdDict.TryGetValue(protocolIdValue, out string description);

            if (description != null)
                Description = description;
            else
                Description = "Undefined";

        } // end of initializer

        public ushort Value { get; set; }
        public string Description { get; }

        private static readonly IDictionary<ushort, string> _btProtocolIdDict = new Dictionary<ushort, string>()
        {
            [0x0001] = "SDP",
            [0x0002] = "UDP",
            [0x0003] = "RFCOMM",
            [0x0004] = "TCP",
            [0x0005] = "TCS-BIN",
            [0x0006] = "TCS-AT",
            [0x0007] = "ATT",
            [0x0008] = "OBEX",
            [0x0009] = "IP",
            [0x000A] = "FTP",
            [0x000C] = "HTTP",
            [0x000E] = "WSP",
            [0x000F] = "BNEP",
            [0x0010] = "UPNP",
            [0x0011] = "HIDP",
            [0x0012] = "HardcopyControlChannel",
            [0x0014] = "HardcopyDataChannel",
            [0x0016] = "HardcopyNotification",
            [0x0017] = "AVCTP",
            [0x0019] = "AVDTP",
            [0x001B] = "CMTP",
            [0x001E] = "MCAPControlChannel",
            [0x001F] = "MCAPDataChannel",
            [0x0100] = "L2CAP"

        }; // end of Dictionary _btProtocolIdDict<>

    } // end of class BTProtocolID

    public class BTSrvClassProfile
    {
        public BTSrvClassProfile() { }

        public BTSrvClassProfile(ushort serviceClassProfile)
        {

            Value = serviceClassProfile;

            _btSrvClassAndProfileDict.TryGetValue(serviceClassProfile, out string description);

            if (description != null)
                Description = description;
            else
                Description = "Undefined";

        } // end of initializer

        public ushort Value { get; set; }
        public string Description { get; }

        private static readonly IDictionary<ushort, string> _btSrvClassAndProfileDict = new Dictionary<ushort, string>()
        {
            [0x1000] = "ServiceDiscoveryServerServiceClassID",
            [0x1001] = "BrowseGroupDescriptorServiceClassID",
            [0x1101] = "SerialPort",
            [0x1102] = "LANAccessUsingPPP",
            [0x1103] = "DialupNetworking",
            [0x1104] = "IrMCSync",
            [0x1105] = "OBEXObjectPush",
            [0x1106] = "OBEXFileTransfer",
            [0x1107] = "IrMCSyncCommand",
            [0x1108] = "Headset",
            [0x1109] = "CordlessTelephony",
            [0x110A] = "AudioSource",
            [0x110B] = "AudioSink",
            [0x110C] = "A/V_RemoteControlTarget",
            [0x110D] = "AdvancedAudioDistribution",
            [0x110E] = "A/V_RemoteControl",
            [0x110F] = "A/V_RemoteControlController",
            [0x1110] = "Intercom",
            [0x1111] = "Fax",
            [0x1112] = "Headset - Audio Gateway",
            [0x1113] = "WAP",
            [0x1114] = "WAP_CLIENT",
            [0x1115] = "PANU",
            [0x1116] = "NAP",
            [0x1117] = "GN",
            [0x1118] = "DirectPrinting",
            [0x1119] = "ReferencePrinting",
            [0x111A] = "Basic Imaging Profile",
            [0x111B] = "ImagingResponder",
            [0x111C] = "ImagingAutomaticArchive",
            [0x111D] = "ImagingReferencedObjects",
            [0x111E] = "Handsfree",
            [0x111F] = "HandsfreeAudioGateway",
            [0x1120] = "DirectPrintingReferenceObjectsService",
            [0x1121] = "ReflectedUI",
            [0x1122] = "BasicPrinting",
            [0x1123] = "PrintingStatus",
            [0x1124] = "HumanInterfaceDeviceService",
            [0x1125] = "HardcopyCableReplacement",
            [0x1126] = "HCR_Print",
            [0x1127] = "HCR_Scan",
            [0x1128] = "Common_ISDN_Access",
            [0x112D] = "SIM_Access",
            [0x112E] = "Phonebook Access - PCE",
            [0x112F] = "Phonebook Access - PSE",
            [0x1130] = "Phonebook Access",
            [0x1131] = "Headset - HS",
            [0x1132] = "Message Access Server",
            [0x1133] = "Message Notification Server",
            [0x1134] = "Message Access Profile",
            [0x1135] = "GNSS",
            [0x1136] = "GNSS_Server",
            [0x1137] = "3D Display",
            [0x1138] = "3D Glasses",
            [0x1139] = "3D Synchronization",
            [0x113A] = "MPS Profile UUID",
            [0x113B] = "MPS SC UUID",
            [0x113C] = "CTN Access Service",
            [0x113D] = "CTN Notification Service",
            [0x113E] = "CTN Profile",
            [0x1200] = "PnPInformation",
            [0x1201] = "GenericNetworking",
            [0x1202] = "GenericFileTransfer",
            [0x1203] = "GenericAudio",
            [0x1204] = "GenericTelephony",
            [0x1205] = "UPNP_Service",
            [0x1206] = "UPNP_IP_Service",
            [0x1300] = "ESDP_UPNP_IP_PAN",
            [0x1301] = "ESDP_UPNP_IP_LAP",
            [0x1302] = "ESDP_UPNP_L2CAP",
            [0x1303] = "VideoSource",
            [0x1304] = "VideoSink",
            [0x1305] = "VideoDistribution",
            [0x1400] = "HDP",
            [0x1401] = "HDP Source",
            [0x1402] = "HDP Sink"

        }; // end of Dictionary _btSrvClassAndProfileDict<>

    } // end of class BTSrvClassProfile

    public class BTGATTService
    {
        public BTGATTService() { }

        public BTGATTService(ushort gattService)
        {

            Value = gattService;

            _btGATTServiceDict.TryGetValue(gattService, out string description);

            if (description != null)
                Description = description;
            else
                Description = "Undefined";

        } // end of initializer

        public ushort Value { get; set; }
        public string Description { get; }

        private static readonly IDictionary<ushort, string> _btGATTServiceDict = new Dictionary<ushort, string>()
        {
            [0x1800] = "Generic Access",
            [0x1801] = "Generic Attribute",
            [0x1802] = "Immediate Alert",
            [0x1803] = "Link Loss",
            [0x1804] = "Tx Power",
            [0x1805] = "Current Time",
            [0x1806] = "Reference Time Update",
            [0x1807] = "Next DST Change",
            [0x1808] = "Glucose",
            [0x1809] = "Health Thermometer",
            [0x180A] = "Device Information",
            [0x180D] = "Heart Rate",
            [0x180E] = "Phone Alert Status",
            [0x180F] = "Battery",
            [0x1810] = "Blood Pressure",
            [0x1811] = "Alert Notification",
            [0x1812] = "Human Interface Device",
            [0x1813] = "Scan Parameters",
            [0x1814] = "Running Speed and Cadence",
            [0x1815] = "Automation IO",
            [0x1816] = "Cycling Speed and Cadence",
            [0x1818] = "Cycling Power",
            [0x1819] = "Location and Navigation",
            [0x181A] = "Environmental Sensing",
            [0x181B] = "Body Composition",
            [0x181C] = "User Data",
            [0x181D] = "Weight Scale",
            [0x181E] = "Bond Management",
            [0x181F] = "Continuous Glucose Monitoring",
            [0x1820] = "Internet Protocol Support",
            [0x1821] = "Indoor Positioning",
            [0x1822] = "Pulse Oximeter",
            [0x1823] = "HTTP Proxy",
            [0x1824] = "Transport Discovery",
            [0x1825] = "Object Transfer",
            [0x1826] = "Fitness Machine",
            [0x1827] = "Mesh Provisioning",
            [0x1828] = "Mesh Proxy",
            [0x1829] = "Reconnection Configuration",
            [0x183A] = "Insulin Delivery",
            [0x183B] = "Binary Sensor",
            [0x183C] = "Emergency Configuration",
            [0x183E] = "Physical Activity Monitor",
            [0x1843] = "Audio Input Control",
            [0x1844] = "Volume Control",
            [0x1845] = "Volume Offset Control",
            [0x1846] = "Coordinated Set Identification",
            [0x1847] = "Device Time",
            [0x1848] = "Media Control",
            [0x1849] = "Generic Media Control",
            [0x184A] = "Constant Tone Extension",
            [0x184B] = "Telephone Bearer",
            [0x184C] = "Generic Telephone Bearer",
            [0x184D] = "Microphone Control",
            [0x184E] = "Audio Stream Control",
            [0x184F] = "Broadcast Audio Scan",
            [0x1850] = "Published Audio Capabilities",
            [0x1851] = "Basic Audio Announcement",
            [0x1852] = "Broadcast Audio Announcement",
            [0x1853] = "Common Audio",
            [0x1854] = "Hearing Access",
            [0x1855] = "TMAS",
            [0x1856] = "Public Broadcast Announcement"

        }; // end of Dictionary _btGATTServiceDict<>

    } // end of class BTGATTService
}

