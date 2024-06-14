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
        settingsFolder = PlatformSupportPathInfo.GetDeveloperToolPath();

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
