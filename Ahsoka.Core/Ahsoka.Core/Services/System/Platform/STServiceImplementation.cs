using Ahsoka.Core;
using Ahsoka.Core.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;

namespace Ahsoka.Services.System.Platform;

[ExcludeFromCodeCoverage]
internal class STServiceImplementation : IServiceImplementation
{
    readonly string[] logoFileTypes = { ".png", ".jpg" };

    #region NativeLibrary
    private const string systmeBacklightService = "/lib/systemd/system/systemd-backlight@.service";
    private const string firmwareModelNumberPath = "/sys/firmware/devicetree/base/model";
    private const string firmwareSerialNumberPath = "/sys/firmware/devicetree/base/serial-number";
    private const string backlightDriverPath = "/sys/devices/platform/panel_backlight/backlight/panel_backlight";
    #endregion

    #region Interface

    string IServiceImplementation.GetHardwareModel()
    {
        return File.ReadAllText(firmwareModelNumberPath).Trim();
    }

    string IServiceImplementation.GetHardwareSerialNumber()
    {
        return File.ReadAllText(firmwareSerialNumberPath).Trim().Replace("\0", "");
    }

    void IServiceImplementation.SetBrightness(int percent)
    {
        if (percent < 0)
            percent = 0;
        else if (percent > 100)
            percent = 100;

        // Disable Backlight Service
        if (File.Exists(systmeBacklightService))
            File.Move(systmeBacklightService, $"{systmeBacklightService}.old",true);

        if (File.Exists(Path.Combine(backlightDriverPath, "brightness")))
        {
            int originalValue = int.Parse(File.ReadAllText(Path.Combine(backlightDriverPath, "brightness")));
            Console.WriteLine(originalValue);

            int brightnessSteps = int.Parse(File.ReadAllText(Path.Combine(backlightDriverPath, "max_brightness")));
            float stepsSize = 100.0f / brightnessSteps;
            int rawValue = (int)Math.Round((percent / stepsSize), 0);
            File.WriteAllText(Path.Combine(backlightDriverPath, "brightness"), rawValue.ToString());

            int newValue = int.Parse(File.ReadAllText(Path.Combine(backlightDriverPath, "brightness")));
            Console.WriteLine(newValue);
        }

    }

    int IServiceImplementation.GetBrightness()
    {
        using var stopwatch = new AhsokaStopwatch("GetBrightness");

        int brightnessSteps = int.Parse(File.ReadAllText(Path.Combine(backlightDriverPath, "max_brightness")));
        int rawValue = int.Parse(File.ReadAllText(Path.Combine(backlightDriverPath, "brightness")));
        return (int)Math.Round(rawValue * (100.0f / brightnessSteps), 0);
    }

    void IServiceImplementation.Reboot()
    {
        LinuxSystemUtilities.Reboot();
    }

    void IServiceImplementation.ShutdownApplication(int shutDownCode)
    {
        // Request Application Unit to Shutdown
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Requesting Application Service Shut Down...");
        ProcessUtility.RunProcessAsync("systemctl", $"stop Ahsoka.Application", null, null, CancellationToken.None);

        // Exit Application
        Environment.Exit(shutDownCode);
    }

    internal static Dictionary<string, string> GetScreenInfo()
    {
        // Touch IDs
        string VendorId = "DNE";
        string ProductId = "DNE";
        try
        {
            string fileData = File.ReadAllText("/sys/bus/i2c/drivers/atmel_mxt_ts/0-004a/object");
            const string search = "T38:\n"; // Section with Version Info
            string[] versionData = fileData.Substring(fileData.IndexOf(search) + search.Length, 11).Split(' ');
            VendorId = $"0x{versionData[1]}{versionData[0]}";
            ProductId = $"0x{versionData[3]}{versionData[2]}";
        }
        catch { } // Prevent crash if not there.

        // Touch Versions and CRC
        string FirmwareVersion = "DNE";
        string HardwareVersion = "DNE";
        string ConfigCrc = "DNE";
        try
        {
            FirmwareVersion = File.ReadAllText("/sys/devices/platform/soc/5c002000.i2c/i2c-0/0-004a/fw_version"); // STUB - weird case where these didn't exist after stop
            HardwareVersion = File.ReadAllText("/sys/devices/platform/soc/5c002000.i2c/i2c-0/0-004a/hw_version");
            ConfigCrc = File.ReadAllText("/sys/devices/platform/soc/5c002000.i2c/i2c-0/0-004a/config_crc");
        }
        catch { } // Prevent crash if not there.

        Dictionary<string, string> results = new()
        {
            { SystemServiceMessages.TouchVendorID, VendorId },
            { SystemServiceMessages.TouchProductID, ProductId },
            { SystemServiceMessages.TouchHardwareVersion, HardwareVersion },
            { SystemServiceMessages.TouchFirmwareVersion, FirmwareVersion },
            { SystemServiceMessages.TouchConfigCRC, ConfigCrc }
        };

        return results;
    }

    public double GetCPUTemp()
    {
        string path = "/sys/devices/virtual/thermal/thermal_zone0/hwmon0/temp1_input";
        if (File.Exists(path))
        {
            var value = int.Parse(File.ReadAllText(path));
            return (double)value / 1000;
        }

        return 0;
    }

    public Dictionary<string, string> GetPlatformVersions()
    {
        ProcessUtility.RunProcessScript("bsp-info -v", "", out string stdout, out string _);

        Dictionary<string, string> results = new()
        {
            { SystemServiceMessages.BSPVersion, stdout.Replace("BSP Version:","").Trim() }
        };

        foreach (KeyValuePair<string, string> pair in GetScreenInfo())
            results.Add(pair.Key, pair.Value);

        return results;
    }

    void IServiceImplementation.SetBootLogo(string logoPath)
    {
        if (!File.Exists(logoPath) || !logoFileTypes.Any(x => logoPath.EndsWith(x))) return;
        string installPath = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(Ahsoka.Core.Hardware.InstallerPaths.BootLogoFileName);
        File.Copy(logoPath, installPath, true);
    }
    #endregion
}
