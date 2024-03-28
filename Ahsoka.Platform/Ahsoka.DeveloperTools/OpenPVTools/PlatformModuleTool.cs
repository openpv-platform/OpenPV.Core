using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.System.Hardware;
using EnovationCloud.Core.DataTransferObjects;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static NetMQ.NetMQSelector;

namespace Ahsoka.DeveloperTools;

internal class PlatformModuleTool : SparkToolBase
{
    const string Folder = "Modules";
    readonly ReleaseDTO viewModel;

    public static string GetModulePath()
    {
        return CustomerToolPaths.GetToolPath(Folder);
    }

    public PlatformModuleTool(ReleaseDTO viewModel) :
        base(viewModel, true)
    {
        this.viewModel = viewModel;
        this.PackageType = "Module";
        this.CanInstall = false;
        this.Description = viewModel.ChannelName;
        this.Name = viewModel.ChannelName;
        this.Version = viewModel.ContainerVersion;
        
        this.IncludedProfiles = "All";
        this.LocalPath = Path.Combine(GetModulePath(),"Temp");
    }

    protected override async Task ExecuteDownload(IToolViewModel toolVM, CancellationTokenSource cancellationToken)
    {
        if (Directory.Exists(GetModulePath()))
            Directory.Delete(GetModulePath(), true);

        toolVM.PercentStatus = "Downloading Modules...";
        foreach (var item in viewModel.Contents)
        {
            toolVM.PercentStatus = $"Downloading {item.Path}...";
            await ExecuteDownloadInternal(toolVM, Path.Combine(LocalPath, item.Path), cancellationToken);
            
        }
        File.WriteAllText(GetVersionFilePath(), Version);

    }

    protected override string GetVersionFilePath()
    {
        return Path.Combine(GetModulePath(), "Modules.customertools.version");
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
