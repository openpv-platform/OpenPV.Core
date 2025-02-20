using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.Installer;
using EnovationCloud.Core.DataTransferObjects;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class PlatformHelpTool : SparkToolBase
{
    public const string Folder = "APIHelp";
    readonly ReleaseDTO viewModel;
    readonly static List<string> frameworks = ["net8.0", "net6.0", "net5.0", "NETStandard2.1", "NETStandard2.0"];

    internal static string GetHelpPath()
    {
        return CustomerToolPaths.GetToolPath(Folder);
    }

    public PlatformHelpTool(ReleaseDTO viewModel) :
        base(viewModel, false)
    {
        this.viewModel = viewModel;
        this.PackageType = "OpenPV API Help";
        this.CanInstall = false;
        this.Description = viewModel.ChannelName;
        this.Name = viewModel.ChannelName;
        this.Version = viewModel.ContainerVersion;

        this.IncludedProfiles = PluginsViewModel.profileWSL + "," + PluginsViewModel.profileCPP + "," + PluginsViewModel.profileCSharp;
        this.LocalPath = GetHelpPath();
    }

    protected override async Task ExecuteDownload(IToolViewModel toolVM, CancellationTokenSource cancellationToken)
    {
        if (Directory.Exists(this.LocalPath))
            Directory.Delete(this.LocalPath, true);

        var download = viewModel.Contents.First();
        string downloadFile = Path.Combine(LocalPath, download.Path);
        toolVM.PercentStatus = $"Downloading {download.Path.Replace(".zip", "")}...";
        await ExecuteDownloadInternal(toolVM, downloadFile, cancellationToken, false);

        string extractPath = LocalPath;

        PackagingUtility.ExtractPackage(downloadFile, extractPath, null, null);

        // Delete File
        File.Delete(downloadFile);

        // Write Version
        File.WriteAllText(GetVersionFilePath(), Version);
    }

    protected override string GetVersionFilePath()
    {
        return Path.Combine(LocalPath, "HelpInfo.customertools.version");
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
}
