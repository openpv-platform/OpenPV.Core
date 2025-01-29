using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Commands;

[ExcludeFromCodeCoverage]
[CommandLinePlugin]
internal static class ClientGeneratorCommands
{
    [CommandLineMethod(@"     --GenerateClients: Generate Clients for a Non-DotNet Language
            [PathToPackageInfo] File containing Package Info
            [PathToOutputFolder] Folder where Class Definitions will be created")]
    private static void GenerateClients(string pathToPackageInfo, string pathToOutputFolder)
    {
        var packageInfo = PackageInformation.LoadPackageInformation(pathToPackageInfo);

        ClientCodeGenerator.GenerateClients(packageInfo, pathToOutputFolder);
    }

}