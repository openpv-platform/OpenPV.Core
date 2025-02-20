using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Ahsoka.Core.Utility;

/// <summary>
/// Allows Assemblys with Matching Major / Minor to load with the current running runtime system.
/// </summary>
[ExcludeFromCodeCoverage]
public class AssemblyResolver
{
    /// <summary>
    /// Initializes the Assembly Resolver.
    /// </summary>
    public static void Init()
    {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    }

    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        var requestorName = args.RequestingAssembly.GetName();
        string requestedName = args.Name.Substring(0, args.Name.IndexOf(","));

        // We will attempt to use previously loaded assemblies if we can.
        // This may still cause an issue if the version is out of date.
        foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = item.GetName();
            if (!string.IsNullOrEmpty(name.Name) && !item.IsCollectible)
            {
                if (name.Name == requestedName)
                {
                    bool returnValue = name.Version.Major == requestorName.Version.Major && name.Version.Minor == requestorName.Version.Minor;
                    if (returnValue)
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Resolved Assembly Lookup of {args.Name} with {name.FullName} ");
                    else
                        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Could not Resolve Lookup of {args.Name} with any loaded assembly ");

                    return item;
                }
            }
        }

        return null;
    }
}