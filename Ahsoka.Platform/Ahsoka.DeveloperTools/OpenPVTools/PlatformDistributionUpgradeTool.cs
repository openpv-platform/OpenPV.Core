using Ahsoka.DeveloperTools.ViewModel;
using EnovationCloud.Core.DataTransferObjects;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class PlatformDistributionUpgradeTool : SparkToolBase
{
    public PlatformDistributionUpgradeTool(ReleaseDTO viewModel, ReleaseFileDTO item) :
        base(viewModel, true)
    {
        this.PackageType = "Target Distribution Upgrade Utility";
        this.Description = $"Distribution Upgrade for " + viewModel.ContainerName;
        this.CanInstall = false;
        this.Version = viewModel.ContainerVersion;
        this.Name = viewModel.ReleaseName + " (Firmware Upgrade)";
        this.CanLaunch = false;

        this.LocalPath = Path.Combine(CustomerToolPaths.GetToolPath("Upgrade"), item.Path);
    }

    protected override async Task ExecuteDownload(IToolViewModel toolVM, CancellationTokenSource cancellationToken)
    {
        string value = Path.GetDirectoryName(LocalPath);
        if (Directory.Exists(value))
            Directory.Delete(value, true);

        await ExecuteDownloadInternal(toolVM, LocalPath, cancellationToken);
    }

    protected override void OnRemove(string path)
    {
        // Delete the Whole Directory.
        path = Path.GetDirectoryName(path);
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }

    public override async Task InstallTool(IToolViewModel toolVM)
    {
        // Do Nothing
        await Task.Run(() => { });
    }
}
