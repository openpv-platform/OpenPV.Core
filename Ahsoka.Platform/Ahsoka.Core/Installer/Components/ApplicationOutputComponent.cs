using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Install;
using Ahsoka.Services.Network;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Mono.Unix;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Ahsoka.Installer.Components;

internal class ApplicationOutputComponent : FileSystemComponentBase
{
    #region Fields
    public const string AppInfoFolder = ".appinfo";
    public const string CoProcessorFolder = ".coprocessor";
    public const string AppNameFile = "AppName.txt";
    public const string AppVersionFile = "AppVersion.txt";
    public const string AppTypeFile = "AppType.txt";
    public const string AppDescriptionFile = "AppDescription.txt";
    public const string ComponentTypeGuid = "333C0904-F3D6-4381-B9E4-B230A21CB464";
    const string ApplicationTypeKeyAttribute = "applicationType";
    const string ExecCommandKeyAttribute = "execCommand";
    const string RequiredLicenseAttribute = "requiredLicense_";

    public const string StartScriptName = "start.sh";
    public const string AppEnvironmentScriptName = "app_env.sh";
    public const string UIHelperScriptName = "start_ui_helper.sh";
    #endregion

    #region Properties
    public override Guid ComponentType => new(ComponentTypeGuid);

    public override bool IsRequiredComponent => true;

    public override bool FilterPlatformFolder => false;

    public override string ComponentName => "Application";
    #endregion

