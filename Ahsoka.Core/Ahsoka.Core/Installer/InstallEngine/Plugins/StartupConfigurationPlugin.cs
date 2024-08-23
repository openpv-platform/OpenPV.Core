using Ahsoka.Installer.Components;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ahsoka.Installer.InstallEngine;

internal class StartupInstallerPlugin : InstallerPlugin
{
    public const string AutoStartFileName = "AutoStartServiceConfig.pbuff";
    public const string OrientationKeyAttribute = "app_env_orientation";
    public const string OverrideWorkingFolder = "OverrideWorkingFolder";
    public const string OverrideApplicationName = "OverrideAppName";

    internal override void AddAdditionalApplicationFiles(string buildLocation, 
        Dictionary<string, string> attributes, 
        Dictionary<string, byte[]> additionalFiles, 
        PackageInformation info, 
        IProgress<PackageProgressInfo> progress)
    {
        if (attributes.TryGetValue(OverrideWorkingFolder, out string workingDirectory))
            attributes.Remove(OverrideWorkingFolder);
        else
            workingDirectory = HardwareInfo.GetHardwareInfo(info.PlatformFamily, info.PlatformQualifier).TargetPathInfo.GetRootPaths(RootPaths.ApplicationRoot);

        if (attributes.TryGetValue(OverrideApplicationName, out string execCommand))
            attributes.Remove(OverrideApplicationName);
        else
            execCommand = info.ExecCommand;

        string startScriptFileContents = Properties.Resources.Ahsoka_Application_Start;
        startScriptFileContents = startScriptFileContents.Replace("${ExecCommand}", execCommand);
        startScriptFileContents = startScriptFileContents.Replace("${ApplicationFolder}", workingDirectory);

        AddStartupCommands(buildLocation, additionalFiles, attributes, info, ref startScriptFileContents);
        
        additionalFiles[$"{ApplicationOutputComponent.ComponentTypeGuid}.{ApplicationOutputComponent.StartScriptName}"] = UTF8Encoding.UTF8.GetBytes(startScriptFileContents);

        var builder = AddEnvironmentValues(info);
        if (builder.Length > 0)
            additionalFiles[$"{ApplicationOutputComponent.ComponentTypeGuid}.{ApplicationOutputComponent.AppEnvironmentScriptName}"] = UTF8Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static StringBuilder AddEnvironmentValues(PackageInformation info)
    {
        StringBuilder builder = new();
       
        AddOrientationAttribute(builder, info);

        foreach (var variable in info.ServiceInfo.EnvironmentValues)
            builder.Append($"export {variable.VariableName}={variable.VariableValue}\n");
        return builder;
    }

    private void AddStartupCommands(string buildLocation, Dictionary<string, byte[]> additionalFiles, Dictionary<string, string> attributes, PackageInformation info, ref string startScriptFileContents)
    {
        StringBuilder preScript = new();
        StringBuilder postScript = new();

        // Add Startup Configuration
        string commandLineScriptName = AddRuntimeConfigToOutputs(buildLocation, additionalFiles, info);

        if (info.ServiceInfo.RuntimeConfiguration.ServiceStartup == StartOptions.BeforeApplicationStart)
        {
            additionalFiles[$"{ApplicationOutputComponent.AppInfoFolder}/AutoStartEnabled"] = new byte[0];
            preScript.Append($"\nchmod +x {commandLineScriptName}\n{commandLineScriptName} --StartServices {Path.GetDirectoryName(commandLineScriptName).Replace("\\", "/")}/{AutoStartFileName} & ");
        }
        else if (info.ServiceInfo.RuntimeConfiguration.ServiceStartup == StartOptions.AfterApplicationStart)
        {
            additionalFiles[$"{ApplicationOutputComponent.AppInfoFolder}/AutoStartEnabled"] = new byte[0];
            postScript.Append($"\nchmod +x {commandLineScriptName}\n{commandLineScriptName} --StartServices {Path.GetDirectoryName(commandLineScriptName).Replace("\\", "/")}/{AutoStartFileName} &");
        }

        // Release if Option Enabled (With Sleep if After to Allow Apps to Start)
        if (info.ServiceInfo.RuntimeConfiguration.ReleaseOption == StartOptions.BeforeApplicationStart)
            preScript.Append($"\n\nsystemd-notify --ready");
        else if (info.ServiceInfo.RuntimeConfiguration.ReleaseOption == StartOptions.AfterApplicationStart)
            postScript.Append($"\n\nsleep {info.ServiceInfo.RuntimeConfiguration.StartupDelay}\nsystemd-notify --ready"); // Add Delay for After Starts

        startScriptFileContents = startScriptFileContents.Replace("${PreReleaseScript}", preScript.ToString());
        startScriptFileContents = startScriptFileContents.Replace("${PostReleaseScript}", postScript.ToString());
    }

    private string AddRuntimeConfigToOutputs(string buildLocation, Dictionary<string, byte[]> additionalFiles, PackageInformation info)
    {
        // Create Path relative to Working Directory
        string pathRelativeToExecutable = $"CommandLine";

        // Create Target Path
        string targetCommandLinePath = HardwareInfo.GetHardwareInfo(info.PlatformFamily, info.PlatformQualifier).TargetPathInfo.GetRootPaths(RootPaths.ApplicationRoot);
        targetCommandLinePath = Path.Combine(targetCommandLinePath, pathRelativeToExecutable, "Ahsoka.CommandLine");

        // Remove Existing Command Line if it exists...we will use theirs
        string pathToCommandLine = Path.Combine(buildLocation, pathRelativeToExecutable);
        if (Directory.Exists(pathToCommandLine))
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "Removing Command Line, Package CommandLine will be used.");
            Directory.Delete(pathToCommandLine,true);

        }
        
        // Add Configuration File Use Custom or Default Depending on Setup.
        var stream = new MemoryStream();
        byte[] jsonData = null;
        ProtoBuf.Serializer.Serialize(stream, info.ServiceInfo.RuntimeConfiguration);
        jsonData = Encoding.UTF8.GetBytes( JsonUtility.Serialize(info.ServiceInfo.RuntimeConfiguration));

        additionalFiles[$"{ApplicationOutputComponent.AppInfoFolder}/{AutoStartFileName}"] = stream.ToArray(); // Place Copy Here as Indicator that we are using Auto Start.
        additionalFiles[$"{ApplicationOutputComponent.AppInfoFolder}/{AutoStartFileName + ".json"}"] = jsonData; // Place JSON so we can read it.

        if (info.ServiceInfo.RuntimeConfiguration.ServiceStartup != StartOptions.NoAutoStart || info.ApplicationType != ApplicationType.Dotnet)
            additionalFiles[pathRelativeToExecutable + $"/{AutoStartFileName}"] = stream.ToArray();

        return targetCommandLinePath.Replace("\\","/");
    }

    private static void AddOrientationAttribute(StringBuilder builder, PackageInformation info)
    {
        // App Orientation to App Keys
        string orientation = "0";
        if (info.SystemOrientation == Orientation.Rotate90)
            orientation = "90";
        if (info.SystemOrientation == Orientation.Rotate180)
            orientation = "180";
        if (info.SystemOrientation == Orientation.Rotate270)
            orientation = "270";

        builder.Append($"export {OrientationKeyAttribute.ToUpperInvariant()}={orientation}\n");
    }

}