using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;

namespace Ahsoka;

class Program
{
    public static void Main(string[] args)
    {
        //Console.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff"));
        using var stopwatch = new AhsokaStopwatch("Ahsoka.Commandline.Main()");
        //AhsokaLogging.LoggingVerbosity = AhsokaVerbosity.Low;

        List<string> mutableArgs = new(args);

        if (mutableArgs.Count > 0 && mutableArgs[0] == "--WaitForDebugger")
        {
            DebugUtility.WaitForDebugger();

            mutableArgs.RemoveAt(0);
        }

        if (mutableArgs.Count > 0 && mutableArgs[0] == "--Verbose")
        {
            AhsokaLogging.LoggingVerbosity = AhsokaVerbosity.Low | AhsokaVerbosity.Performance;
            mutableArgs.RemoveAt(0);
        }

        CommandLine.Execute(mutableArgs.ToArray());
    }
}
