using Ahsoka.Core;
using Ahsoka.Core.Hardware;
using Ahsoka.Core.Spark;
using Ahsoka.Core.Utility;
using Ahsoka.Services.System;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
#pragma warning disable IDE0051 // Remove unused private members

namespace Ahsoka.DesktopOnly.Commands;

[ExcludeFromCodeCoverage]
[CommandLinePlugin]
internal static class PrepCommands
{
    [CommandLineMethod(@"     --ReturnToInstaller: Shutdown Current Application and Return to Installer UI
                                                 [TargetConnectionInfoFile] - Connection Info File (JSON)")]
    private static void ReturnToInstaller(string connectionInfoFile)
    {
        var connectionInfo = JsonUtility.DeserializeWithTypeInfo<TargetConnectionInfo>(File.ReadAllText(connectionInfoFile));

        IProgress<string> progress = new Progress<string>(Console.WriteLine);
        var config = new ServiceConfiguration()
        {
            ServiceName = "SystemService",
            TcpConnectionAddress = connectionInfo.HostName,
            DataChannel = 5001,
        };

        var client = new SystemServiceClient(config);
        var cancellationTokenSource = new CancellationTokenSource();
        var prep = RemoteTargetToolFactory.GetToolsForPlatform(connectionInfo.PlatformFamily);

        var result = prep.ReturnToInstaller(connectionInfo, client, cancellationTokenSource, progress);

        if (result == null || !result.IsInstaller)
        {
            Console.WriteLine("Return To Installer failed.");
            Environment.Exit(1);
        }

        Console.WriteLine("Return To Installer Succeeded.");

    }

    [CommandLineMethod(@"     --FactoryPrep: Stores Factory Information in the Cloud (PN/SN/MAC)
            [TargetConnectionInfoFile] - Connection Info File (JSON)
            [SparkConnectionInfoFile] - Spark Connection Info File (JSON)
            [ManufacturingInfoFile] - Manufacturing Info File (JSON) ")]
    private static void FactoryPrep(string connectionInfoFile, string sparkConnectionInfoFile, string manufacturingInfoFile)
    {
        IProgress<string> progress = new Progress<string>(Console.WriteLine);

        var connectionInfo = JsonUtility.DeserializeWithTypeInfo<TargetConnectionInfo>(File.ReadAllText(connectionInfoFile));
        var sparkInfo = JsonUtility.DeserializeWithTypeInfo<SparkConnectionInfo>(File.ReadAllText(sparkConnectionInfoFile));
        var manufacturingInfo = JsonUtility.DeserializeWithTypeInfo<ManufacturingInfo>(File.ReadAllText(manufacturingInfoFile));

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

        var connectionInfo = JsonUtility.DeserializeWithTypeInfo<TargetConnectionInfo>(File.ReadAllText(connectionInfoFile));
        var sparkInfo = JsonUtility.DeserializeWithTypeInfo<SparkConnectionInfo>(File.ReadAllText(sparkConnectionInfoFile));

        var prep = RemoteTargetToolFactory.GetToolsForPlatform(connectionInfo.PlatformFamily);
        if (!prep.Prep(connectionInfo, sparkInfo, progress))
        {
            Console.WriteLine("Prep failed.");
            Environment.Exit(1);
        }
    }

}