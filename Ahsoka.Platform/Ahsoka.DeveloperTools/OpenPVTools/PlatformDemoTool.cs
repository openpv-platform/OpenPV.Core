using Ahsoka.DeveloperTools.ViewModel;
using EnovationCloud.Core.DataTransferObjects;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class PlatformDemoTool : SparkToolBase
{
    const string Folder = "Demos";
    readonly ReleaseDTO viewModel;

    public static string GetDemoPath()
    {
        return CustomerToolPaths.GetToolPath(Folder);
    }

    public PlatformDemoTool(ReleaseDTO viewModel) :
        base(viewModel, false)
    {
        this.viewModel = viewModel;
        this.PackageType = "Demo Project";
        this.CanInstall = false;
        this.Description = viewModel.ChannelName;
        this.Name = viewModel.ChannelName;
        this.Version = viewModel.ContainerVersion;

        this.IncludedProfiles = PluginsViewModel.profileWSL + "," + PluginsViewModel.profileCPP + "," + PluginsViewModel.profileCSharp;


        this.LocalPath = GetDemoPath();

    }

    protected override async Task ExecuteDownload(IToolViewModel toolVM, CancellationTokenSource cancellationToken)
    {
        if (Directory.Exists(this.LocalPath))
            Directory.Delete(this.LocalPath, true);

        toolVM.PercentStatus = "Downloading Demos...";
        foreach (var item in viewModel.Contents)
        {
            toolVM.PercentStatus = $"Downloading {item.Path.Replace(".zip", "")}...";
            await ExecuteDownloadInternal(toolVM, Path.Combine(LocalPath, item.Path), cancellationToken);
        }

        File.WriteAllText(GetVersionFilePath(), Version);

    }

    protected override string GetVersionFilePath()
    {
        return Path.Combine(LocalPath, "Demos.customertools.version");
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
