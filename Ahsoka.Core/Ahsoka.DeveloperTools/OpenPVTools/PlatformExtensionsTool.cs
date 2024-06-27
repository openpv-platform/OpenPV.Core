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

namespace Ahsoka.DeveloperTools;

internal class PlatformExtensionsTool : SparkToolBase
{
    const string Folder = System.Extensions.ExtensionsFolder;
    readonly ReleaseDTO viewModel;
    readonly static List<string> frameworks = ["net8.0", "net6.0", "net5.0", "NETStandard2.1", "NETStandard2.0"];
    string currentVersionString = string.Empty;
    public static string GetExtensionsPath()
    {
        return CustomerToolPaths.GetToolPath(Folder);
    }

    public PlatformExtensionsTool(ReleaseDTO viewModel) :
        base(viewModel, false)
    {
        this.viewModel = viewModel;
        this.PackageType = System.Extensions.ExtensionsFolder;
        this.CanInstall = false;
        this.Description = viewModel.ChannelName;
        this.Name = viewModel.ChannelName;
        this.Version = viewModel.ContainerVersion;
        this.HasInfo = true;

        this.IncludedProfiles = PluginsViewModel.profileWSL + "," + PluginsViewModel.profileCPP + "," + PluginsViewModel.profileCSharp;
        this.LocalPath = GetExtensionsPath();
    }

    protected override async Task ExecuteDownload(IToolViewModel toolVM, CancellationTokenSource cancellationToken)
    {
        if (Directory.Exists(this.LocalPath))
            Directory.Delete(this.LocalPath, true);

        var download = viewModel.Contents.First();
        string downloadFile = Path.Combine(LocalPath, download.Path);
        toolVM.PercentStatus = $"Downloading {download.Path.Replace(".zip", "")}...";
        await ExecuteDownloadInternal(toolVM, downloadFile, cancellationToken);


        var archive = new ZipArchive(new MemoryStream(File.ReadAllBytes(downloadFile)));

        // Process the Nuget Files
        foreach (var item in archive.Entries)
        {
            var extensionInfoText = new StreamReader(item.Open()).ReadToEnd();
            var info = JsonUtility.Deserialize<ExtensionConfiguration>(extensionInfoText);
            if (info != null)
            {
                toolVM.PercentStatus = $"Fetching {info.ExtensionName}...";
                string rootDirectory = Path.Combine(LocalPath, info.ExtensionName);
                if (!Directory.Exists(rootDirectory))
                    Directory.CreateDirectory(rootDirectory);

                File.WriteAllText(Path.Combine(rootDirectory, item.Name), extensionInfoText);

                string libDirectory = Path.Combine(rootDirectory, "lib");

                if (!Directory.Exists(libDirectory))
                    Directory.CreateDirectory(libDirectory);

                await DownloadPackage(toolVM, cancellationToken, info, false,  libDirectory, true);


                if (info.HasUx)
                {
                    string uxDirectory = Path.Combine(rootDirectory, "ux");

                    if (!Directory.Exists(uxDirectory))
                        Directory.CreateDirectory(uxDirectory);

                    await DownloadPackage(toolVM, cancellationToken, info, true, uxDirectory, false);
                }
            }
        }

        // Delete File
        File.Delete(downloadFile);

        // Write Version
        File.WriteAllText(GetVersionFilePath(), Version);
    }

    public override string GetInfo(IToolViewModel toolVM)
    {
        if (currentVersionString != string.Empty)
            return currentVersionString;
        else
            currentVersionString = "Fetching List";
      
        var task =  Task.Run(async () =>
        {
            StringBuilder builder = new StringBuilder();

            CancellationTokenSource source = new();
            var download = viewModel.Contents.First();
            string tempFile = Path.Combine(Path.GetTempPath(), download.Path);

            await ExecuteDownloadInternal(toolVM, tempFile, source);

            var archive = new ZipArchive(new MemoryStream(File.ReadAllBytes(tempFile)));

            // Process the Nuget Files
            foreach (var item in archive.Entries)
            {
                var extensionInfoText = new StreamReader(item.Open()).ReadToEnd();
                var info = JsonUtility.Deserialize<ExtensionConfiguration>(extensionInfoText);
                if (info != null)
                    AddVersionInfo(builder, info);
            }

            currentVersionString = builder.ToString();
        }).ContinueWith((result)=> 
        {
            if (toolVM is PluginsViewModel changed)
                changed.UpdateShowInfo();
        });
        
        return currentVersionString;
    }

