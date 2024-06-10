using Ahsoka.CustomerTools.ViewModel;
using EnovationCloud.Core.DataTransferObjects;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.CustomerTools;

internal class PlatformSupportTool : SparkToolBase
{
    public PlatformSupportTool(ReleaseDTO viewModel, ReleaseFileDTO item) :
        base(viewModel, true)
    {

        this.PackageType = PlatformSupportPathInfo.PlatformSupportRootFolderName;
        this.CanInstall = false;
        this.Name = item.Path.Replace(".zip", "").Replace(".", " ").Replace("_", " ");
        this.Version = viewModel.ContainerVersion;
        this.Description = $"Development Package for {Name.Substring(0, Name.IndexOf(" "))}";

        // Filter Non Development Platforms
        if (OperatingSystem.IsLinux())
        {
            if (Name.StartsWith(PlatformFamily.OpenViewLinux.ToString()) || Name.StartsWith(PlatformFamily.Ubuntu64.ToString()))
                this.IncludedProfiles = PluginsViewModel.profileWSL + "," + PluginsViewModel.profileCPP;
            else
                this.IncludedProfiles = "None";
        }
        else if (OperatingSystem.IsMacOS())
        {
            if (Name.StartsWith(PlatformFamily.OpenViewLinux.ToString()))
                this.IncludedProfiles = PluginsViewModel.profileWSL + "," + PluginsViewModel.profileCPP + "," + PluginsViewModel.profileCSharp;
            else if (Name.ToLower().StartsWith(PlatformFamily.MacosArm64.ToString().ToLower()))
                this.IncludedProfiles = PluginsViewModel.profileCSharp;
            else if (Name.StartsWith(PlatformFamily.Ubuntu64.ToString()))
                this.IncludedProfiles = PluginsViewModel.profileCPP;
            else
                this.IncludedProfiles = "None";
        }
        else
        {
            if (Name.StartsWith(PlatformFamily.OpenViewLinux.ToString()) || Name.StartsWith(PlatformFamily.Windows64.ToString()))
                this.IncludedProfiles = PluginsViewModel.profileCSharp;
            else if (Name.ToLower().StartsWith(PlatformFamily.Ubuntu64.ToString().ToLower()))
                this.IncludedProfiles = PluginsViewModel.profileWSL + "," + PluginsViewModel.profileCPP;
            else
                this.IncludedProfiles = "None";
        }



        this.LocalPath = Path.Combine(CustomerToolPaths.GetToolPath(PlatformSupportPathInfo.PlatformSupportRootFolderName), Path.GetFileNameWithoutExtension(item.Path), item.Path);
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
