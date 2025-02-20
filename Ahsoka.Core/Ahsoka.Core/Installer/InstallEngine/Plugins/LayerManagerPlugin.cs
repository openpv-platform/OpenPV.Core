using Ahsoka.Core;
using Ahsoka.Core.Utility;
using Ahsoka.Installer.Components;
using Ahsoka.Services.Install;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ahsoka.Installer.InstallEngine;

internal class LayerManagerPlugin : InstallerPlugin
{

    public override void AddAdditionalApplicationFiles(string buildLocation,
        Dictionary<string, string> attributes,
        Dictionary<string, byte[]> additionalFiles,
        PackageInformation info,
        IProgress<PackageProgressInfo> progress)
    {

        if (info.PlatformFamily == PlatformFamily.OpenViewLinuxPro)
        {
            var configJsonContents = JsonUtility.Serialize(info.ScreenConfiguration);
            var protobufferStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(protobufferStream, info.ScreenConfiguration);

            additionalFiles[$"{ApplicationOutputConstants.AppInfoFolder}/SurfaceConfiguration.pbuff"] = protobufferStream.ToArray();
            additionalFiles[$"{ApplicationOutputConstants.AppInfoFolder}/SurfaceConfiguration.pbuff.json"] = Encoding.UTF8.GetBytes(configJsonContents);
        }
    }
}