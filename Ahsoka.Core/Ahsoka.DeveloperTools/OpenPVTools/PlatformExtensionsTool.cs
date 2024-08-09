using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.System;
using Ahsoka.Utility;
using EnovationCloud.Core.DataTransferObjects;
using EnovationCloud.Core.Utilities;
using Microsoft.Extensions.Configuration;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static NetMQ.NetMQSelector;

namespace Ahsoka.DeveloperTools;

internal class PlatformExtensionsTool : SparkToolBase
{
    public const string Folder =  "ExtensionInfo";
    readonly ReleaseDTO viewModel;
    readonly static List<string> frameworks = ["net8.0", "net6.0", "net5.0", "NETStandard2.1", "NETStandard2.0"];

    private static string GetExtensionsPath()
    {
        return CustomerToolPaths.GetToolPath(Folder);
    }
    
    public PlatformExtensionsTool(ReleaseDTO viewModel) :
        base(viewModel, false)
    {
        this.viewModel = viewModel;
        this.PackageType = System.Extensions.ExtensionsFolder;
        this.CanInstall = false;
        this.Description = viewModel.ChannelName.TrimEnd('s') + " Info";
        this.Name = viewModel.ChannelName.TrimEnd('s') + " Info";
        this.Version = viewModel.ContainerVersion;
       
        this.IncludedProfiles = PluginsViewModel.profileWSL + "," + PluginsViewModel.profileCPP + "," + PluginsViewModel.profileCSharp;
        this.LocalPath = GetExtensionsPath();
    }

    protected override async Task ExecuteDownload(IToolViewModel toolVM, CancellationTokenSource cancellationToken)
    {
        GetInfo(toolVM);

        if (Directory.Exists(this.LocalPath))
            Directory.Delete(this.LocalPath, true);

        toolVM.MainViewModel.AvailableExtensions.Clear();
        
        var download = viewModel.Contents.First();
        string downloadFile = Path.Combine(LocalPath, download.Path);
        toolVM.PercentStatus = $"Downloading {download.Path.Replace(".zip", "")}...";
        await ExecuteDownloadInternal(toolVM, downloadFile, cancellationToken, false);

        var archive = new ZipArchive(new MemoryStream(File.ReadAllBytes(downloadFile)));

        // Process the Nuget Files
        foreach (var item in archive.Entries)
        {
            var extensionInfoText = new StreamReader(item.Open()).ReadToEnd();
            var info = JsonUtility.Deserialize<ExtensionConfiguration>(extensionInfoText);

            if (info != null)
            {
                toolVM.MainViewModel.AvailableExtensions.Add(info);
                toolVM.PercentStatus = $"Fetching {info.ExtensionName}...";
                File.WriteAllText(Path.Combine(LocalPath, item.Name), extensionInfoText);

                await DownloadDemos(toolVM, info, cancellationToken);
            }
        }

        // Delete File
        File.Delete(downloadFile);

        // Write Version
        File.WriteAllText(GetVersionFilePath(), Version);
    }

    private async Task DownloadDemos(IToolViewModel toolVM, ExtensionConfiguration config, CancellationTokenSource cancellationToken)
    {
        string demoPath = Path.Combine(LocalPath, config.ExtensionName,"demos");

        if (Directory.Exists(demoPath))
            Directory.CreateDirectory(demoPath);

        var nugetVersion = new NuGetVersion(this.Version);

        var version = await GetVersionToDownload(config, config.PackageName, nugetVersion.IsPrerelease);

        await PlatformExtensionsTool.DownloadPackage(toolVM, cancellationToken, config.PackageName, version.FirstOrDefault(), false, demoPath, false, true);
    }
 
    protected override string GetVersionFilePath()
    {
        return Path.Combine(LocalPath, "Extensions.customertools.version");
    }

    protected override bool GetIsDownloaded()
    {
        bool isDownloaded = File.Exists(GetVersionFilePath());
        if (isDownloaded)
        {
            string fileVersion = File.ReadAllText(GetVersionFilePath());
            return fileVersion == this.Version;
        }

        return false;
    }

    public override async Task InstallTool(IToolViewModel toolVM)
    {
        // Do Nothing
        await Task.Run(() => { });
    }

    internal static void LoadExtensionList(IToolViewModel toolVM)
    {
        string pathToExtensionInfo = CustomerToolPaths.GetToolPath(Folder);
        if (Directory.Exists(pathToExtensionInfo))
        {
            foreach (var item in Directory.EnumerateFiles(pathToExtensionInfo, "*.extensioninfo.json"))
            {
                try
                {
                    string fileInfo = File.ReadAllText(item);
                    var info = JsonUtility.Deserialize<ExtensionConfiguration>(fileInfo);
                    if (!string.IsNullOrEmpty(info.PackageName))
                        toolVM.MainViewModel.AvailableExtensions.Add(info);
                }
                catch { }
            }
        }
    }

