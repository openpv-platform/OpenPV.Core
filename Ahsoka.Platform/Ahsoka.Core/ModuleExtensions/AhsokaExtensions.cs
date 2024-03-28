using Ahsoka.Installer.InstallEngine;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static NetMQ.NetMQSelector;

namespace Ahsoka.System;

internal class AhsokaExtensions
{
    private static List<Assembly> ExtensionLibraries { get; set; } = new();

    public static List<ModuleInfoConfiguration> Configurations
    {
        get; private set;
    } = new ();

    static AhsokaExtensions()
    {
        LoadExtensions();
    }

    public static List<Assembly> GetExtensionLibraries(bool includeCoreLibrary = false)
    {
        // Always Load Ahsoka.Core Assembly
        var assemblies = ExtensionLibraries.ToList();
        if (includeCoreLibrary)
            assemblies.Add(Assembly.GetExecutingAssembly());

        return assemblies;
    }

    public static List<string> GetModuleFiles(string moduleExtension)
    {
        List<string> strings = new List<string>();
        var configuration = Configurations.FirstOrDefault(x => x.ExtensionName == moduleExtension);
        if (configuration != null)
        {
            string path = Path.GetDirectoryName(configuration.FilePath);
            strings.Add(configuration.FilePath);
            strings.Add(Path.Combine(path,configuration.Library));
            foreach (var item in configuration.DependentLibraries)
                strings.Add(Path.Combine(path, item));
        }

        return strings;
    }

    internal static void LoadExtensions(string path = null)
    {
        if ( !Directory.Exists(path))
            path = PlatformSupportPathInfo.GetModuleSupportFolder();

        if (Directory.Exists(path))
            foreach (var file in Directory.GetFiles(path, "*.moduleinfo.json", SearchOption.AllDirectories))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High,$"AhsokaExtensions - {file}");
                try
                {
                    ModuleInfoConfiguration moduleInfoConfiguration = JsonUtility.Deserialize<ModuleInfoConfiguration>(File.ReadAllText(file));
                    moduleInfoConfiguration.FilePath = file;
                    if (!Configurations.Any(x => x.Library == moduleInfoConfiguration.Library))
                    {
                        Configurations.Add(moduleInfoConfiguration);

                        LoadExtensionLibrary(moduleInfoConfiguration);
                        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Loading Module {moduleInfoConfiguration.ExtensionName }");
                    }
                }
                catch { }
            }
    }

    internal static void LoadExtensionLibrary(ModuleInfoConfiguration moduleInfoConfiguration)
    {
        string path = Path.Combine(Path.GetDirectoryName(moduleInfoConfiguration.FilePath), moduleInfoConfiguration.Library);
        var assembly = Assembly.LoadFrom(path);
        ExtensionLibraries.Insert(0, assembly);
    }

    public static List<T> GetExtensions<T>() where T:class
    {
        List<T> installers = new();
        var assemblies = GetExtensionLibraries();

        foreach (Assembly assembly in assemblies)
        {
            foreach (var item in assembly.DefinedTypes)
            {
                if (item.IsSubclassOf(typeof(T)))
                    installers.Add(ClassLoader.CreateType(item) as T);
            }
        }
        return installers;
    }
}

/// <summary>
/// Module Extension Configuration Info
/// </summary>
public partial class ModuleInfoConfiguration
{
    internal string FilePath { get; set; }

}
