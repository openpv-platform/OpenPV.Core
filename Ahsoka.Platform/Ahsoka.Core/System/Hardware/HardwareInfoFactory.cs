using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ahsoka.System.Hardware;


public partial class HardwareInfo
{
    private static Dictionary<string, HardwareInfo> hardwareDefinitions;

    // Default to In Process "Configurationless" Configuration
    static HardwareInfo()
    {
        LoadHardwareInfo();
    }
   
    internal static void LoadHardwareInfo(string fallbackPath = null)
    {
        hardwareDefinitions = new Dictionary<string, HardwareInfo>();

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || (OperatingSystem.IsLinux() && Environment.OSVersion.VersionString.Contains("Ubuntu")))
        {
            // Load Files in Hardware Folders
            string path = Path.Exists(PlatformSupportPathInfo.PlatformSupportRootFolder) ? PlatformSupportPathInfo.PlatformSupportRootFolder : fallbackPath;
            if (Directory.Exists(path))
            {
                foreach (var pathToAppSupport in Directory.GetDirectories(path))
                {
                    string hwPath = Path.Combine(pathToAppSupport, "Hardware");
                    if (Directory.Exists(hwPath))
                    {
                        foreach (var hardwareDef in Directory.EnumerateFiles(hwPath, "*HardwareConfiguration.json"))
                        {
                            try
                            {
                                string fileText = File.ReadAllText(hardwareDef);
                                var hd = JsonUtility.Deserialize<HardwareInfo>(fileText);
                                hardwareDefinitions.Add(hd.FactoryInfo.PlatformDescription, hd);
                            }
                            catch { Console.WriteLine($"Failed to load HardwareConfiguration at {hardwareDef}"); }
                        }
                    }
                }
            }
            else { return; } // Running on a target...Don't Load Anything.
        }

    }

    /// <summary>
    /// Gets the Hardware Information from the Supported Hardware List by Family / Qualifier
    /// </summary>
    /// <param name="family">Platform Family for the Platform</param>
    /// <param name="qualifier">Platform Qualifier for the Variant</param>
    /// <returns>Hardware Information</returns>
    public static HardwareInfo GetHardwareInfo(PlatformFamily family, string qualifier)
    {
        return hardwareDefinitions.FirstOrDefault(x => x.Value.PlatformFamily == family && x.Value.PlatformQualifier == qualifier).Value;
    }

    /// <summary>
    /// Gets the Hardware Information from the Supported Hardware List by Harware Description
    /// </summary>
    /// <param name="value"></param>
    /// <returns>Hardware Information</returns>
    public static HardwareInfo GetHardwareInfoByDescription(string value)
    {
        value = value ?? "";

        if (hardwareDefinitions.TryGetValue(value, out var hardwareInfo))
            return hardwareInfo;
        else
            return null;
    }

    /// <summary>
    /// Get a list of Hardware Information Descriptions for use in Drop Downs
    /// </summary>
    /// <returns>Hardware Information</returns>
    public static List<string> GetHardwareInfoDescriptions()
    {
        return hardwareDefinitions.Keys.ToList();
    }
}
