using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ahsoka.Utility;

/// <summary>
/// Allows Assemblys with Matching Major / Minor to load with the current running runtime system.
/// </summary>
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
                    {
                        Console.WriteLine($"Resolved Assembly Lookup of {args.Name}");
                        Console.WriteLine($"                       with {name.FullName} ");
                        Console.WriteLine("");
                    }

                    return item;
                }
            }
        }

        return null;
    }
}