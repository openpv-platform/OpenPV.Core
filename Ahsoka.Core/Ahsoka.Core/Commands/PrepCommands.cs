using Ahsoka.Services.System;
using Ahsoka.Spark;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
#pragma warning disable IDE0051 // Remove unused private members

namespace Ahsoka.Commands;

[ExcludeFromCodeCoverage]
[CommandLinePlugin]
internal static class PrepCommands
{
    [CommandLineMethod(@"     --FactoryPrep: Stores Factory Information in the Cloud (PN/SN/MAC)
            [TargetConnectionInfoFile] - Connection Info File (JSON)
            [SparkConnectionInfoFile] - Spark Connection Info File (JSON)
            [ManufacturingInfoFile] - Manufacturing Info File (JSON) ")]
    private static void FactoryPrep(string connectionInfoFile, string sparkConnectionInfoFile, string manufacturingInfoFile)
    {
        IProgress<string> progress = new Progress<string>(Console.WriteLine);

        var connectionInfo = JsonUtility.Deserialize<TargetConnectionInfo>(File.ReadAllText(connectionInfoFile));
        var sparkInfo = JsonUtility.Deserialize<SparkConnectionInfo>(File.ReadAllText(sparkConnectionInfoFile));
        var manufacturingInfo = JsonUtility.Deserialize<ManufacturingInfo>(File.ReadAllText(manufacturingInfoFile));

        var prep = RemoteTargetToolFactory.GetToolsForPlatform(connectionInfo.PlatformFamily);
        if (!prep.Prep(connectionInfo, sparkInfo, progress, manufacturingInfo))
        {
            Console.WriteLine("FactoryPrep failed.");
            Environment.Exit(1);
        }
    }

    [CommandLineMethod(@"     --Prep: Stores Factory Information in the Cloud (PN/SN/MAC)
            [TargetConnectionInfoFile] - Connection Info File (JSON)
            [SparkConnectionInfoFile] - Spark Connection Info File (JSON)")]
    private static void Prep(string connectionInfoFile, string sparkConnectionInfoFile)
    {
        IProgress<string> progress = new Progress<string>(Console.WriteLine);

        var connectionInfo = JsonUtility.Deserialize<TargetConnectionInfo>(File.ReadAllText(connectionInfoFile));
        var sparkInfo = JsonUtility.Deserialize<SparkConnectionInfo>(File.ReadAllText(sparkConnectionInfoFile));

        var prep = RemoteTargetToolFactory.GetToolsForPlatform(connectionInfo.PlatformFamily);
        if (!prep.Prep(connectionInfo, sparkInfo, progress))
        {
            Console.WriteLine("Prep failed.");
            Environment.Exit(1);
        }
    }

    [CommandLineMethod(@"     --GetHardwareModel: Get Model information to identify the hardware")]
    private static void GetHardwareModel()
    {
        var coreInterop = SystemService.GetInterop();
        Console.WriteLine($"{RemoteTargetToolFactory.HardwareModelPrefix}{coreInterop.GetHardwareModel()}");
    }

    [CommandLineMethod(@"     --GetHardwareSerialNumber: Get UniqueId used to identify the hardware")]
    private static void GetHardwareSerialNumber()
    {
        var coreInterop = SystemService.GetInterop();
        Console.WriteLine($"{RemoteTargetToolFactory.HardwareIdPrefix}{coreInterop.GetHardwareSerialNumber()}");
    }
}