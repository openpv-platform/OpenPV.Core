using Ahsoka.Core.Utility;
using Ahsoka.Services.Install;
using Ahsoka.Services.System;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
#pragma warning disable IDE0051 // Remove unused private members

namespace Ahsoka.Core.Commands;

[ExcludeFromCodeCoverage]
[CommandLinePlugin]
internal static class InstallerCommands
{
    [CommandLineMethod(@"     --StartInstallService: Start the Installer and System Services")]
    private static void StartInstallService()
    {
        ConfigurationLoader.SetRuntimeConfiguration(ConfigurationLoader.CreateDefaultConfiguration());

        // Get a Service Configuration for Out of Process Installs
        ServiceConfiguration configLocal = ConfigurationLoader.GetServiceConfig(InstallService.Name);

        Console.WriteLine("Starting Installer Service");

        var installService = new InstallService(configLocal);
        AhsokaRuntime.Default.StartEndPoints(installService);

        // Blocks Until Done
        installService.WaitForStop();

        Console.WriteLine("Service Shutdown Completed");
    }

    [CommandLineMethod(@"     --ReturnToInstaller: Shutdown Current Application and Return to Installer UI
                                                 [TargetConnectionInfoFile] - Connection Info File (JSON)")]
    private static void ReturnToInstaller(string connectionInfoFile)
    {
        var connectionInfo = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(connectionInfoFile), CoreJsonSerializerContext.Default.TargetConnectionInfo);

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
}