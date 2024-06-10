using Ahsoka.Services.Bluetooth.LE;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;

namespace Ahsoka;

class Program
{
    public static void Main(string[] args)
    {
        // Force Load Assemblies
        Console.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff"));
        using var stopwatch = new AhsokaStopwatch("Ahsoka.Commandline.Main()");

        List<string> mutableArgs = new(args);

        if (mutableArgs.Count > 0 && mutableArgs[0] == "--WaitForDebugger")
        {
            DebugUtility.WaitForDebugger();

            mutableArgs.RemoveAt(0);
        }

        CommandLine.Execute(mutableArgs.ToArray());
    }
}
