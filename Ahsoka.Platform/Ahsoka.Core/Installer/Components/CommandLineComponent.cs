using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ahsoka.Installer.Components;

internal class CommandLineComponent : PackageComponentBase
{
    public const string InstallerFileNameAttribute = "installerFileName";
    public const string InstallerArgsAttribute = "installerArgs";
    public const string PackageName = "CommandLine";

    public static readonly Guid CommandLineComponentType = new("46AED61D-9818-4769-AFBD-61C3782DEF35");

    public override Guid ComponentType => CommandLineComponentType;

    public override bool IsRequiredComponent => true;

    public override string ComponentName => PackageName;

    protected override PackageComponent OnCreateFilePackageStream(string buildLocation,
        PackageInformation info,
        Dictionary<string, byte[]> additionalFiles,
        List<InstallerPlugin> installerPlugins,
        IProgress<PackageProgressInfo> progress)

    {
        // Add Extensions
        foreach(var item in info.ServiceInfo.RuntimeConfiguration.Extensions)
            Extensions.InstallExtension(additionalFiles, item);

        var componentData = base.OnCreateFilePackageStream(buildLocation, info, additionalFiles, installerPlugins, progress);
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
