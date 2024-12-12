using Ahsoka.Installer.Components;
using Ahsoka.Services.Device;
using Ahsoka.Services.Install;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Ahsoka.Installer.InstallEngine;

internal class DeviceManagerPlugin : InstallerPlugin
{
    public override void AddAdditionalApplicationFiles(string buildLocation,
        Dictionary<string, string> attributes,
        Dictionary<string, byte[]> additionalFiles,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress)
    {
        DeviceList list = new();
        foreach (var item in info.HardwareConfiguration.HardwareItems)
        {
            list.Devices.Add(new DeviceInfo()
            {
                Id = (uint)HashCode32.GetHashCode32(item.Name),
                CustomStartup = item.CustomStartup,
                Name = item.Name,
                State = DeviceState.NoState,
                DeviceType = item.DeviceType
            });

        }
       
        var configJsonContents = JsonUtility.Serialize(list);
        var protobufferStream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(protobufferStream, list);

        additionalFiles[$"{ApplicationOutputConstants.AppInfoFolder}/DeviceConfiguration.pbuff"] = protobufferStream.ToArray();
        additionalFiles[$"{ApplicationOutputConstants.AppInfoFolder}/DeviceConfiguration.pbuff.json"] = Encoding.UTF8.GetBytes(configJsonContents);
    }
}