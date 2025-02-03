using Ahsoka.Core;
using Ahsoka.Core.Utility;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.Network;

[ExcludeFromCodeCoverage]
internal class LinuxNetworkUtilities
{
    public static void StartAdapter(string adapterId)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"COMMAND: networkctl up {adapterId}");
        ProcessUtility.RunProcess("networkctl", $"up {adapterId}", null, out string log, out string error);
        if (error != "")
            Console.WriteLine($"stderr: {error}");
    }

    public static void StopAdapter(string adapterId)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"networkctl down {adapterId}");
        ProcessUtility.RunProcess("networkctl", $"down {adapterId}", null, out string log, out string error);
        if (error != "")
            Console.WriteLine($"stderr: {error}");
    }

    public static void AdapterRequestDHCPAddress(string adapterId)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Requesting IP Address: networkctl reconfigure {adapterId}");
        ProcessUtility.RunProcess("networkctl", $"reconfigure {adapterId}", null, out string log, out string error);
        if (error != "")
            Console.WriteLine($"stderr: {error}");
    }

    internal static bool WaitForTimeSync(int timeoutSeconds)
    {
        if (timeoutSeconds <= 0)
        {
            Console.WriteLine("Skipping Time Sync");
            return true;
        }

        var dt = DateTime.Now.AddSeconds(timeoutSeconds);

        while (dt > DateTime.Now)
        {
            // Restarting Time Sync Service
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "Restarting Time Sync Service");
            ProcessUtility.RunProcess("systemctl", "restart systemd-timesyncd.service", null, out string log, out string error);
            if (error != "")
                Console.WriteLine($"stderr: {error}");

            // Setup interface and request address via DHCP
            log = error = "";
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "Waiting for Time Sync...");
            ProcessUtility.RunProcess("timedatectl", "status", null, out log, out error);
            if (error != "")
            {
                Console.WriteLine($"stderr: {error}");
                return false;
            }
            if (log.Contains("System clock synchronized: yes"))
                return true;
        }
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"System clock not synchronized given {timeoutSeconds}s timeout");
        return false;

    }

}
