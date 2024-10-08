using Ahsoka.Services.Network.ESPWifi;
using Ahsoka.Services.Network.Platform;
using Ahsoka.Services.System.Platform;
using Ahsoka.Services.Video;
using Ahsoka.System;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Commands;

[ExcludeFromCodeCoverage]
[CommandLinePlugin]
internal static class TestCommands
{
    [CommandLineMethod(@"     --Test: Run Test Script
            [WiFi] Test WiFi Directly
            [WiFiService] Test WiFi Via Ahsoka Services
            [WiFiServiceAP] Test WiFi AccessPoint Via Ahsoka Services
            [SystemService] Test System Services
            [MonitorIgnitionNotification] Test Ignition Pin Notifications
            [IOService] Test IO on the Display.")]
    private static void Test(string[] args)
    {
        if (args.Length >= 1)
            if (args[0] == "WiFi")
                ESPTest.RunTest();
            else if (args[0] == "WiFiService")
                NetworkTests.TestNetworkService();
            else if (args[0] == "WiFiServiceAP")
                NetworkTests.TestNetworkServiceAP();
            else if (args[0] == "SystemService")
                SystemTests.TestSystemService();
            else if (args[0] == "VideoTest")
                VideoTests.TestVideo(args[1]);
            else
                Console.WriteLine($"Unrecognized test: {args[0]}");
    }
}