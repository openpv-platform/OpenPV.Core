using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Core.Commands;

[ExcludeFromCodeCoverage]
[CommandLinePlugin]
internal static class ProtoGeneratorCommands
{
    [CommandLineMethod(@"     --GenerateProto: Generate Proto File for a Non-DotNet Language
            [ServiceName] 
            [OutputFileName] Folder where Class Definitions will be created")]
    private static void GenerateProto(string ServiceName, string outputFileName)
    {
        ProtoGenerator.GenerateProtoFile(ServiceName, outputFileName);
    }

}