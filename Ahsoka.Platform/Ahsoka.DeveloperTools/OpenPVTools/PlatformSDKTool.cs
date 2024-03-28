using Ahsoka.DeveloperTools.ViewModel;
using EnovationCloud.Core.DataTransferObjects;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class PlatformSDKTool : SparkToolBase
{
    public PlatformSDKTool(ReleaseDTO viewModel, ReleaseFileDTO item) :
        base(viewModel, false)
    {
        this.IncludedProfiles = PluginsViewModel.profileCPP;
        this.PackageType = "SDK Install";
        this.CanInstall = true;
        this.CanLaunch = false;
        this.Version = viewModel.ContainerVersion;
        this.Name = viewModel.ReleaseName + " (C++ Toolchain)";
        this.InstallText = "This process will install the SDK on your local OS to support C++ compiling for the selected target platform.";
        this.LocalPath = Path.Combine(CustomerToolPaths.GetToolPath("Toolchain"), item.Path);
    }

    protected override async Task ExecuteDownload(IToolViewModel toolVM, CancellationTokenSource cancellationToken)
    {
        await ExecuteDownloadInternal(toolVM, LocalPath, cancellationToken);
    }

    public override async Task InstallTool(IToolViewModel toolVM)
    {

        string downloadFileName = LocalPath;
        string downloadDirectory = Path.GetDirectoryName(downloadFileName);

        string zipPath = Path.Combine(downloadDirectory, "TempExtract");
        ZipFile.ExtractToDirectory(downloadFileName, zipPath);

        List<string> files = Directory.GetFiles(zipPath, "*.sh", SearchOption.AllDirectories).ToList();
        string installScript = files.Where(x => x.ToLower().Contains("install")).First();
        string toolchain = files.Where(x => x.ToLower().Contains("toolchain")).First();

        await Task.Run(() =>
        {
            string file, args;
            if (OperatingSystem.IsLinux())
            {
                file = "/bin/bash";
                args = $"-c \"chmod 711 {installScript} && chmod 711 {toolchain} && sudo {installScript} {toolchain} \"";
            }
            else
            {
                _ = toolVM.MainViewModel.Dialog.ShowDialog("Platform Error", "C++ Toolkit not supported on Windows", "Ok");
                return;
            }
            ProcessUtility.RunProcessInWindow(file, args, true);
        }).ContinueWith(result =>
        {
            Directory.Delete(zipPath, true);
        });

    }
}
