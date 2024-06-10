using Ahsoka.Application.Storyboard;
using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System;

namespace Ahsoka.Base;

public class Program
{
    public static void Main()
    {
        Console.WriteLine($"App Main Time {DateTime.Now:hh:mm:ss.fff}");

        // Show Warnings and Errors as well as Performance Metrics
        AhsokaLogging.LoggingVerbosity = AhsokaVerbosity.Medium | AhsokaVerbosity.Performance;
        AhsokaStopwatch.MaxDepth = 3;

        using var stopwatch = new AhsokaStopwatch("OpenPV-Demo.Main()");

        // Start The Application Model
        ApplicationViewModelRoot.Default.StartAndRun();
    }
}
