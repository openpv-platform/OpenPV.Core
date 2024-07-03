using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Install;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ahsoka.Installer.Components;

internal class EndInstallEngineComponent : InstallEngineComponent
{
    #region Properties
    public override bool IsRequiredComponent => true;

    public override Guid ComponentType => new("9E91F444-5A25-4AD2-9235-12437B5E736C");

    public override string ComponentName => "End Install";
    #endregion

    #region Methods

    protected override PackageComponent OnCreatePackageStream(string buildLocation,
        PackageInformation info,
         List<InstallerPlugin> installedPlugins,
        IProgress<PackageProgressInfo> progress = null)
    {
        return new PackageComponent()
        {
            ComponentType = ComponentType,
            Data = new MemoryStream(UTF8Encoding.UTF8.GetBytes(ComponentType.ToString())),
            Version = VersionUtility.GetAppVersionString()
        };
    }

    protected override void OnCleanup(string buildLocation,
        PackageComponent componentData,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress = null)
    {
        // No cleanup memory due to memory stream
    }

    protected override InstallResult OnInstallComponent(InstallContext context, PackageComponent item)
    {


        return InstallResult.Success;
    }
    #endregion
}
