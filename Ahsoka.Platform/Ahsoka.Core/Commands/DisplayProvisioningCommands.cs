using Ahsoka.Spark;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.IO;

namespace Ahsoka.Commands;

[CommandLinePlugin]
internal static class DisplayProvisioningCommands
{
    [CommandLineMethod(@"     --ProvisionDisplay: License the Display For the Specified MOdules
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

        var connectionInfo = JsonUtility.Deserialize<TargetConnectionInfo>(File.ReadAllText(targetConnectionInfoFile));
        var sparkInfo = JsonUtility.Deserialize<SparkConnectionInfo>(File.ReadAllText(sparkConnectionInfoFile));
        var provisionInfo = JsonUtility.Deserialize<ProvisionDisplayRequest>(File.ReadAllText(provisioningFile));

        if (!SparkCustomerTools.ProvisionDisplayInSpark(connectionInfo, sparkInfo, provisionInfo, progress))
        {
            Console.WriteLine("Display Provisioning failed.");
            Environment.Exit(1);
        }
    }
}