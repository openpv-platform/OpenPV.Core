using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using Extensions = Ahsoka.System.Extensions;

namespace Ahsoka.Installer.Components;

internal class CommandLineComponent : PackageComponentBase
{
    string overridePath = null;
    public const string InstallerFileNameAttribute = "installerFileName";
    public const string InstallerArgsAttribute = "installerArgs";
    public const string PackageName = "CommandLine";

    public static readonly Guid CommandLineComponentType = new("46AED61D-9818-4769-AFBD-61C3782DEF35");

    public override Guid ComponentType => CommandLineComponentType;

    public override string ComponentName => PackageName;

    protected override string GetPackagingPath(string buildLocation, PackageInformation info)
    {
        if (overridePath != null) 
            return overridePath;

        return base.GetPackagingPath(buildLocation, info);
    }

    protected override PackageComponent OnCreateFilePackageStream(string buildLocation,
        PackageInformation info,
        Dictionary<string, byte[]> additionalFiles,
        List<InstallerPlugin> installerPlugins,
        IProgress<PackageProgressInfo> progress)
    {
        PackageComponent componentData = null;

        if (info.OptimizedServiceManager && info.CanUseOptimizerdServiceManager())
        {
            progress.Report(new PackageProgressInfo() { Message = " " });
            progress.Report(new PackageProgressInfo() { Message = "      Generating Optimized Service Manager" });
            progress.Report(new PackageProgressInfo() { Message = " " });

            // Path to Output
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            overridePath = Path.Combine(path, "publish");
           
            IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
            {
                string result = info.Data?.TrimStart();
                if (!string.IsNullOrEmpty(result))
                    progress.Report(new PackageProgressInfo() { Message = "      " + info.Data.TrimStart() });
            });

            Extensions.CreateAhsokaCommandLine(info, progressText, path);

            componentData = base.OnCreateFilePackageStream(buildLocation, info, additionalFiles, installerPlugins, progress);

            progress.Report(new PackageProgressInfo() { Message = " " });

            Directory.Delete(path, true);
        }
        else
        {
            if (info.OptimizedServiceManager)
                progress.Report(new PackageProgressInfo() { Message = "         Compiler not found, using Dynamic Extensions, install ServiceCompiler if you wish to use this feature" });

            // Add Extensions to App Folder
            foreach (var item in info.ServiceInfo.RuntimeConfiguration.ExtensionInfo)
                Extensions.InstallExtension(additionalFiles, item.ExtensionName);

            componentData = base.OnCreateFilePackageStream(buildLocation, info, additionalFiles, installerPlugins, progress);
        }

        if (componentData != null)
        {
            if (info.PlatformFamily == PlatformFamily.Windows64)
                componentData.Attributes.Add(InstallerFileNameAttribute, "Ahsoka.CommandLine.exe");
            else
                componentData.Attributes.Add(InstallerFileNameAttribute, "Ahsoka.CommandLine");

            componentData.Attributes.Add(InstallerArgsAttribute, "--StartInstallService");
        }
        return componentData;
    }

    protected override string GetTargetPath()
    {
        return String.Empty;
    }
}