    #region Methods
    protected override string GetTargetPath()
    {
        return SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.ApplicationRoot);
    }

    protected override PackageComponent OnCreateFilePackageStream(string buildLocation, 
        PackageInformation info, 
        Dictionary<string, byte[]> additionalFiles, 
        List<InstallerPlugin> installerPlugins, 
        IProgress<PackageProgressInfo> progress)
    {


        // Add Version Info to .appinfo Folder
        if (info.ApplicationVersion != null)
            additionalFiles[$"{AppInfoFolder}/{AppVersionFile}"] = Encoding.UTF8.GetBytes(info.ApplicationVersion);

        if (info.PackageDescription != null)
            additionalFiles[$"{AppInfoFolder}/{AppDescriptionFile}"] = Encoding.UTF8.GetBytes(info.PackageDescription);

        additionalFiles[$"{AppInfoFolder}/{AppNameFile}"] = Encoding.UTF8.GetBytes(info.PackageName);
        additionalFiles[$"{AppInfoFolder}/{AppTypeFile}"] = Encoding.UTF8.GetBytes(info.ApplicationType.ToString());
      
        // Attributes to add to the Install Package
        Dictionary<string, string> attributes = new()
        {
            // Capture Common Attributes (App Type and Original Execute Command
            { ApplicationTypeKeyAttribute, info.ApplicationType.ToString() },
            { ExecCommandKeyAttribute, info.ExecCommand }
        };

        foreach(var license in info.Licenses)
            attributes[RequiredLicenseAttribute + license.LicenseName] = license.LicenseId.ToString();

        // Run Plugins To allow them to Prepare App Folder (or replace it.
        foreach (var plugin in installerPlugins)
            plugin.PrepareApplicationArea(ref buildLocation, info, progress);

        // Add any additional files to the Install Package
        foreach (var plugin in installerPlugins)
            plugin.AddAdditionalApplicationFiles(buildLocation, attributes, additionalFiles, info, progress);

        // Add Component Attributes
        var componentData = base.OnCreateFilePackageStream(buildLocation, info, additionalFiles, installerPlugins, progress);
        if (componentData != null)
        {
            foreach (var item in attributes)
                componentData.Attributes[item.Key] = item.Value;
        }

        // Run Plugins To allow them to Prepare App Folder (or replace it.
        foreach (var plugin in installerPlugins)
            plugin.CleanupApplication(buildLocation, info, progress);

        return componentData;
    }

    protected override InstallResult OnInstallComponent(InstallContext context, PackageComponent item)
    {
        var valid = ValidateLicenses(context, item);
        if (!valid)
            context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"One or more licenses were not valid." });
        
        var result = base.OnInstallComponent(context, item);

        if (OperatingSystem.IsLinux())
        {
            // Install ahsoka-early.target for Ahsoka.Application.service
            string targetFileContents = Properties.Resources.ahsoka_early;
            string pathToSystemTargetFile = LinuxPathInfo.CreateSystemManagerPath("system/ahsoka-early.target");

            File.WriteAllText(pathToSystemTargetFile, targetFileContents);
            context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Installed target file for {pathToSystemTargetFile}" });

            // Install Ahsoka.Application.service
            string unitFileContents = Properties.Resources.Ahsoka_Application;
            string pathToSystemUnitFile = LinuxPathInfo.CreateSystemManagerPath("system/Ahsoka.Application.service");

            File.WriteAllText(pathToSystemUnitFile, unitFileContents);
            context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Installed unit file for {pathToSystemUnitFile}" });

            // Reload systemd after unit file installed or updated
            var output = Process.Start("systemctl", "daemon-reload");
            output.WaitForExit();
            if (output.ExitCode != 0)
                context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Failed to reload systemd for {pathToSystemUnitFile}" });

            // Extract Start Script, Environment Script and UI Helper if Needed.
            AddScript(context, StartScriptName);
            AddScript(context, AppEnvironmentScriptName);
            AddScript(context, UIHelperScriptName);
   
            // Get the Applicatoin Type and Command / Name and pass it to the Plugin so it can make any 
            // modifications to the system specific to this application type.
            ApplicationType projectType = Enum.Parse<ApplicationType>(item.Attributes[ApplicationTypeKeyAttribute]);
            string appCommand = item.Attributes[ExecCommandKeyAttribute];
            string workingDirectory = SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.ApplicationRoot);

            // Run Installer Plugin to handle 
            InstallerPlugin appPlugin = InstallerPlugin.GetApplicationPlugin(projectType);
            appPlugin?.ModifyTargetInstall(workingDirectory, appCommand);

            AddInstaller(context, item);

            // Enable the Services for Application Startup.
            output = Process.Start("systemctl", "disable Ahsoka.Installer.service");
            output.WaitForExit();
            if (output.ExitCode != 0)
                context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Failed to disable Ahsoka.Installer.service" });

            output = Process.Start("systemctl", "enable Ahsoka.Application.service");
            output.WaitForExit();
            if (output.ExitCode != 0)
                context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Failed to enable Ahsoka.Application.service" });
        }


        void AddInstaller(InstallContext context, PackageComponent item)
        {
            string workingDirectory = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.ApplicationRoot),"CommandLine");

            string tempFile = Path.GetTempFileName();
            using (FileStream installer = new(tempFile, FileMode.Create))
                context.InstallEngine.ExtractPackage(CommandLineComponent.CommandLineComponentType, installer, context.Progress);

            context.Log?.Report(new InstallLogInfo()
            {
                LogMessage = $"Inflating CommandLine to {workingDirectory}"
            });

            // Extract Zip to Output Area
            PackagingUtility.ExtractPackage(tempFile, workingDirectory, context.Progress, context.Log);

            // Remove Temp File
            File.Delete(tempFile);
        }
        
        return result;
    }

    private bool ValidateLicenses(InstallContext context, PackageComponent item)
    {
        bool licensesValid = true;
        context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Validating Licenses" });

        foreach (var attributeValue in item.Attributes)
        {
            if (attributeValue.Key.ToLower().StartsWith(RequiredLicenseAttribute.ToLower()))
            {
                string licenseKeyName = attributeValue.Key.Replace(RequiredLicenseAttribute, "");
                context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Validating License for Module {licenseKeyName}" });
                if (!ModuleLicenseManager.IsLicensed(attributeValue.Value))
                {
                    licensesValid = false;
                    context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Failed to find License for Module {licenseKeyName} on Target but may be available in Spark" });
                }
            }
        }
        return licensesValid;
    }

    private void AddScript(InstallContext context, string fileToCopy)
    {
        // Path to Script on Target
        string pathToScriptFile = $"{SystemInfo.HardwareInfo.FactoryInfo.RootPath}/{fileToCopy}";
       
        // Extract Start Script from Package
        string pathToPackagesStartScript = $"{SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.ApplicationRoot)}/{ComponentType.ToString().ToUpperInvariant()}.{fileToCopy}";
        if (File.Exists(pathToPackagesStartScript))
        {
            string startScriptFileContents = File.ReadAllText(pathToPackagesStartScript);

            // Write Script to Service Start Location
            File.WriteAllText(pathToScriptFile, startScriptFileContents);

            // Set Permissions.
            new UnixFileInfo(pathToScriptFile).FileAccessPermissions |= FileAccessPermissions.UserExecute;
            context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Installed Application script at {pathToScriptFile}" });

            // Delete Original Script After Move
            File.Delete(pathToPackagesStartScript);
        }
        else
        {
            // Report Error and Delete Any Script
            context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Application Script Not Found at {pathToPackagesStartScript}, Clearing Existing Script" });
            if (File.Exists(pathToScriptFile))
                File.Delete(pathToScriptFile);
        }
    }

    #endregion
}
