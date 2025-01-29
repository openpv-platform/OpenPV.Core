using Ahsoka.Installer.Components;
using Ahsoka.Services.Install;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ahsoka.Installer.InstallEngine;

/// <summary>
/// Base Class for Adding Service Configuration Files
/// </summary>
/// <typeparam name="T"></typeparam>
public class ConfigurationPluginBase<T> : InstallerPlugin where T : class, new()
{
    private string extensionName;
    private string configurationFileName;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="extensionName"></param>
    /// <param name="configurationFileName"></param>
    protected ConfigurationPluginBase(string extensionName, string configurationFileName)
    {
        this.extensionName = extensionName;
        this.configurationFileName = configurationFileName;
    }

    /// <summary>
    /// Adds Configuration Files to the .AppInfo folder on Generate
    /// </summary>
    /// <param name="buildLocation"></param>
    /// <param name="attributes"></param>
    /// <param name="additionalFiles"></param>
    /// <param name="info"></param>
    /// <param name="progress"></param>
    /// <exception cref="Exception"></exception>
    public override void AddAdditionalApplicationFiles(string buildLocation,
        Dictionary<string, string> attributes,
        Dictionary<string, byte[]> additionalFiles,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress)
    {
        var extensionInfo = info.ServiceInfo.RuntimeConfiguration.ExtensionInfo.FirstOrDefault(x => x.ExtensionName.Equals(extensionName));
        if (extensionInfo == null && extensionInfo.ConfigurationFile == null)
            throw new Exception($"{extensionName} Not Configured.");

        var configJsonContents = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(info.GetPackageInfoPath()), extensionInfo.ConfigurationFile));
        T configObject = JsonUtility.Deserialize<T>(configJsonContents.ToString());
        var protobufferStream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(protobufferStream, configObject);

        additionalFiles[$"{ApplicationOutputConstants.AppInfoFolder}/{configurationFileName}"] = protobufferStream.ToArray();
        additionalFiles[$"{ApplicationOutputConstants.AppInfoFolder}/{configurationFileName}.json"] = configJsonContents;
    }
}
