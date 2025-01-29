using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ahsoka;

#pragma warning disable CS1591

[CommandLinePlugin]
public static class CommandLine
{
    public static void Execute(string[] args)
    {
        string pathToExecutable = Environment.ProcessPath;

        using var stopwatch = new AhsokaStopwatch("Ahsoka.Commandline.Main()");

        args = HandleStartupOptions(args);

        string version = VersionUtility.GetAppVersionString();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine();
        Console.WriteLine($"Executing Ahsoka.CommandLine ({version}) from...");
        Console.ResetColor();

        Console.WriteLine($"    {pathToExecutable}");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"With Arguments...");
        Console.ResetColor();
        Console.Write("    ");
        if (args.Length > 0)
        {
            foreach (var item in args)
                Console.Write(item + " ");
        }
        else
            Console.WriteLine($"<No Arguements Received>");

        Console.WriteLine();
        Console.WriteLine();

        // Get Command and Args
        string command = string.Empty;
        string[] commandArgs = Array.Empty<string>();

        if (args.Length > 0)
            command = args[0].TrimStart('-');

        if (args.Length > 1)
            commandArgs = args[1..];

        bool commmandFound = ExecuteCommand(command, commandArgs);
        if (!commmandFound)
        {
            PrintHelp();
        }
    }

    private static string[] HandleStartupOptions(string[] args)
    {
        List<string> mutableArgs = new(args);
        if (mutableArgs.Count > 0)
        {
            int waitForDebugger = mutableArgs.IndexOf("--WaitForDebugger");
            if (waitForDebugger >= 0)
            {
                DebugUtility.WaitForDebugger();
                mutableArgs.RemoveAt(waitForDebugger);
            }

            int verbose = mutableArgs.IndexOf("--Verbose");
            if (verbose >= 0)
            {
                AhsokaLogging.LoggingVerbosity = AhsokaVerbosity.Low | AhsokaVerbosity.Performance;
                mutableArgs.RemoveAt(verbose);
            }
        }

        return mutableArgs.ToArray();
    }

    private static bool ExecuteCommand(string command, string[] commandArgs)
    {
        var methods = GetCommands();

        if (methods.TryGetValue(command, out CommandInfo info))
        {
            var parameters = info.Method.GetParameters();
            object[] methodArgs = new object[parameters.Length];

            if (parameters.Length > 0 && parameters[0].ParameterType == typeof(string[]))
                methodArgs[0] = commandArgs;
            else
                commandArgs.CopyTo(methodArgs, 0);

            info.Method.Invoke(null, methodArgs);
        }

        return info != null;
    }

    static Dictionary<string, CommandInfo> GetCommands()
    {
        Dictionary<string, CommandInfo> methods = new();

        var assemblies = System.Extensions.GetExtensionLibraries(true);

        foreach (var asmwf in assemblies)
        {
            if (asmwf.TryGetTarget(out Assembly assembly))
                foreach (var item in assembly.DefinedTypes)
                {
                    var attr = item.GetCustomAttribute<CommandLinePlugin>();
                    if (attr != null)
                        foreach (var method in item.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                        {
                            var commandAttr = method.GetCustomAttribute<CommandLineMethod>();
                            if (commandAttr != null)
                                methods.Add(method.Name, new CommandInfo()
                                {
                                    MethodAttribute = commandAttr,
                                    Method = method,
                                    Plugin = $"{item.Name} ({assembly.GetName().Name})"
                                });
                        }
                }
        }
        return methods;
    }

    private static void PrintHelp()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Usage: Ahsoka.CommandLine --COMMAND [<ARGS>]");
        Console.ResetColor();

        string plugin = "";
        foreach (var methodInfo in GetCommands().Values)
        {
            if (plugin != methodInfo.Plugin)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine();
                Console.WriteLine($"Available in {methodInfo.Plugin}");
                plugin = methodInfo.Plugin;
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine(methodInfo.MethodAttribute.HelpText);
        }
    }
}
