using Ahsoka.System.Hardware;
using System.IO;

namespace Ahsoka.DeveloperTools.ViewModel;

/// <summary>
/// Get Paths for Customer Tools
/// </summary>
public static class CustomerToolPaths
{
    static readonly string settingsFolder;
    static string toolsPath;

    static CustomerToolPaths()
    {
        settingsFolder = PlatformSupportPathInfo.GetDeveloperToolRootPath();

        toolsPath = PlatformSupportPathInfo.GetDeveloperVersionRootPath();

        if (!Directory.Exists(settingsFolder))
            Directory.CreateDirectory(settingsFolder);
    }

    /// <summary>
    /// Gets Path to Tools
    /// </summary>
    /// <param name="toolDirectory"></param>
    /// <returns></returns>
    public static string GetToolPath(string toolDirectory)
    {
        return Path.Combine(toolsPath, toolDirectory);
    }

    /// <summary>
    /// Get Paths for Settings
    /// </summary>
    /// <param name="settingFileName"></param>
    /// <returns></returns>
    public static string GetSettingsPath(string settingFileName)
    {
        return Path.Combine(settingsFolder, settingFileName);
    }

}
