using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ahsoka.Installer.Components;

internal class StartLogoComponent : InstallEngineComponent
{
    public const string PackageName = "StartLogo";
    public static readonly Guid StartLogoComponentType = new("34F8A5E2-3F2A-40D5-98E4-8122B58291BE");

    public override Guid ComponentType => StartLogoComponentType;

    public override bool IsRequiredComponent => false;

    public override string ComponentName => PackageName;

    protected override PackageComponent OnCreatePackageStream(string buildLocation,
       PackageInformation info,
       List<InstallerPlugin> installedPlugins,
       IProgress<PackageProgressInfo> progress = null)
    {
        if (info.StartupLogoPath == null)
            return null;

        string logoPath = Path.GetFullPath(info.StartupLogoPath, Path.GetDirectoryName(info.GetPackageInfoPath()));
        if (!File.Exists(logoPath))
            return null;

        var data = new MemoryStream(File.ReadAllBytes(logoPath));
        if (info.SystemOrientation != Orientation.DefaultOrientation)
        {
            var image = Image.Load(data);
            if (info.SystemOrientation == Orientation.Rotate180)
                image.Mutate(x => x.Rotate(180));
            else if (info.SystemOrientation == Orientation.Rotate270)
                image.Mutate(x => x.Rotate(270));
            else if (info.SystemOrientation == Orientation.Rotate90)
                image.Mutate(x => x.Rotate(90));

            data.Dispose();
            data = new MemoryStream();
            image.SaveAsPng(data);
            data.Seek(0, SeekOrigin.Begin);
        }

        var component = new PackageComponent()
        {
            ComponentType = StartLogoComponentType,
            Data = data,
            Version = VersionUtility.GetAppVersionString()
        };

        return component;

    }

    protected override void OnCleanup(string buildLocation, PackageComponent componentData, PackageInformation info, IProgress<PackageProgressInfo> progress = null)
    {

    }

    protected override InstallResult OnInstallComponent(InstallContext context, PackageComponent item)
    {
        try
        {
            string pathToLogo = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.BootLogoFileName);
            string pathToLogoFolder = Path.GetDirectoryName(pathToLogo);
            if (!Directory.Exists(pathToLogoFolder))
                Directory.CreateDirectory(pathToLogoFolder);

            using (var fs = new FileStream(pathToLogo, FileMode.OpenOrCreate))
                context.InstallEngine.ExtractPackage(this.ComponentType, fs, context.Progress);

            if (File.Exists(pathToLogo))
                context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Installed Boot Logo at {pathToLogo} {File.Exists(pathToLogo)}" });
            else
            {
                context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Failed to Install Logo at {pathToLogo}" });
                return InstallResult.UnknownError;
            }
        }
        catch (Exception ex)
        {
            context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = ex.Message + ex.StackTrace });
            return InstallResult.ExceptionThrownDuringInstall;
        }

        return InstallResult.Success;
    }
}
