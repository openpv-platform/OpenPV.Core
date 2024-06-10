using Ahsoka.Utility;
using System;
using System.Collections.Generic;
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

    private static void LoadHardwareInfo()
    {
        hardwareDefinitions = new Dictionary<string, HardwareInfo>();

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || (OperatingSystem.IsLinux() && Environment.Is64BitProcess))
        {
            var windows = JsonUtility.Deserialize<HardwareInfo>(Properties.Resources.WindowsHardwareConfiguration);
            hardwareDefinitions.Add(windows.FactoryInfo.PlatformDescription, windows);

            var ubuntu = JsonUtility.Deserialize<HardwareInfo>(Properties.Resources.UbuntuHardwareConfiguration);
            hardwareDefinitions.Add(ubuntu.FactoryInfo.PlatformDescription, ubuntu);

            var macos = JsonUtility.Deserialize<HardwareInfo>(Properties.Resources.MacOSHardwareConfiguration);
            hardwareDefinitions.Add(macos.FactoryInfo.PlatformDescription, macos);
        }
        else { return; } // Running on a target...Don't Load Anything.

        var openView = JsonUtility.Deserialize<HardwareInfo>(Properties.Resources.OpenViewS50HardwareConfiguration);
        hardwareDefinitions.Add(openView.FactoryInfo.PlatformDescription, openView);

        openView = JsonUtility.Deserialize<HardwareInfo>(Properties.Resources.OpenViewS70HardwareConfiguration);
        hardwareDefinitions.Add(openView.FactoryInfo.PlatformDescription, openView);

        openView = JsonUtility.Deserialize<HardwareInfo>(Properties.Resources.OpenViewST4HardwareConfiguration);
        hardwareDefinitions.Add(openView.FactoryInfo.PlatformDescription, openView);

        openView = JsonUtility.Deserialize<HardwareInfo>(Properties.Resources.OpenViewST4PlusHardwareConfiguration);
        hardwareDefinitions.Add(openView.FactoryInfo.PlatformDescription, openView);

        openView = JsonUtility.Deserialize<HardwareInfo>(Properties.Resources.PowerVisionDisplayHardwareConfiguration);
        hardwareDefinitions.Add(openView.FactoryInfo.PlatformDescription, openView);

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
