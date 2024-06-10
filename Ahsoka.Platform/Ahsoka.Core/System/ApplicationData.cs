using System.IO;
using System.Reflection;

namespace Ahsoka.System;

/// <summary>
/// Gets Paths for use storing Application Data
/// Provides for Local Data - Data which will go away after a reinstall
///              Persistent Data - Data which will survive a reinstall but can be removed with the App Reset 
///              Permanent Data - Data which will only be removed if the device is "Prepped" by a developer
/// </summary>
public class ApplicationDataPaths
{
    /// <summary>
    /// Returns a unique path for data related to your executable.
    /// This data is deleted on a new install
    /// </summary>
    /// <returns></returns>
    public static string GetLocalDataPath()
    {
        string dataPath = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetAhsokaPath("data"), "CurrentAppData");
        if (!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        return dataPath;
    }

    /// <summary>
    /// Returns a unique path for storing data related to your Executable.
    /// This Data will survive a new install.
    /// </summary>
    /// <returns></returns>
    public static string GetPersistentDataPath()
    {
        string dataPath = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetAhsokaPath("data"), "PersistentAppData");
        if (!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        return dataPath;
    }

    /// <summary>
    /// Returns a unique path for storing data related to your Executable.
    /// This Data will survive a factory restore (Installer UI) and only deleted by Prep
    /// </summary>
    /// <returns></returns>
    public static string GetPermanentDataPath()
    {
        string app = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
        string dataPath = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetAhsokaPath("data"), "PermanentAppData", app);
        if (!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        return dataPath;
    }

    /// <summary>
    /// Clears data related to your executable. both local and persistent
    /// Optionally can clear App application data or the Services Stored Data as well (Factory Reset)
    /// </summary>
    /// <returns></returns>
    public static void ClearAppData(bool clearAllAppData = false, bool clearAllData = false)
    {
        string dataPath = GetLocalDataPath();
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);

        dataPath = GetPersistentDataPath();
        if (Directory.Exists(dataPath))
            Directory.Delete(dataPath, true);


        // Clear Service Data Root
        // Used by Installer UI for Full Restore
        if (clearAllData)
        {
            string serviceData = SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(Hardware.RootPaths.ServiceDataRoot);
            if (Directory.Exists(serviceData))
                Directory.Delete(serviceData, true);

            // Clear Temp
            string tempData = SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(Hardware.RootPaths.TempRoot);
            if (Directory.Exists(tempData))
                Directory.Delete(tempData, true);

            // Clear Logs
            string logData = SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(Hardware.RootPaths.LogRoot);
            if (Directory.Exists(logData))
                Directory.Delete(logData, true);
        }
    }
}
