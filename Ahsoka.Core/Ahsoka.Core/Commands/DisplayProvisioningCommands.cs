using Ahsoka.Core.Hardware;
using Ahsoka.Core.Spark;
using Ahsoka.Core.Utility;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Ahsoka.Core.Commands;

[ExcludeFromCodeCoverage]
[CommandLinePlugin]
internal static class DisplayProvisioningCommands
{
    [CommandLineMethod(@"     --ProvisionDisplay: License the Display For the Specified Modules
            [TargetConnectionInfoFile] - Connection Info File (JSON)
            [SparkConnectionInfoFile] - Spark Connection Info File (JSON)
            [ProvisioningFile] - Provisioning File with OTA and License Info (JSON).")]
    private static void ProvisionDisplay(string targetConnectionInfoFile,
            string sparkConnectionInfoFile,
            string provisioningFile)
    {
        IProgress<string> progress = new Progress<string>(info =>
        {
            Console.WriteLine(info);
        });

        var connectionInfo = JsonUtility.DeserializeWithTypeInfo<TargetConnectionInfo>(File.ReadAllText(targetConnectionInfoFile));
        var sparkInfo = JsonUtility.DeserializeWithTypeInfo<SparkConnectionInfo>(File.ReadAllText(sparkConnectionInfoFile));
        var provisionInfo = JsonUtility.DeserializeWithTypeInfo<ProvisionDisplayRequest>(File.ReadAllText(provisioningFile));

        if (!SparkCustomerTools.ProvisionDisplayInSpark(connectionInfo, sparkInfo, provisionInfo, progress))
        {
            Console.WriteLine("Display Provisioning failed.");
            Environment.Exit(1);
        }
    }

    [CommandLineMethod(@"     --ProvisionDisplay: Setup Displays to acquire licenses on first Spark connection
            [DeviceInfoFile] - Device Info File (csv)
            [SparkConnectionInfoFile] - Spark Connection Info File (JSON)
            [ProvisioningFile] - Provisioning File with OTA and License Info (JSON).")]
    private static void PreProvisonDisplays(string deviceInfoFile,
            string sparkConnectionInfoFile,
            string provisioningFile)
    {
        IProgress<string> progress = new Progress<string>(Console.WriteLine);

        var sparkInfo = JsonUtility.DeserializeWithTypeInfo<SparkConnectionInfo>(File.ReadAllText(sparkConnectionInfoFile));
        var provisionInfo = JsonUtility.DeserializeWithTypeInfo<ProvisionDisplayRequest>(File.ReadAllText(provisioningFile));

        if (!SparkCustomerTools.PreProvisionDevices(deviceInfoFile, sparkInfo, provisionInfo, progress))
        {
            Console.WriteLine("Display Provisioning failed.");
            Environment.Exit(1);
        }
    }
}