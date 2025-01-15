using Ahsoka.ServiceFramework;
using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Device.Handlers;

internal class Esp32Handler : DeviceHandlerBase
{
    private readonly object syncRoot = new();

    internal Esp32Handler()
    {
        this.SupportsFirmware = true;
    }

    internal override List<UsbDeviceId> Init(DeviceInfo deviceInfo, Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        Task.Run(() =>
        {
            ActivateESPModule();
            deviceInfo.State = GetDeviceState();
            sendNotification(DeviceMessageTypes.Ids.OnboardDeviceReady, deviceInfo,null);
        });

        return new();
    }

    internal override DeviceState GetDeviceState()
    {
        string path = "/sys/devices/platform/soc/48004000.mmc/power/runtime_status";

        if (!Path.Exists(path))
            return DeviceState.Ready;

        string result = File.ReadAllText(path);
        if (result.Contains("active"))
            return DeviceState.Enabled;
        else
            return DeviceState.Disabled;
    }

    internal override void EnableDevice(DeviceInfo deviceInfo, Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        // If we are already booted, then no point in going through this function.
        if (GetDeviceState() == DeviceState.Enabled)
        {
            sendNotification(DeviceMessageTypes.Ids.DeviceEnabled, deviceInfo, null);
            return;
        }

        Task.Run(() =>
        {
            using var stopwatch = new AhsokaStopwatch("ESP Init");

            EnableESPDevice();

            // Wait up to a 1 seconds for the device to be active
            for (int i = 0; i < 20; i++)
            {
                if (GetDeviceState() == DeviceState.Enabled)
                {
                    base.EnableDevice(deviceInfo, sendNotification);
                    return;
                }
                else
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Waiting for ESP Device");
                    Thread.Sleep(100);
                }
            }
        });
    }

    internal override void DisableDevice(DeviceInfo deviceInfo, Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        DisableESPDevice();
        base.DisableDevice(deviceInfo, sendNotification);
    }


    private static void ActivateESPModule()
    {
        ProcessUtility.RunProcess("/bin/sh", "-c \"rm -f /dev/esps0 ; /bin/mknod -m 0666 /dev/esps0 c 221 0\"", null, out string log, out string error);
        if (log != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network log: {log}");
        if (error != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network error: {error}");

        ProcessUtility.RunProcess("/bin/sh", "-c \"/sbin/modprobe esp32_sdio\"", null, out log, out error);
        if (log != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network log: {log}");
        if (error != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network error: {error}");

    }

    private static void EnableESPDevice()
    {
        ProcessUtility.RunProcess("/bin/sh", "-c \"gpioset gpiochip2 15=1\"", null, out string log, out string error);
        if (log != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network log: {log}");
        if (error != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network error: {error}");

        Thread.Sleep(1000);
       
        ProcessUtility.RunProcess("/bin/sh", "-c \"echo 1 > /sys/devices/platform/soc/48004000.mmc/manual_detect_card_change\"", null, out log, out error);
        if (log != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network log: {log}");
        if (error != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network error: {error}");
    }

    private static void DisableESPDevice()
    {
        ProcessUtility.RunProcess("/bin/sh", "-c \"gpioset gpiochip2 15=0\"", null, out string log, out string error);
        if (log != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network log: {log}");
        if (error != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network error: {error}");

        ProcessUtility.RunProcess("/bin/sh", "-c \"echo 0 > /sys/devices/platform/soc/48004000.mmc/manual_detect_card_change\"", null, out log, out error);
        if (log != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network log: {log}");
        if (error != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Network error: {error}");
    }


    internal override byte[] GetFirmwareData(PlatformFamily platformFamily, string platformQualifier)
    {
        string path = Path.Combine(PlatformSupportPathInfo.GetPlatformResourcePath(platformFamily), "ESPFirmware.zip");
        if (File.Exists(path))
            return File.ReadAllBytes(path);
        else
            return null;
    }

    internal override void InstallLocal(string fwFile, LocalProgress<InstallLogInfo> progress)
    {
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        progress.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Installing ESP Firmware" });

        ZipFile.ExtractToDirectory(fwFile, outputPath);

        ProcessUtility.RunProcess("/bin/sh", $"-c \"chmod +x ./ESPFirmware/esp32_init.sh ; ./ESPFirmware/esp32_init.sh {outputPath}\"", outputPath, out string log, out string error);
        if (error != "")
            throw new Exception(error);

        progress.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"ESP Firmware Completed" });

        Directory.Delete(outputPath, true);

        base.InstallLocal(fwFile, progress);
    }

    public void InstallRemote(HardwareInfo info,
        SshClient sshClient,
        ScpClient scpClient,
        IProgress<string> progress)
    {
        progress.Report("");
        progress.Report($"Adding network Firmware");

        var firmwareData = GetFirmwareData(info.PlatformFamily, info.PlatformQualifier);

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
}
