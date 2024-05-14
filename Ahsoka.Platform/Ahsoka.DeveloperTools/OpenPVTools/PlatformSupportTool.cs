using Ahsoka.DeveloperTools.ViewModel;
using EnovationCloud.Core.DataTransferObjects;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace Ahsoka.DeveloperTools;

internal class PlatformSupportTool : SparkToolBase
{
    PlatformFirmwareTool firmwareTool = null;
    public const string VersionFileName = "VersionInfo.customertools.version";

    public PlatformFirmwareTool GetPlatformFirmwareTool()
    {
        return firmwareTool;
    }

    public PlatformSupportTool(ReleaseDTO viewModel, ReleaseFileDTO item) :
        base(viewModel, true)
    {

        this.PackageType = PlatformSupportPathInfo.PlatformSupportRootFolderName;
        this.CanInstall = false;
        this.Name = item.Path.Replace(".zip", "").Replace(".", " ").Replace("_", " ");
        this.Version = viewModel.ContainerVersion;
        this.Description = $"Development Package for {Name.Substring(0, Name.IndexOf(" "))}";

        // Update Platform 
        string platformFamily = item.Path.Substring(0, item.Path.IndexOf("_"));
        var family = PlatformFamily.Any;
        if (Enum.TryParse(platformFamily, out family))
            this.PlatformFamily = family;

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

        UpdateLocalTool();

        this.HasInfo = true;
        
    }

    public override string GetInfo()
    {
        // Look for "Default Plaform Support folder first (may be in the Repo instead of AppData).
        string localPath = Path.Combine(PlatformSupportPathInfo.GetPlatformSupportPath(PlatformFamily));
        if (!Path.Exists(localPath))
            localPath = Path.Combine(Path.GetDirectoryName(LocalPath)); // Fall back to Path passed in


        StringBuilder builder = new StringBuilder();
        foreach (var folder in Directory.GetDirectories(localPath))
        {
            string folderName = Path.GetFileName(folder);
            string versionFile = Path.Combine(folder, VersionFileName);
            string version = this.Version;
            
            if (File.Exists(versionFile))
                version = File.ReadAllText(versionFile);

            builder.AppendLine($"- {folderName} ({version})");
        }

        return builder.ToString();
    }


    private void UpdateLocalTool()
    {
        // Look for "Default Plaform Support folder first (may be in the Repo instead of AppData).
        string firmwareFolder = Path.Combine(PlatformSupportPathInfo.GetPlatformSupportPath(PlatformFamily),"Firmware");
        if (!Path.Exists(firmwareFolder))
            firmwareFolder = Path.Combine(Path.GetDirectoryName(LocalPath), "Firmware"); // Fall back to Path passed in

        if (Directory.Exists(firmwareFolder))
            this.firmwareTool = new PlatformFirmwareTool(firmwareFolder, PlatformFamily);

    }

    protected override async Task ExecuteDownload(IToolViewModel toolVM, CancellationTokenSource cancellationToken)
    {
        string value = Path.GetDirectoryName(LocalPath);
        if (Directory.Exists(value))
            Directory.Delete(value, true);

        await ExecuteDownloadInternal(toolVM, LocalPath, cancellationToken);

        UpdateLocalTool();
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
        await Task.CompletedTask;
    }
}
