using Ahsoka.Installer;
using Ahsoka.System;
using Ahsoka.Utility;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ahsoka.Commands;

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