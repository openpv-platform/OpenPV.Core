using Ahsoka.ServiceFramework;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using static NetMQ.NetMQSelector;

namespace Ahsoka.System;

internal class AhsokaExtensions
{
    internal const string LibFolder = "lib";
    internal const string ModulesFolder = "Modules";
    private static bool initComplete = false;

    private static List<Assembly> ExtensionLibraries { get; set; } = new(); 

    public static List<ModuleInfoConfiguration> Configurations
    {
        get; private set;
    } = new ();

    static AhsokaExtensions()
    {
        
    }

    internal static void InstallModuleExtension(string basePath, string extensionName, IProgress<string> progress = null)
    {
        Dictionary<string, byte[]> additionalFiles = new Dictionary<string, byte[]> ();
        InstallModuleExtension(additionalFiles, extensionName);

        progress.Report($" ");
        progress.Report($"Installing {extensionName}");
        
        foreach (var item in additionalFiles)
        {
            progress.Report($"  Adding {item.Key}");
            string path = Path.Combine(basePath, item.Key);

            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllBytes(path, item.Value);
        }
    }

    internal static void InstallModuleExtension(Dictionary<string, byte[]> additionalFiles, string extensionName)
    {
        var config = Configurations.FirstOrDefault(x => x.ExtensionName == extensionName);
        string pathToModuleCache = Path.GetDirectoryName(config.FilePath);

        foreach (var file in Directory.GetFiles(pathToModuleCache,"*", SearchOption.AllDirectories)) 
        {
            if (!file.Contains("\\ux\\"))
            {
                string fileNameFromModules = file.Replace(pathToModuleCache, "");
                string fileName = $"Modules/{extensionName}/{fileNameFromModules.Replace("\\", "/").TrimStart('/')}";
                additionalFiles[fileName] = File.ReadAllBytes(file);
            }
        }
    }

    internal static void LoadExtensions(string path = null)
    {
        if (initComplete)
            return;

        if (path == null)
            path = PlatformSupportPathInfo.GetModuleSupportFolder();

        string modulePath = path;

        // Make sure this is the Module Folder
        if (!modulePath.EndsWith(ModulesFolder))
            modulePath = Path.Combine(path, ModulesFolder);

        // Fallback to desktop Module Support Folder
        if (!Directory.Exists(modulePath))
            modulePath = PlatformSupportPathInfo.GetModuleSupportFolder();

        if (Directory.Exists(modulePath))
        {
            foreach (var file in Directory.GetFiles(modulePath, "*.moduleinfo.json", SearchOption.AllDirectories))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"AhsokaExtensions - {file}");
                try
                {
                    ModuleInfoConfiguration moduleInfoConfiguration = JsonUtility.Deserialize<ModuleInfoConfiguration>(File.ReadAllText(file));
                    moduleInfoConfiguration.FilePath = file;
                    if (!Configurations.Any(x => x.PackageName == moduleInfoConfiguration.PackageName))
                    {
                        Configurations.Add(moduleInfoConfiguration);

                        LoadExtension(moduleInfoConfiguration);
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Loading Module {moduleInfoConfiguration.ExtensionName}");
                    }
                }
                catch { }
            }
        }

        initComplete = Configurations.Count() > 0;
    }

    public static List<Assembly> GetExtensionLibraries(bool includeCoreLibrary = false)
    {
        // Always Load Ahsoka.Core Assembly
        var assemblies = ExtensionLibraries.ToList();
        if (includeCoreLibrary)
            assemblies.Add(Assembly.GetExecutingAssembly());

        return assemblies;
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

    private static List<string> GetModuleFiles(string moduleExtension)
    {
        List<string> strings = new List<string>();
        var configuration = Configurations.FirstOrDefault(x => x.ExtensionName == moduleExtension);
        if (configuration != null)
        {
            string path = Path.GetDirectoryName(configuration.FilePath);
            strings.Add(configuration.FilePath);
            string moduleFolder = Path.Combine(Path.GetDirectoryName(configuration.FilePath), LibFolder);
            foreach (var item in Directory.GetFiles(moduleFolder, "*.dll"))
                strings.Add(item);
        }

        return strings;
    }

    private static void LoadExtension(ModuleInfoConfiguration moduleInfoConfiguration)
    {
        var list =  AppDomain.CurrentDomain.GetAssemblies().Select(x=>x.GetName().Name).ToList();   
           
        foreach (var item in GetModuleFiles(moduleInfoConfiguration.ExtensionName).Where(x => x.EndsWith(".dll")))
        {
            try
            {
                // Don't Reload Files
                if (list.Contains(Path.GetFileNameWithoutExtension(item)))
                    continue;

                var assembly = Assembly.LoadFrom(item);
                ExtensionLibraries.Insert(0, assembly);
            }
            catch
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Error Loading This Module {item}");
            }
        }
    }
}

/// <summary>
/// Module Extension Configuration Info
/// </summary>
public partial class ModuleInfoConfiguration
{
    internal string FilePath { get; set; }

    /// <summary>
    /// FileName for Library
    /// </summary>
    public string LibraryFilename { get { return PackageName + ".dll"; } }
    
    /// <summary>
    /// FileName for UXLibrary
    /// </summary>
    public string UXLibraryFilename { get { return UxPackageName + ".dll"; } }

}
