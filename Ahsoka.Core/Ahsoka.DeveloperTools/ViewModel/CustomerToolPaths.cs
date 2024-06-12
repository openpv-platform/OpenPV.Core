using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.IO;

namespace Ahsoka.DeveloperTools.ViewModel;

internal static class CustomerToolPaths
{
    static readonly string settingsFolder;
    static string toolsPath;

    static CustomerToolPaths()
    {
        if (OperatingSystem.IsMacOS())
            settingsFolder = Path.Combine(Environment.GetEnvironmentVariable("HOME"), "Library", "Application Support", "AhsokaData");
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
            settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".AhsokaData");
        else
            settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AhsokaData");

        var version = VersionUtility.GetAppVersion();
        toolsPath = Path.Combine(settingsFolder, $"{version.Major}.0");

        if (!Directory.Exists(settingsFolder))
            Directory.CreateDirectory(settingsFolder);
    }

    public static string GetToolPath(string toolDirectory)
    {
        return Path.Combine(toolsPath, toolDirectory);
    }

    public static string GetSettingsPath(string settingFileName)
    {
        return Path.Combine(settingsFolder, settingFileName);
    }

}
