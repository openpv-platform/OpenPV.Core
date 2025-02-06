using Ahsoka.Core.Utility;
using Ahsoka.Services.System;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
#pragma warning disable IDE0051 // Remove unused private members

namespace Ahsoka.Core.Commands;

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

        var connectionInfo = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(connectionInfoFile), CoreJsonSerializerContext.Default.TargetConnectionInfo);
        var sparkInfo = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(sparkConnectionInfoFile), CoreJsonSerializerContext.Default.SparkConnectionInfo);
        var manufacturingInfo = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(manufacturingInfoFile), CoreJsonSerializerContext.Default.ManufacturingInfo);

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

        var connectionInfo = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(connectionInfoFile), CoreJsonSerializerContext.Default.TargetConnectionInfo);
        var sparkInfo = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(sparkConnectionInfoFile), CoreJsonSerializerContext.Default.SparkConnectionInfo);

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