using Ahsoka.Demo.Storyboard;
using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System;

namespace Ahsoka.Demo.SBIO.ViewModels;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($"App Main Time {DateTime.Now:hh:mm:ss.fff}");

        // Show Warnings and Errors as well as Performance Metrics
        AhsokaLogging.LoggingVerbosity = AhsokaVerbosity.Medium | AhsokaVerbosity.Performance;
        AhsokaStopwatch.MaxDepth = 3;

        using var stopwatch = new AhsokaStopwatch("Ahsoka.Demo.SBIO.Main()");

        // Start The Application Model
        DemoViewModelRoot.Default.StartAndRun();
    }
}
