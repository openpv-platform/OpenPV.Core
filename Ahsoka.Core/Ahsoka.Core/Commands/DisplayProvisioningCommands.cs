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

        var connectionInfo = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(targetConnectionInfoFile), CoreJsonSerializerContext.Default.TargetConnectionInfo);
        var sparkInfo = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(sparkConnectionInfoFile), CoreJsonSerializerContext.Default.SparkConnectionInfo);
        var provisionInfo = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(provisioningFile), CoreJsonSerializerContext.Default.ProvisionDisplayRequest);

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

        var sparkInfo = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(sparkConnectionInfoFile), CoreJsonSerializerContext.Default.SparkConnectionInfo);
        var provisionInfo = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(provisioningFile), CoreJsonSerializerContext.Default.ProvisionDisplayRequest);

        if (!SparkCustomerTools.PreProvisionDevices(deviceInfoFile, sparkInfo, provisionInfo, progress))
        {
            Console.WriteLine("Display Provisioning failed.");
            Environment.Exit(1);
        }
    }
}