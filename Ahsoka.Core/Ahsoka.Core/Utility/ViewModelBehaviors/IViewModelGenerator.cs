

using Ahsoka.Installer;
using Ahsoka.System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ahsoka.Utility;

/// <summary>
/// Base Interface for adding Behaviors to Ahsoka Endpoint Clients
/// </summary>
public interface IViewModelGenerator
{
    /// <summary>
    /// List of Generators Supported
    /// </summary>
    public List<string> SupportedGenerators { get; }

    /// <summary>
    /// Type of Source Code This Generator Supports
    /// </summary>
    public ApplicationType SupportedType { get; }

    /// <summary>
    /// Interface for Extensing the Generated Source's Header (Includes / Import / Usings)
    /// </summary>
    /// <param name="generatorOutput"></param>
    /// <param name="nameSpace"></param>
    /// <param name="isHeader">Indicates if generator is creating a header or implementation for CPP.  Unused or other languages</param>
    void ExtendHeader(StringBuilder generatorOutput, string nameSpace, bool isHeader);

    /// <summary>
    /// Extens
    /// </summary>
    /// <param name="generatorOutput"></param>
    /// <param name="nameSpace"></param>
    void ExtendConstructor(StringBuilder generatorOutput, string nameSpace);

    /// <summary>
    /// Called after Generation is complete for a class.  Enables adding 
    /// related data to the file such as constants and typedefs.
    /// </summary>
    /// <param name="generatorOutput"></param>
    /// <param name="className"></param>
    void ExtendAfterClassOutput(StringBuilder generatorOutput, string className);

    /// <summary>
    /// Called for each Property Setter to enable generating code
    /// thats called as a property is set on object.
    /// </summary>
    /// <param name="generatorOutput"></param>
    /// <param name="propertyName"></param>
    /// <param name="dataType"></param>
    /// <returns></returns>
    void ExtendSetter(StringBuilder generatorOutput, string propertyName, string dataType);


    /// <summary>
    /// Called after methods are added to the class to allow the extension
    /// to add its own methods.
    /// </summary>
    /// <param name="generatorOutput"></param>
    /// <param name="className"></param>
    /// <param name="isHeader">Indicates if generator is creating a header or implementation for CPP.  Unused or other languages</param>
    void ExtendMethods(StringBuilder generatorOutput, string className, bool isHeader);
}

/// <summary>
/// Class used to manage Generator Plugins (IViewModelGenerator) when running 
/// a code generator.
/// </summary>
public class GeneratorUtility
{
    List<IViewModelGenerator> plugins;
    string generatorName;
    ApplicationType appType;

    /// <summary>
    /// Constructor used by Generators
    /// </summary>
    /// <param name="generatorName"></param>
    /// <param name="applicationType"></param>
    public GeneratorUtility(string generatorName, ApplicationType applicationType)
    {
        this.generatorName = generatorName;
        appType = applicationType;

        // Load Applicable Plugins
        plugins = Extensions.GetExtensions<IViewModelGenerator>().Where(x => x.SupportedType == applicationType && x.SupportedGenerators.Contains(generatorName)).ToList();
    }

    /// <summary>
    /// Extend the Header with Using, Includes etc.
    /// </summary>
    /// <param name="generatorOutput"></param>
    /// <param name="nameSpace"></param>
    /// <param name="isHeader">Indicates if generator is creating a header or implementation for CPP.  Unused or other languages</param>
    public void ExtendHeader(StringBuilder generatorOutput, string nameSpace, bool isHeader = false)
    {
        foreach (var item in plugins)
            item.ExtendHeader(generatorOutput, nameSpace, isHeader);
    }

    /// <summary>
    ///  Called to allow adding logic to the Constructors such as initializing 
    ///  behaviors or setting property values.
    /// </summary>
    /// <param name="generatorOutput"></param>
    /// <param name="className"></param>
    public void ExtendConstructor(StringBuilder generatorOutput, string className)
    {
        foreach (var item in plugins)
            item.ExtendConstructor(generatorOutput, className);
    }

    /// <summary>
    /// Called after Generation is complete for a class.  Enables adding 
    /// related data to the file such as constants and typedefs.
    /// </summary>
    /// <param name="generatorOutput"></param>
    /// <param name="className"></param>
    public void ExtendAfterClassOutput(StringBuilder generatorOutput, string className)
    {
        foreach (var item in plugins)
            item.ExtendAfterClassOutput(generatorOutput, className);

    }

    /// <summary>
    /// Extend the class by adding new methods to the generated output
    /// Called after generator has added any built in methods.
    /// </summary>
    /// <param name="generatorOutput"></param>
    /// <param name="className"></param>
    /// <param name="isHeader">Indicates if generator is creating a header or implementation for CPP.  Unused or other languages</param>
    public void ExtendMethods(StringBuilder generatorOutput, string className, bool isHeader)
    {
        foreach (var item in plugins)
            item.ExtendMethods(generatorOutput, className, isHeader);
    }

    /// <summary>
    /// Called for each Property Setter to enable generating code
    /// thats called as a property is set on object.
    /// </summary>
    /// <param name="generatorOutput"></param>
    /// <param name="propertyName"></param>
    /// <param name="dataType"></param>
    /// <returns></returns>
    public void ExtendSetter(StringBuilder generatorOutput, string propertyName, string dataType)
    {
        foreach (var item in plugins)
            item.ExtendSetter(generatorOutput, propertyName, dataType);

    }
}