    private void AddVersionInfo(StringBuilder builder, ExtensionConfiguration configuration)
    {
        if (!string.IsNullOrEmpty(configuration.PackageName))
        {
            string version = GetVersionToDownload(configuration, configuration.PackageName).GetAwaiter().GetResult();
            builder.AppendLine($" - {configuration.PackageName} ({version})");
        }
    }

    private async Task DownloadPackage(IToolViewModel toolVM, CancellationTokenSource cancellationToken, ExtensionConfiguration configuration, bool ux, string directory, bool evaluateDeps)
    {
        string packageName = ux ? configuration.UxPackageName : configuration.PackageName;
        if (string.IsNullOrEmpty(packageName))
            return;

        string thisVersion = await GetVersionToDownload(configuration, packageName);

        Console.WriteLine($"Found {packageName} with version {thisVersion} for download");

        if (thisVersion != null)
            await DownloadNugetFiles(toolVM, cancellationToken, directory, packageName, thisVersion, null, evaluateDeps);

    }

    private async Task<string> GetVersionToDownload(ExtensionConfiguration configuration, string packageName)
    {
        var source = new PackageSource("https://api.nuget.org/v3/index.json");
        var providers = Repository.Provider.GetCoreV3();
        var repository = new SourceRepository(source, providers);

        var search = await repository.GetResourceAsync<RawSearchResourceV3>();
        var filter = new SearchFilter(true);

        var response = await search.Search($"packageid:{packageName}", filter, skip: 0, take: 20, NullLogger.Instance, CancellationToken.None);
        var versions = response.SelectMany(x => x["versions"]);

        var results = versions.Select(x => new NuGetVersion((string)x.SelectToken("version")));
        var nugetVersion = new NuGetVersion(this.Version);

        // Determine if we are running a pre-release version. If not, we will only include pre-release versions
        bool allowDevelop = nugetVersion.IsPrerelease;

        NuGetVersionFloatBehavior behavior = nugetVersion.IsPrerelease ? NuGetVersionFloatBehavior.PrereleaseMinor : NuGetVersionFloatBehavior.Major;

        // Find the Best Version
        string thisVersion = null;

        // For Internally Build Extensions, we require the matching version.
        if (configuration.RequireExactVersion)
        {
            // Use The Version that Matchers this Tool otherwise, we can't download
            if (versions.Any(x => x.SelectToken("version").ToString() == nugetVersion.ToString()))
                thisVersion = nugetVersion.ToFullString();
        }
        else
        {
            var minVersion = new NuGetVersion(configuration.MinVersion);
            var maxVersion = new NuGetVersion(configuration.MaxVersion);
            thisVersion = new VersionRange(minVersion, true, maxVersion, false, new FloatRange(behavior, minVersion, "")).FindBestMatch(results)?.ToFullString();
        }

        return thisVersion;
    }

    private static async Task DownloadNugetFiles(IToolViewModel toolVM, CancellationTokenSource cancellationToken, string directory, string package, string version, List<string> previousDownloads, bool evaluateDeps)
    {
        previousDownloads ??= new List<string>();

        string nugetFile = Path.Combine(directory, $"{package}.{version}.nupkg");
        string nugetUrl = $"https://www.nuget.org/api/v2/package/{package}/{version}";
        string libraryFile = package + ".dll";

        if (previousDownloads.Contains(package) || string.IsNullOrEmpty(package) )
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

            if (package.StartsWith("Ahsoka"))
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
        
            if (libEntry != null)
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
                        await DownloadNugetFiles(toolVM, cancellationToken, directory, depId, depVersion, previousDownloads, evaluateDeps);
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
            return fileVersion == Version;
        }

        return isDownloaded;

    }

    public override async Task InstallTool(IToolViewModel toolVM)
    {
        // Do Nothing
        await Task.Run(() => { });
    }


}
