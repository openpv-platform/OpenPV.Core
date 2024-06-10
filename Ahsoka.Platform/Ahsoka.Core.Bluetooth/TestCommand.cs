using Ahsoka.Services.Bluetooth.Platform;
using Ahsoka.System;
using System;
using System.Linq;

namespace Ahsoka.Commands;

[CommandLinePlugin]
internal static class TestCommands
{
    [CommandLineMethod(@"     --TestBluetooth: Run Test Script
            [BluetoothService] Test Bluetooth via Ahsoka Services
            [BluetoothDevMon] Test Bluetooth via DevMon")]
    private static void TestBluetooth(string[] args)
    {
        if (args.Length >= 1)
            if (args[0] == "BluetoothService")
                BluetoothServiceTests.TestBluetoothService();
            else if (args[0] == "BluetoothDevMon")
            {
                string[] tokens = null;
                if (args.Length > 2)
                    tokens = args.Skip(2).ToArray();
                BluetoothDevMon.BTDevMon(tokens);
            }
            else
                Console.WriteLine($"Unrecognized test: {args[0]}");
    }
}