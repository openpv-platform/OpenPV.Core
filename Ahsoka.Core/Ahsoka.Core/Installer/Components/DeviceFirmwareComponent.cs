using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Device;
using Ahsoka.Services.Device.Handlers;
using Ahsoka.Services.Install;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;


namespace Ahsoka.Installer.Components;

internal class DeviceFirmwareComponent : InstallEngineComponent
{
    public const string PackageName = "DeviceFirmware";
    public static readonly Guid DeviceFirmwareComponentType = new("A76E50EA-0A8F-4341-A0A2-BA7BC76DE299");

    public override Guid ComponentType => DeviceFirmwareComponentType;

    public override string ComponentName => PackageName;

    protected override PackageComponent OnCreatePackageStream(string buildLocation,
        PackageInformation info,
        List<InstallerPlugin> installedPlugins,
        IProgress<PackageProgressInfo> progress = null)
    {
        Dictionary<DeviceTypes, byte[]> firmware = new();

        // Create Firmware Package
        foreach (var item in info.HardwareConfiguration.HardwareItems)
        {
            var handler = DeviceHandlerBase.GetDeviceHandler(item.DeviceType);

            if (handler != null && handler.SupportsFirmware)
            {
                byte[] networkFirmware = handler.GetFirmwareData(info.PlatformFamily, info.PlatformQualifier);
                if (networkFirmware != null)
                    firmware[item.DeviceType] = networkFirmware;
            }
        }

        if (firmware.Count > 0)
        {
            // Create Zip File of all Firmware
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var item in firmware)
                {
                    var firmwareFile = archive.CreateEntry(item.Key.ToString());
                    using (var entryStream = firmwareFile.Open())
                        entryStream.Write(item.Value);
                }
            }

            // Return Zip Stream
            return new()
            {
                ComponentType = DeviceFirmwareComponentType,
                Data = new MemoryStream(memoryStream.ToArray()),
                Version = VersionUtility.GetAppVersionString()
            };
        }

        return null;
    }

    protected override void OnCleanup(string buildLocation, PackageComponent componentData, PackageInformation info, IProgress<PackageProgressInfo> progress = null)
    {

    }

    protected override InstallResult OnInstallComponent(InstallContext context, PackageComponent item)
    {
        try
        {
            string pathToFirmware = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string pathToZip = Path.Combine(pathToFirmware, "deviceFirmware.zip");
            Directory.CreateDirectory(pathToFirmware);
            using (var fs = new FileStream(pathToZip, FileMode.OpenOrCreate))
                context.InstallEngine.ExtractPackage(this.ComponentType, fs, context.Progress);

            // Extract Zip to Output Area
            PackagingUtility.ExtractPackage(pathToZip, pathToFirmware, context.Progress, context.Log);

            // Delete Firmware File
            File.Delete(pathToZip);

            foreach (var fwFile in Directory.GetFiles(pathToFirmware))
            {
                // Look for Installer / Handler
                if (Enum.TryParse(Path.GetFileName(fwFile), true, out DeviceTypes deviceType))
                {
                    context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Firmware Installer Found for {deviceType}" });
                    var handler = DeviceHandlerBase.GetDeviceHandler(deviceType);
                    if (handler != null)
                        handler.InstallLocal(fwFile, context.Log);
                }
            }

            Directory.Delete(pathToFirmware, true);

            context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Installed Device Firmware Successfully" });
        }
        catch (Exception ex)
        {
            context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = ex.Message + ex.StackTrace });
            return InstallResult.ExceptionThrownDuringInstall;
        }

        return InstallResult.Success;
    }
}

