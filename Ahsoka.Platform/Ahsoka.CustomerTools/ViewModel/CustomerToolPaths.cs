using Ahsoka.Utility;
using System;
using System.IO;

namespace Ahsoka.CustomerTools.ViewModel;
internal static class CustomerToolPaths
{
    static readonly string settingsFolder;
    static readonly string toolPath;

    public static string LocalDataPath
    {
        get;
        private set;
    }

    static CustomerToolPaths()
    {
        if (OperatingSystem.IsMacOS())
            settingsFolder = Path.Combine(Environment.GetEnvironmentVariable("HOME"), "Library", "Application Support", "AhsokaData");
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
            settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".AhsokaData");
        else
            settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AhsokaData");

        var version = VersionUtility.GetAppVersion();
        toolPath = Path.Combine(settingsFolder, $"{version.Major}.0");

        if (!Directory.Exists(settingsFolder))
            Directory.CreateDirectory(settingsFolder);
    }

    public static string GetToolPath(string toolDirectory)
    {
        return Path.Combine(toolPath, toolDirectory);
    }

    public static string GetSettingsPath(string settingFileName)
    {
        return Path.Combine(settingsFolder, settingFileName);
    }

}
