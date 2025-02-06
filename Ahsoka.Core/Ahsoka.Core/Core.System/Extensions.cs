using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility;
using Ahsoka.Installer;
using Ahsoka.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Ahsoka.Core;

/// <summary>
/// Extension Manager for OpenPV
/// </summary>
public class Extensions
{
    internal const string LibFolder = "lib";
    internal const string UxFolder = "ux";
    internal const string ExtensionsFolder = ".extensions";
    static AssemblyLoadContext currentLoadContext;

    private static List<WeakReference<Assembly>> ExtensionLibraries { get; set; } = new();

    internal static List<ExtensionConfiguration> Configurations
    {
        get;
        private set;
    } = new();

    static Extensions()
    {

    }

    /// <summary>
    /// Add a Private Extension to the Extension Manager
    /// </summary>
    /// <param name="assembly"></param>
    public static void AddPrivateExtension(Assembly assembly)
    {
        ExtensionLibraries.Add(new WeakReference<Assembly>(assembly));
    }

    internal static void InstallExtension(Dictionary<string, byte[]> additionalFiles, string extensionName, IProgress<string> progress = null)
    {
        var config = Configurations.FirstOrDefault(x => x.ExtensionName == extensionName);
        if (config == null)
        {
            progress?.Report($"No extension found for {extensionName}");
            return;
        }

        string pathToExtensionCache = Path.GetDirectoryName(config.FilePath);

        foreach (var file in Directory.GetFiles(pathToExtensionCache, "*", SearchOption.AllDirectories))
        {
            if (!file.Contains("\\ux\\") && !file.Contains("\\demos"))
            {
                string fileNameFromExtensions = file.Replace(pathToExtensionCache, "");
                string fileName = $"{ExtensionsFolder}/{extensionName}/{fileNameFromExtensions.Replace("\\", "/").TrimStart('/')}";
                additionalFiles[fileName] = File.ReadAllBytes(file);
            }
        }
    }

