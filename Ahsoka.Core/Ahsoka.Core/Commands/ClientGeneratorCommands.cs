using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System.Collections.Generic;
using System.IO;

namespace Ahsoka.Commands;

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