    internal static async Task<List<string>> GetVersionToDownload(ExtensionConfiguration configuration, string packageName, bool pullPreRelease)
    {
        var source = new PackageSource("https://api.nuget.org/v3/index.json");
        var providers = Repository.Provider.GetCoreV3();
        var repository = new SourceRepository(source, providers);

        var search = await repository.GetResourceAsync<PackageSearchResource>();
        var filter = new SearchFilter(pullPreRelease);

        var response = await search.SearchAsync($"packageid:{packageName}", filter, skip: 0, take: 20, NullLogger.Instance, CancellationToken.None);
        var versions = await response.First().GetVersionsAsync();
        var results = versions.Select(x => x.Version);

        NuGetVersionFloatBehavior behavior = pullPreRelease ? NuGetVersionFloatBehavior.PrereleaseMinor : NuGetVersionFloatBehavior.Minor;

        // Find the Best Version
        string thisVersion = null;

        var minVersion = new NuGetVersion(configuration.MinVersion);
        var maxVersion = new NuGetVersion(configuration.MaxVersion);
        var availableList = new VersionRange(minVersion, true, maxVersion, false, new FloatRange(behavior, minVersion, "")); 
       
        thisVersion = availableList.FindBestMatch(results)?.ToFullString();

        var listOfResults = results.OrderDescending().Where(x => availableList.Satisfies(x) && (x.IsPrerelease != true || x.IsPrerelease == pullPreRelease)).Select(x=>x.ToString()).ToList();
        return listOfResults;
    }

    internal static async Task DownloadPackage(IToolViewModel toolVM, CancellationTokenSource cancellationToken, string packageName, string version, bool ux, string directory, bool evaluateDeps, bool demoOnly)
    {
        Console.WriteLine($"Fetching {packageName} with version {version} for download");

        if (version != null)
            await DownloadNugetFiles(toolVM, cancellationToken, directory, packageName, version, null, evaluateDeps, demoOnly);
    }

    private static async Task DownloadNugetFiles(IToolViewModel toolVM, CancellationTokenSource cancellationToken, string directory, string package, string version, List<string> previousDownloads, bool evaluateDeps, bool demoOnly)
    {
        previousDownloads ??= new List<string>();

        string nugetFile = Path.Combine(directory, $"{package}.{version}.nupkg");
        string nugetUrl = $"https://www.nuget.org/api/v2/package/{package}/{version}";
        string libraryFile = package + ".dll";

        if (previousDownloads.Contains(package) || string.IsNullOrEmpty(package))
            return;

        previousDownloads.Add(package);

        using (DownloadUtility nugetDL = new(nugetUrl, nugetFile))
        {
            nugetDL.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                toolVM.DownloadPercent = (int)progressPercentage;
            };

            await nugetDL.StartDownload(cancellationToken);
        }

        using (var stream = new FileStream(nugetFile, FileMode.Open))
        using (var archive = new ZipArchive(stream))
        {
            // ExtractLibrary
            var libEntries = archive.Entries.Where(x => x.Name.ToLower().EndsWith(libraryFile.ToLower())).ToList();
            ZipArchiveEntry libEntry = null;
            
            foreach (var framework in frameworks)
            {
                libEntry = libEntries.FirstOrDefault(x => x.FullName.Contains(framework, StringComparison.CurrentCultureIgnoreCase));
                if (libEntry != null)
                    break;
            }

            if (package.StartsWith("Ahsoka") && demoOnly)
            {
                var demos = archive.Entries.Where(x => x.FullName.StartsWith("demo") && x.FullName.EndsWith(".zip"));
                foreach (var demo in demos)
                {
                    // Up One and Over 
                    string pathToDemos = Path.Combine(Path.GetDirectoryName(directory), "demos");
                    if (!Directory.Exists(pathToDemos))
                        Directory.CreateDirectory(pathToDemos);

                    demo.ExtractToFile(Path.Combine(pathToDemos, demo.Name));
                }
            }

            if (libEntry != null && !demoOnly)
            {
                libEntry.ExtractToFile(Path.Combine(directory, libraryFile), true);

                // TODO: Extract Any Include Files.
                if (evaluateDeps)
                {
                    var specEntry = archive.Entries.FirstOrDefault(x => x.Name.EndsWith("nuspec"));
                    string specFile = Path.Combine(directory, specEntry.Name);
                    specEntry.ExtractToFile(specFile, true);
                    var xmlDeps = XDocument.Load(specFile);

                    var dependencyGroups = xmlDeps.Document.Descendants().Where(x => x.Name.LocalName == "group");

                    var deps = FindBestGroup(dependencyGroups);

                    foreach (var item in deps)
                    {
                        string depId = item.Attribute("id").Value;
                        if (depId == "Ahsoka.Core")
                            continue;

                        string depVersion = item.Attribute("version").Value;

                        // Recurse and Download Files
                        await DownloadNugetFiles(toolVM, cancellationToken, directory, depId, depVersion, previousDownloads, evaluateDeps, false);
                    }

                    File.Delete(specFile);
                }
            }

        }

        File.Delete(nugetFile);


    }

    private static IEnumerable<XElement> FindBestGroup(IEnumerable<XElement> dependencyGroups)
    {
        XElement foundElement = null;
        foreach (var fw in frameworks)
        {
            foundElement = dependencyGroups.FirstOrDefault(x => x.Attribute("targetFramework").Value.StartsWith(fw, StringComparison.CurrentCultureIgnoreCase));
            if (foundElement != null)
                break;
        }

        if (foundElement == null)
            return new List<XElement>();

        return foundElement.Descendants().Where(x => x.Name.LocalName == "dependency");

    }


}