    /// <summary>
    ///  Load Extensions from the specified path (or use default if null)
    /// </summary>
    /// <param name="extPath"></param>
    /// <param name="name"></param>
    /// <param name="includeUx"></param>
    public static void LoadExtensions(string extPath = "", bool includeUx = false, string name = "ExtensionContext")
    {
        // Search for .extensions folder in the project / path
        if (String.IsNullOrEmpty(extPath))
        {
            string path = AppContext.BaseDirectory;
            extPath = PlatformSupportPathInfo.FindPlatformSupportPath(path, ExtensionsFolder);
        }

        if (!Directory.Exists(extPath))
            return;

        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Extension Path is " + extPath);

        // Unload any Libraries in the Load Context.
        if (currentLoadContext != null)
        {
            foreach (var item in ExtensionLibraries)
                if (item.TryGetTarget(out var lib))
                    ClassLoader.RemoveAssembly(lib);

            ExtensionLibraries.Clear();
            Configurations.Clear();

            currentLoadContext.Unload();
            currentLoadContext = null;
        }

        currentLoadContext = new AssemblyLoadContext("ExtensionContext", true);

        if (Directory.Exists(extPath))
        {
            foreach (var file in Directory.GetFiles(extPath, "*.extensioninfo.json", SearchOption.AllDirectories))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"AhsokaExtensions - {file}");
                try
                {
                    ExtensionConfiguration extInfoConfiguration = JsonUtility.DeserializeWithTypeInfo(File.ReadAllText(file), CoreJsonSerializerContext.Default.ExtensionConfiguration);
                    extInfoConfiguration.FilePath = file;
                    if (!Configurations.Any(x => x.PackageName == extInfoConfiguration.PackageName))
                    {
                        Configurations.Add(extInfoConfiguration);

                        LoadExtension(extInfoConfiguration, includeUx);
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Loading Module {extInfoConfiguration.ExtensionName}");
                    }
                }
                catch { }
            }
        }
    }

    internal static List<WeakReference<Assembly>> GetExtensionLibraries(bool includeCoreLibrary = false)
    {
        // Always Load Ahsoka.Core Assembly
        var assemblies = ExtensionLibraries.ToList();
        if (includeCoreLibrary)
            assemblies.Add(new WeakReference<Assembly>(Assembly.GetExecutingAssembly()));

        return assemblies;
    }

    internal static List<T> GetExtensions<T>() where T : class
    {
        List<T> installers = new();
        var assemblies = GetExtensionLibraries();

        foreach (var assembly in assemblies)
        {
            if (assembly.TryGetTarget(out var t))
                foreach (var item in t.DefinedTypes)
                {
                    if (typeof(T).IsAssignableFrom(item))
                        installers.Add(ClassLoader.CreateType(item) as T);
                }
        }
        return installers;
    }

    private static List<string> GetExtensionFiles(string extension, bool includeUx)
    {
        List<string> strings = new List<string>();
        var configuration = Configurations.FirstOrDefault(x => x.ExtensionName == extension);
        if (configuration != null)
        {
            string path = Path.GetDirectoryName(configuration.FilePath);
            strings.Add(configuration.FilePath);
            string extensionFolder = Path.Combine(Path.GetDirectoryName(configuration.FilePath), LibFolder);
            if (Directory.Exists(extensionFolder))
                foreach (var item in Directory.GetFiles(extensionFolder, "*.dll"))
                    strings.Add(item);

            if (includeUx)
            {
                extensionFolder = Path.Combine(Path.GetDirectoryName(configuration.FilePath), UxFolder);
                if (Directory.Exists(extensionFolder))
                    foreach (var item in Directory.GetFiles(extensionFolder, "*.dll"))
                        strings.Add(item);
            }
        }

        return strings;
    }

    private static void LoadExtension(ExtensionConfiguration extensionConfiguration, bool includeUx)
    {
        foreach (var item in GetExtensionFiles(extensionConfiguration.ExtensionName, includeUx).Where(x => x.EndsWith(".dll")))
        {
            try
            {
                // Don't Reload Files 
                var currentList = currentLoadContext.Assemblies.FirstOrDefault(x => x.GetName().Name == Path.GetFileNameWithoutExtension(item));
                if (currentList == null)
                {
                    Assembly asm = null;
                    if (SystemInfo.IsDesktop())
                    {
                        using var fs = new FileStream(item, FileMode.Open);
                        asm = currentLoadContext.LoadFromStream(fs);
                    }
                    else
                    {
                        asm = currentLoadContext.LoadFromAssemblyPath(item);
                    }

                    ExtensionLibraries.Insert(0, new WeakReference<Assembly>(asm));
                    ClassLoader.AddAssembly(asm);

                }
            }
            catch
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Error Loading This Extension - {item}");
            }
        }
    }

    internal static Dictionary<string, GeneratorCommandType> GetCommands(PackageInformation packageInfo, string extensionName, CommandTypes commandTypes)
    {
        var commands = new Dictionary<string, GeneratorCommandType>();

        var config = Configurations.FirstOrDefault(x => x.ExtensionName == extensionName);
        if (config != null)
        {

            foreach (var item in ExtensionLibraries)
            {
                if (item.TryGetTarget(out Assembly assembly))
                {
                    if (assembly.GetName().Name.Equals(Path.GetFileNameWithoutExtension(config.LibraryFilename), StringComparison.OrdinalIgnoreCase))
                        foreach (var defType in assembly.DefinedTypes)
                        {
                            if (typeof(IExtensionGenerator).IsAssignableFrom(defType) && !defType.IsAbstract &&
                                defType.Name == config.SdkGeneratorName)
                            {
                                // Execute Generator
                                try
                                {
                                    IExtensionGenerator generator = ClassLoader.CreateType(defType, null) as IExtensionGenerator;
                                    generator.GetCommands(packageInfo, commands, commandTypes);
                                }
                                catch (Exception ex)
                                {
                                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, ex.Message);
                                }
                            }
                        }
                }
            }
        }

        return commands;
    }

    internal static void CreateAhsokaCommandLine(PackageInformation info, IProgress<CommandOutput> progress, string path)
    {
        Directory.CreateDirectory(path);

        string project = Path.Combine(path, "Ahsoka.CommandLine.csproj");
        CreateProject(info, project, info.ServiceInfo.RuntimeConfiguration.ExtensionInfo);

        progress.Report(new CommandOutput() { Data = "Restoring Nuget Packages" });
        ProcessUtility.RunProcessAsync("dotnet", "restore --no-cache", path, progress, CancellationToken.None);
        progress.Report(new CommandOutput() { Data = " " });

        progress.Report(new CommandOutput() { Data = "Creating Optimized Service Executable" });
        ProcessUtility.RunProcessAsync("dotnet", $"publish {project} -p:PublishProfile=Outputs", path, progress, CancellationToken.None);
        progress.Report(new CommandOutput() { Data = " " });
    }

    private static void CreateProject(PackageInformation packageInfo, string projectFile, List<ExtensionInfo> extensionInfos)
    {
        string pathToExtensionInfo = packageInfo.GetPackageExtentionFolder();

        var configs = Extensions.Configurations;

        StringBuilder references = new StringBuilder();

        string projectFileText = Resources.Ahsoka_CommandLine;
        XDocument doc = XDocument.Parse(projectFileText);
        var propGroup = doc.Descendants("PropertyGroup").FirstOrDefault();
        propGroup.Add(new XElement("Version", GitVersionInformation.FullSemVer));

        // If this is a released (non developer) version, update nuget to match.
        var packageGroup = doc.Descendants("ItemGroup").FirstOrDefault(x => x.Attributes().Any(y => y.Name == "Label" && y.Value == "Packages"));
        if (packageGroup != null)
        {
            var package = new XElement("PackageReference");
            package.Add(new XAttribute("Include", "Ahsoka.Core"));
            package.Add(new XAttribute("Version", GitVersionInformation.FullSemVer));
            packageGroup.Add(package);

            AssemblyLoadContext buildContext = new("BuildContext", true);


            foreach (var extension in extensionInfos)
            {
                var info = configs.FirstOrDefault(x => x.ExtensionName == extension.ExtensionName);

                package = new XElement("PackageReference");
                package.Add(new XAttribute("Include", info.PackageName));
                package.Add(new XAttribute("Version", info.InstalledVersion));
                packageGroup.Add(package);


                foreach (var item in Extensions.GetExtensionFiles(info.ExtensionName, false))
                {
                    if (item.EndsWith(info.PackageName + ".dll"))
                    {
                        var asm = buildContext.LoadFromAssemblyPath(item);
                        var typeToAdd = asm.ExportedTypes.First(x => !x.IsAbstract && !x.IsGenericType && x.IsPublic);
                        string loaderLine = $"        Ahsoka.Core.Extensions.AddPrivateExtension(typeof({typeToAdd.FullName}).Assembly);";
                        references.AppendLine(loaderLine);
                    }
                }
            }

            buildContext.Unload();
        }

        // Write Project
        doc.Save(projectFile);

        string programFileName = Path.Combine(Path.GetDirectoryName(projectFile), "Program.cs");
        string file = Properties.Resources.Program_cs;
        file = file.Replace("/*ClassLoaderAdds*/", references.ToString());
        File.WriteAllText(programFileName, file);

        // Write Program
        string outputFileName = Path.Combine(Path.GetDirectoryName(projectFile), "Properties", "PublishProfiles", "Outputs.pubxml");
        file = Properties.Resources.Publish_xmlpub;
        string runtimeID = packageInfo.PlatformFamily == PlatformFamily.OpenViewLinux ? "linux-arm" : "linux-arm64";
        file = file.Replace("<!--RUNTIMEID-->", runtimeID);

        Directory.CreateDirectory(Path.GetDirectoryName(outputFileName));
        File.WriteAllText(outputFileName, file);
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

