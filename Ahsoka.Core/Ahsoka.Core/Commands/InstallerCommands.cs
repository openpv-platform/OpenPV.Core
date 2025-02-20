using Ahsoka.Core;
using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility;
using Ahsoka.Services.Install;
using Ahsoka.Services.System;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
#pragma warning disable IDE0051 // Remove unused private members

namespace Ahsoka.DesktopOnly.Commands;

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

  
}