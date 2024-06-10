using Ahsoka.ServiceFramework;
using Ahsoka.System;

namespace Ahsoka.Commands;

[CommandLinePlugin]
internal static class ClientGeneratorCommands
{
    [CommandLineMethod(@"     --GenerateClients: Generate Clients for a Non-DotNet Language
            [PathToOutputFolder] Folder where Class Definitions will be created")]
    private static void GenerateClients(string pathToOutputFolder)
    {
        ClientCodeGenerator.GenerateClients(pathToOutputFolder);
    }

}