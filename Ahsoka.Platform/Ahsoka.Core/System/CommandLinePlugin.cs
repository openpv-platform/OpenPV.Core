using System;
using System.Reflection;

namespace Ahsoka.System;


/// <summary>
/// Attribute for Identifying Ahsoka Command Line Plugins
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CommandLinePlugin : Attribute
{

}

/// <summary>
/// Attribute for Identifying Command Line Methods on CommandPlugins
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class CommandLineMethod : Attribute
{
    /// <summary>
    /// Gets the Help Text associated with the Command Method
    /// </summary>
    public string HelpText { get; init; }

    /// <summary>
    /// Constructor taking Help Text
    /// </summary>
    /// <param name="helpText"></param>
    public CommandLineMethod(string helpText)
    {
        this.HelpText = helpText;
    }
}

internal class CommandInfo
{
    public CommandLineMethod MethodAttribute { get; set; }
    public MethodInfo Method { get; set; }
    public string Plugin { get; set; }
}

