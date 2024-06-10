using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Install;
using Ahsoka.Services.Network;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ahsoka.Core.Bluetooth.LE")]
[assembly: InternalsVisibleTo("Ahsoka.Core.Bluetooth")]

namespace Ahsoka.Installer.Components;

internal class NetworkFirmwareComponent : InstallEngineComponent
{
    public const string PackageName = "NetworkFirmware";
    public static readonly Guid NetworkFirmwareComponentType = new("A76E50EA-0A8F-4341-A0A2-BA7BC76DE299");

    public override Guid ComponentType => NetworkFirmwareComponentType;

    public override bool IsRequiredComponent => false;

    public override string ComponentName => PackageName;

    protected override PackageComponent OnCreatePackageStream(string buildLocation,
        PackageInformation info,
        List<InstallerPlugin> installedPlugins,
        IProgress<PackageProgressInfo> progress = null)
    {
        var hardware = HardwareInfo.GetHardwareInfo(info.PlatformFamily, info.PlatformQualifier);
        var networkFirmware = GetPlatformFirmware(hardware);
        if (networkFirmware == null)
            return null;

        byte[] data = networkFirmware.GetData(info.PlatformFamily);
        if (data == null)
            return null;

        PackageComponent component = new()
        {
            ComponentType = NetworkFirmwareComponentType,
            Data = new MemoryStream(data),
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
            string pathToFirmware = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(pathToFirmware);
            using (var fs = new FileStream(Path.Combine(pathToFirmware, "firmware.zip"), FileMode.OpenOrCreate))
                context.InstallEngine.ExtractPackage(this.ComponentType, fs, context.Progress);

            var package = context.InstallEngine.PackageContents;
            var hardware = SystemInfo.HardwareInfo; // Pull Installed System Hardware Info.
            var networkFirmware = GetPlatformFirmware(hardware);
            networkFirmware?.InstallLocal(pathToFirmware);
            context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Installed Network Firmware Successfully" });
        }
        catch (Exception ex)
        {
            context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = ex.Message + ex.StackTrace });
            return InstallResult.ExceptionThrownDuringInstall;
        }

        return InstallResult.Success;
    }

    public static INetworkFirmwareBase GetPlatformFirmware(HardwareInfo info)
    {
        if (info.NetworkInfo.AdapterInfoList.Count == 0)
            return null;

        var module = info.NetworkInfo.AdapterInfoList[0].Module;
        if (module == ModuleType.Esp32)
            return new ESPNetworkFirmware();
        else
            return null;
    }
}
