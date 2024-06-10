using Ahsoka.ServiceFramework;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Renci.SshNet;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace Ahsoka.Services.Network;

[ExcludeFromCodeCoverage]
internal class ESPNetworkFirmware : INetworkFirmwareBase
{
    private readonly object syncRoot = new();
    private enum ESPBootedState
    {
        Off,
        Init,
        Boot,
    };

    private static ESPBootedState bootState = ESPBootedState.Off;

    public void Init()
    {
        lock (syncRoot)
        {
            // If we are already booted, then no point in going through this function.
            if (bootState >= ESPBootedState.Init)
                return;

            using var stopwatch = new AhsokaStopwatch("ESP Init");
            CreateESPDeviceFile();

            bootState = ESPBootedState.Init;
        }
    }

    public void InstallLocal(string pathToFirmware)
    {
        var pathToZip = Path.Combine(pathToFirmware, "firmware.zip");

        ZipFile.ExtractToDirectory(pathToZip, pathToFirmware);

        ProcessUtility.RunProcess("/bin/sh", $"-c \"chmod +x ./ESPFirmware/esp32_init.sh ; ./ESPFirmware/esp32_init.sh {pathToFirmware}\"", pathToFirmware, out string log, out string error);
        if (error != "")
            throw new Exception(error);

        Directory.Delete(pathToFirmware, true);
    }

    public void InstallRemote(HardwareInfo info,
        SshClient sshClient,
        ScpClient scpClient,
        IProgress<string> progress)
    {
        progress.Report("");
        progress.Report($"Adding network Firmware");

        var firmwareData = GetData(info.PlatformFamily);

        if (firmwareData == null)
            throw new Exception("ESP Firmware Package (firmware.zip) is missing from the resources directory");

        if (firmwareData != null)
        {
            var zipFileTargetDirectory = $"/tmp/{Guid.NewGuid()}";
            var zipFileTargetPath = $"{zipFileTargetDirectory}/firmware.zip";
            var commandResult = sshClient.RunCommand($"mkdir -p {zipFileTargetDirectory}");

            if (commandResult.Result != string.Empty)
                throw new Exception(commandResult.Result);

            using MemoryStream data = new(firmwareData);

            scpClient.Upload(data, zipFileTargetPath);
            progress.Report($"   100% Uploaded");

            progress.Report("");
            progress.Report($"Unziping Firmware on Target");
            var unzipInstall = sshClient.RunCommand($"unzip {zipFileTargetPath} -d {zipFileTargetDirectory}");
            progress.Report(unzipInstall.Result);

            progress.Report($"Removing Temp File");
            var deleteInstallFile = sshClient.RunCommand($"rm {zipFileTargetPath}");

            if (deleteInstallFile.Error != string.Empty)
                throw new Exception(deleteInstallFile.Result);

            progress.Report($"Granting Execute to Install Script");
            commandResult = sshClient.RunCommand($"chmod +x {zipFileTargetDirectory}/ESPFirmware/esp32_init.sh");

            progress.Report($"Installing Network Firmware");
            commandResult = sshClient.RunCommand($"{zipFileTargetDirectory}/ESPFirmware/esp32_init.sh {zipFileTargetDirectory}");
            if (commandResult.Error != string.Empty)
                throw new Exception(commandResult.Error);

            commandResult = sshClient.RunCommand($"rm -r {zipFileTargetDirectory}");
            if (commandResult.Result != string.Empty)
                throw new Exception(commandResult.Result);
        }
    }

    public byte[] GetData(Ahsoka.System.PlatformFamily platformFamily)
    {
        string path = Path.Combine(PlatformSupportPathInfo.GetPlatformResourcePath(platformFamily), "ESPFirmware.zip");
        if (File.Exists(path))
            return File.ReadAllBytes(path);
        else
            return null;

    }

    public void Boot()
    {
        lock (syncRoot)
        {
            if (bootState >= ESPBootedState.Boot)
                return;

            ProcessUtility.RunProcess("/bin/sh", "-c \"/lib/esp32/esp32_power.sh on\"", null, out string log, out string error);
            if (log != "")
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network log: {log}");
            if (error != "")
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network error: {error}");

            // Wait up to a 1 seconds for the device to be active
            for (int i = 0; i < 20; i++)
            {
                string path = "/sys/devices/platform/soc/48004000.mmc/power/runtime_status";
                string result = File.ReadAllText(path);
                if (result.Contains("active"))
                    return;
                else
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Waiting for Device Detect {result.Trim()}");
                    Thread.Sleep(100);
                }
            }
        }
    }

    public void Shutdown()
    {
        Console.WriteLine("Shutdown Called But Not Shutting Down");
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network log: Shutdown Requested But Leaving Power On");

        return;   // We have not handled shutdown correctly yet
        /*ProcessUtility.RunProcess("/bin/sh", "-c \"/lib/esp32/esp32_power.sh\"", null, out string log, out string error);
        if (log != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network log: {log}");
        if (error != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network error: {error}");*/
    }

    public void CreateESPDeviceFile()
    {
        ProcessUtility.RunProcess("/bin/sh", "-c \"rm -f /dev/esps0 ; /bin/mknod -m 0666 /dev/esps0 c 221 0\"", null, out string log, out string error);
        if (log != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network log: {log}");
        if (error != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network error: {error}");
    }
}