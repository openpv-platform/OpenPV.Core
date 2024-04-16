using Ahsoka.Installer;
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

internal class Extensions
{
    internal const string LibFolder = "lib";
    internal const string ExtensionsFolder = "Extensions";
    private static bool initComplete = false;

    private static List<Assembly> ExtensionLibraries { get; set; } = new(); 

    public static List<ExtensionConfiguration> Configurations
    {
        get; private set;
    } = new ();

    static Extensions()
    {
        
    }

    internal static void InstallExtension(string basePath, string extensionName, IProgress<string> progress = null)
    {
        Dictionary<string, byte[]> additionalFiles = new Dictionary<string, byte[]> ();
        InstallExtension(additionalFiles, extensionName);

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

    internal static void InstallExtension(Dictionary<string, byte[]> additionalFiles, string extensionName)
    {
        var config = Configurations.FirstOrDefault(x => x.ExtensionName == extensionName);
        string pathToExtensionCache = Path.GetDirectoryName(config.FilePath);

        foreach (var file in Directory.GetFiles(pathToExtensionCache,"*", SearchOption.AllDirectories)) 
        {
            if (!file.Contains("\\ux\\"))
            {
                string fileNameFromExtensions = file.Replace(pathToExtensionCache, "");
                string fileName = $"Extensions/{extensionName}/{fileNameFromExtensions.Replace("\\", "/").TrimStart('/')}";
                additionalFiles[fileName] = File.ReadAllBytes(file);
            }
        }
    }

    internal static void LoadExtensions(string path = null)
    {
        if (initComplete)
            return;

        if (path == null)
            path = PlatformSupportPathInfo.GetExtensionSupportFolder();

        string extPath = path;

        // Make sure this is the Extension Folder
        if (!extPath.EndsWith(ExtensionsFolder))
            extPath = Path.Combine(path, ExtensionsFolder);

        // Fallback to desktop Extension Support Folder
        if (!Directory.Exists(extPath))
            extPath = PlatformSupportPathInfo.GetExtensionSupportFolder();

        if (Directory.Exists(extPath))
        {
            foreach (var file in Directory.GetFiles(extPath, "*.extensioninfo.json", SearchOption.AllDirectories))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"AhsokaExtensions - {file}");
                try
                {
                    ExtensionConfiguration extInfoConfiguration = JsonUtility.Deserialize<ExtensionConfiguration>(File.ReadAllText(file));
                    extInfoConfiguration.FilePath = file;
                    if (!Configurations.Any(x => x.PackageName == extInfoConfiguration.PackageName))
                    {
                        Configurations.Add(extInfoConfiguration);

                        LoadExtension(extInfoConfiguration);
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Loading Module {extInfoConfiguration.ExtensionName}");
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

    private static List<string> GetExtensionFiles(string extension)
    {
        List<string> strings = new List<string>();
        var configuration = Configurations.FirstOrDefault(x => x.ExtensionName == extension);
        if (configuration != null)
        {
            string path = Path.GetDirectoryName(configuration.FilePath);
            strings.Add(configuration.FilePath);
            string extensionFolder = Path.Combine(Path.GetDirectoryName(configuration.FilePath), LibFolder);
            foreach (var item in Directory.GetFiles(extensionFolder, "*.dll"))
                strings.Add(item);
        }

        return strings;
    }

    private static void LoadExtension(ExtensionConfiguration extensionConfiguration)
    {
        var list =  AppDomain.CurrentDomain.GetAssemblies().Select(x=>new { x.GetName().Name, Assm = x  }).ToList();   
           
        foreach (var item in GetExtensionFiles(extensionConfiguration.ExtensionName).Where(x => x.EndsWith(".dll")))
        {
            try
            {
                // Don't Reload Files
                var asm = list.FirstOrDefault(x => x.Name == Path.GetFileNameWithoutExtension(item))?.Assm;
                if (asm == null)
                    asm = Assembly.UnsafeLoadFrom(item);

                if (!ExtensionLibraries.Contains(asm))
                {
                    ExtensionLibraries.Insert(0, asm);
                    ClassLoader.AddAssembly(asm);
                }
            }
            catch
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Error Loading This Extension - {item}");
            }
        }
    }

    internal static Dictionary<string, GeneratorCommandType> GetCommands(PackageInformation packageInfo, string extensionName,CommandTypes commandTypes )
    {
        var commands = new Dictionary<string, GeneratorCommandType>();

        var config = Configurations.FirstOrDefault(x => x.ExtensionName == extensionName);
        if (config != null)
        {
            try
            {
                var asm = ExtensionLibraries.FirstOrDefault(x => Path.GetFileName(x.Location) == config.LibraryFilename);
                if (asm != null)
                    foreach (var defType in asm.DefinedTypes)
                    {
                        if (typeof(IExtensionGenerator).IsAssignableFrom(defType) && !defType.IsAbstract &&
                            defType.Name == config.SdkGeneratorName)
                        {
                            // Execute Generator
                            IExtensionGenerator generator = ClassLoader.CreateType(defType, null) as IExtensionGenerator;
                            generator.GetCommands(packageInfo, commands, commandTypes);
                        }
                    }
            }
            catch
            {
                // TODO Notify User of Bad Extension
            }

        }

        return commands;
    }
}

/// <summary>
/// Types of Commands to Get / Execute
/// </summary>
public enum CommandTypes
{
    /// <summary>
    /// Commands that must be called to generate artifacts used for Compiling - Run During SDK Updates
    /// </summary>
    SdkGenerator,

    /// <summary>
    /// Commands that Generate Models from Application Configurations - Run During SDK Updates and Build
    /// </summary>
    ModelGenerators,

    /// <summary>
    /// Commands that Generate Other Build Artifacts - Run Prior to Generation
    /// </summary>
    BuildCommands
}


/// <summary>
/// Code Generator for Extensions
/// </summary>
public interface IExtensionGenerator
{
    /// <summary>
    /// Executes a Code Generator
    /// </summary>
    /// <param name="packageInfo"></param>
    /// <param name="commandsToExecute"></param>
    /// <param name="commandTypes"></param>
    /// <returns></returns>
    void GetCommands(PackageInformation packageInfo, Dictionary<string, GeneratorCommandType> commandsToExecute, CommandTypes commandTypes);
}

/// <summary>
/// Type of Commands Returned from the Generator
/// </summary>
public enum GeneratorCommandType
{
    /// <summary>
    /// Arguments to pass to the OpenPV CLI
    /// </summary>
    AhsokaCommandLine,

    /// <summary>
    /// Execute Command in Shell
    /// </summary>
    ShellCommand
}

