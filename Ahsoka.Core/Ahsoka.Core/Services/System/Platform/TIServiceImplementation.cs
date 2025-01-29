using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;

namespace Ahsoka.Services.System.Platform;

[ExcludeFromCodeCoverage]
internal class TIServiceImplementation : IServiceImplementation
{
    #region Fields
    readonly string[] logoFileTypes = { ".png", ".jpg" };

    public const string NativeLibraryName = "AhsokaLib/Ahsoka.Services.ST.so";
    private const string firmwareModelNumberPath = "/sys/firmware/devicetree/base/model";
    private const string firmwareSerialNumberPath = "/sys/block/mmcblk0/device/cid";
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

    private void BuzzerInit()
    {

    }

    void IServiceImplementation.SetBrightness(int percent)
    {
        if (percent < 0)
            percent = 0;
        else if (percent > 100)
            percent = 100;
    }

    int IServiceImplementation.GetBrightness()
    {
        return 100;
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

    private static Dictionary<string, string> GetScreenInfo()
    {
        Dictionary<string, string> results = new();

        return results;
    }

    public double GetCPUTemp()
    {
        string path = "/sys/devices/virtual/thermal/thermal_zone0/temp";
        if (File.Exists(path))
        {
            var value = int.Parse(File.ReadAllText(path));
            return (double)value / 1000;
        }

        return 0;
    }

    public Dictionary<string, string> GetPlatformVersions()
    {
        ProcessUtility.RunProcessScript("uname -r", "", out string stdout, out string _);

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
        string installPath = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(Ahsoka.System.Hardware.InstallerPaths.BootLogoFileName);
        File.Copy(logoPath, installPath, true);
    }
    #endregion
}
