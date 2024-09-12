using Ahsoka.Utility;
using System;

namespace Ahsoka;

class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff"));

        // Embedded Targets Don't Load Extensions...Assumed to be Compiled In.
        // Initialing Assembly Resolver
        AssemblyResolver.Init();
        Console.WriteLine($"Loading Extensions ...");
        System.Extensions.LoadExtensions(null, false, "CommandLine");

        CommandLine.Execute(args);
    }
}
