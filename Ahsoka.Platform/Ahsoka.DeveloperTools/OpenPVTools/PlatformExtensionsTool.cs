using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.System;
using Ahsoka.Utility;
using EnovationCloud.Core.DataTransferObjects;
using EnovationCloud.Core.Utilities;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ahsoka.DeveloperTools;

internal class PlatformExtensionsTool : SparkToolBase
{
    const string Folder = System.Extensions.ExtensionsFolder;
    readonly ReleaseDTO viewModel;
    static List<string> frameworks = new List<string>() { "net8.0", "net6.0", "net5.0", "NETStandard2.1" };

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

        this.IncludedProfiles = "All";
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

                await DownloadPackage(toolVM, cancellationToken, info.PackageName, libDirectory, true);


                if (info.HasUx)
                {
                    string uxDirectory = Path.Combine(rootDirectory, "ux");

                    if (!Directory.Exists(uxDirectory))
                        Directory.CreateDirectory(uxDirectory);

                    await DownloadPackage(toolVM, cancellationToken, info.UxPackageName, uxDirectory, false);
                }
            }
        }

        // Delete File
        File.Delete(downloadFile);

        // Write Version
        File.WriteAllText(GetVersionFilePath(), Version);
    }

    private async Task DownloadPackage(IToolViewModel toolVM, CancellationTokenSource cancellationToken, string packageName, string directory, bool evaluateDeps)
    {
        var source = new PackageSource("https://api.nuget.org/v3/index.json");
        var providers = Repository.Provider.GetCoreV3();
        var repository = new SourceRepository(source, providers);

        var search = await repository.GetResourceAsync<RawSearchResourceV3>();
        var filter = new SearchFilter(true);

        var response = await search.Search($"packageid:{packageName}", filter, skip: 0, take: 20, NullLogger.Instance, CancellationToken.None);
        var versions = response.SelectMany(x => x["versions"]);
        var nugetVersion = new NuGetVersion(this.Version);

        var results = versions.Select(x => new NuGetVersion((string)x.SelectToken("version")));
        var minVersion = new NuGetVersion(nugetVersion.Major, nugetVersion.Minor, 0);

        string thisVersion = null;
        if (versions.Any(x => x.SelectToken("version").ToString() == nugetVersion.ToString()))
            thisVersion = nugetVersion.ToFullString();
        else
           thisVersion = new VersionRange(minVersion, true, nugetVersion, true).FindBestMatch(results)?.ToFullString();

        if (thisVersion != null)
            await DownloadNugetFiles(toolVM, cancellationToken, directory, packageName, thisVersion, null, evaluateDeps);
    }

    private static async Task DownloadNugetFiles(IToolViewModel toolVM, CancellationTokenSource cancellationToken, string directory, string package, string version, List<string> previousDownloads, bool evaluateDeps)
    {
        if (previousDownloads == null)
            previousDownloads = new List<string>();

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

        using (FileStream stream = new FileStream(nugetFile, FileMode.Open))
        using (ZipArchive archive = new ZipArchive(stream))
        {
            // ExtractLibrary
            var libEntries = archive.Entries.Where(x => x.Name.ToLower().EndsWith(libraryFile.ToLower())).ToList();
            ZipArchiveEntry libEntry = null;

            foreach (var framework in frameworks)
            {
                libEntry = libEntries.FirstOrDefault(x => x.FullName.ToLower().Contains(framework.ToLower()));
                if (libEntry != null)
                    break;
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
            foundElement = dependencyGroups.FirstOrDefault(x => x.Attribute("targetFramework").Value.ToLower().StartsWith(fw));
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
