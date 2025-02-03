using Ahsoka.Installer;
using System.Collections.Generic;

namespace Ahsoka.Core.Commands;

[CommandLinePlugin]
internal static class TestCommands
{
    [CommandLineMethod(@"     --ExampleCommand")]
    private static void GenerateStoryboardClasses()
    {
    }
}

internal class ExampleCodeGenerator : IExtensionGenerator
{
    public void GetCommands(PackageInformation packageInfo, Dictionary<string, GeneratorCommandType> commandsToExecute, CommandTypes commandTypes)
    {
        commandsToExecute.Add($" --ExampleCommand", GeneratorCommandType.AhsokaCommandLine);
    }
}