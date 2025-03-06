using Ahsoka.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Ahsoka.Services.System.Platform;

[ExcludeFromCodeCoverage]
internal class DesktopServiceImplementation : IServiceImplementation
{
    int currentBrightness = 5;
    int brightnessSteps = 19; // Matching Select

    public string GetHardwareSerialNumber()
    {
        if (OperatingSystem.IsWindows())
            return GetWMICValue("bios get serialnumber");
        else
            return "12345678";
    }

    public string GetHardwareModel()
    {
        if (OperatingSystem.IsWindows())
            return GetWMICValue("csproduct get name");
        else
            return "Generic Desktop";
    }

    void IServiceImplementation.SetBrightness(int percent)
    {
        if (percent < 0)
            percent = 0;
        else if (percent > 100)
            percent = 100;

        currentBrightness = (int)Math.Round(percent / (100.0f / brightnessSteps));
    }

    int IServiceImplementation.GetBrightness()
    {
        int rawValue = currentBrightness;
        return (int)Math.Round(rawValue * (100.0f / brightnessSteps));
    }

    private static string GetWMICValue(string args)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = @"wmic",
            Arguments = args,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        var process = Process.Start(processStartInfo);
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output.Substring(output.IndexOf("\r\n")).Replace("\r", "").Replace("\n", "").Trim();
    }

    void IServiceImplementation.Reboot()
    {
        // Not Impemented on Windows
    }

    void IServiceImplementation.ShutdownApplication(int shutDownCode)
    {
        Environment.Exit(shutDownCode);
    }

    public double GetCPUTemp()
    {
        // NOTE: This means that this is not implemented
        return 0.0;
    }

    public Dictionary<string, string> GetPlatformVersions()
    {
        return new Dictionary<string, string>();
    }

    void IServiceImplementation.SetBootLogo(string logoPath)
    {
        // Not Impemented on Windows
        if (!File.Exists(logoPath))
            throw new FileNotFoundException("Logo Path does not Exist");

        string installPath = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(Ahsoka.Core.Hardware.InstallerPaths.BootLogoFileName);
        if (!Directory.Exists(Path.GetDirectoryName(installPath)))
            Directory.CreateDirectory(Path.GetDirectoryName(installPath));

        File.Copy(logoPath, installPath, true);

    }

}
