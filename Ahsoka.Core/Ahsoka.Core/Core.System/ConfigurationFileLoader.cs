using Ahsoka.Core.Utility;
using System.Text.Json.Serialization.Metadata;

namespace Ahsoka.Core;

// Singleton Class used to Load All Ahsoka
// configuration files ensuring any upgrades 
// are run

/// <summary>
/// Singleton Class used to Load All Ahsoka
/// configuration files ensuring any upgrades 
/// are run
/// </summary>
public class ConfigurationFileLoader
{
    /// <summary>
    /// Loads a Configuration File executing any Upgrades needed.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="useTypeResolver"></param>
    /// <returns></returns>
    public static T LoadFile<T>(string path, bool useTypeResolver = true) where T : class, new()
    {
        string data = UpgradeEngine.ExecuteUpgrade(path);
        if (useTypeResolver)
            return JsonUtility.DeserializeWithTypeInfo<T>(data);
        else
            return JsonUtility.Deserialize<T>(data);
    }

    /// <summary>
    /// Open a File from raw string data with the known file type...aka file extension
    /// </summary>
    /// <param name="fileType">File Extension or File Name</param>
    /// <param name="data">data</param>
    /// <param name="useTypeResolver"></param>
    /// <returns></returns>
    public static T LoadFileContents<T>(string fileType, string data, bool useTypeResolver = true) where T : class, new()
    {
        string contents = UpgradeEngine.ExecuteUpgrade(fileType, data);
        if (useTypeResolver)
            return JsonUtility.DeserializeWithTypeInfo<T>(contents);
        else
            return JsonUtility.Deserialize<T>(contents);
    }
}
