using Ahsoka.Installer;
using Ahsoka.Utility;
using System.Collections.Generic;
using System.Text;

namespace Ahsoka.Core.DataService;

/// <summary>
/// Base Interface for adding Behaviors to Ahsoka Endpoint Clients
/// </summary>
public class DataServiceGeneratorPlugin : IViewModelGenerator
{
    List<string> IViewModelGenerator.SupportedGenerators => new List<string> { "Ahsoka.Core.Can", "Ahsoka.Core.Storyboard" };

    ApplicationType IViewModelGenerator.SupportedType => ApplicationType.Dotnet;

    void IViewModelGenerator.ExtendHeader(StringBuilder generatorOutput, string nameSpace, bool isHeader)
    {
    }

    void IViewModelGenerator.ExtendConstructor(StringBuilder generatorOutput, string nameSpace)
    {
    }

    void IViewModelGenerator.ExtendSetter(StringBuilder generatorOutput, string propertyName, string dataType)
    {
    }

    void IViewModelGenerator.ExtendAfterClassOutput(StringBuilder generatorOutput, string className)
    {
    }

    void IViewModelGenerator.ExtendMethods(StringBuilder generatorOutput, string className, bool isHeader)
    {
    }
}