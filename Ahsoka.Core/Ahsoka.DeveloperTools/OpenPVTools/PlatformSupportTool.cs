using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using EnovationCloud.Core.DataTransferObjects;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        if (family == PlatformFamily.PowerVision)
        {
            this.IncludedProfiles = PluginsViewModel.profilePV;
        }
        else if (OperatingSystem.IsLinux())
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
                this.IncludedProfiles = PluginsViewModel.profileCSharp + "," + PluginsViewModel.profileWSL + "," + PluginsViewModel.profileCPP;
            else if (Name.ToLower().StartsWith(PlatformFamily.Ubuntu64.ToString().ToLower()))
                this.IncludedProfiles = PluginsViewModel.profileWSL + "," + PluginsViewModel.profileCPP;
            else
                this.IncludedProfiles = "None";
        }

        this.LocalPath = Path.Combine(CustomerToolPaths.GetToolPath(PlatformSupportPathInfo.PlatformSupportRootFolderName), Path.GetFileNameWithoutExtension(item.Path), item.Path);

        UpdateLocalTool();

        this.HasInfo = true;

    }

    public override string GetInfo(IToolViewModel vm)
    {
        // Look for "Default Plaform Support folder first (may be in the Repo instead of AppData).
        string localPath = string.Empty;

        try { localPath = Path.Combine(PlatformSupportPathInfo.GetPlatformSupportPath(PlatformFamily)); }
        catch { }

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

        string firmwareFileName = GetFirmwareVersionFile();
        if (firmwareFileName != null)
        {
            string firmwareFolder = localPath + "_Firmware";
            string imageFile = "image.*." + firmwareFileName.Replace("Firmware_", "");
            string fileName = Directory.EnumerateFiles(firmwareFolder, imageFile, SearchOption.AllDirectories).FirstOrDefault();
            string versionNumber = this.Version;

            if (File.Exists(fileName))
                versionNumber = Path.GetFileNameWithoutExtension(fileName).Replace("image.", "");

            builder.AppendLine($"- Firmware ({versionNumber})");
        }


        return builder.ToString();
    }

    private void UpdateLocalTool()
    {
        string imageFileName = string.Empty; ;


        // Find Path to Support Folder (Changes based on Running in Development)
        string firmwareFolder = string.Empty;
        try
        {
            try { firmwareFolder = Path.Combine(PlatformSupportPathInfo.GetPlatformSupportPath(PlatformFamily)); }
            catch { }

            if (!Path.Exists(firmwareFolder))
                firmwareFolder = Path.Combine(Path.GetDirectoryName(LocalPath)); // Fall back to Path passed in

            string firmwareFileName = GetFirmwareVersionFile(firmwareFolder);
            if (firmwareFileName != null)
            {
                string firmwareLocation = firmwareFolder + "_Firmware";
                string imageFile = "image.*." + firmwareFileName.Replace("Firmware_", "");
                imageFileName = Directory.EnumerateFiles(firmwareLocation, imageFile, SearchOption.AllDirectories).FirstOrDefault();
            }
        }
        catch { }

        if (File.Exists(imageFileName))
            if (PlatformFamily == PlatformFamily.OpenViewLinux)
                this.firmwareTool = new PlatformFirmwareTool(imageFileName, PlatformFamily);
            else if (PlatformFamily == PlatformFamily.OpenViewLinuxPro)
                this.firmwareTool = new PlatformProFirmwareTool(imageFileName, PlatformFamily);

        // Reload Hardware next time its requested.
        HardwareInfo.LoadHardwareInfo(CustomerToolPaths.GetToolPath("PlatformSupport"));
    }

    protected override async Task ExecuteDownload(IToolViewModel toolVM, CancellationTokenSource cancellationToken)
    {
        string value = Path.GetDirectoryName(LocalPath);

        if (Directory.Exists(value))
            Directory.Delete(value, true);

        await Task.Run(() =>
        {
            ExecuteDownloadInternal(toolVM, LocalPath, cancellationToken).GetAwaiter().GetResult();

            string firmwareFileName = GetFirmwareVersionFile();

            // Fetch Firmware if Not Available
            if (firmwareFileName != null)
            {
                string firmwareFolder = value + "_Firmware";
                string pathToDownload = Path.Combine(firmwareFolder, firmwareFileName);
                string pathToFolder = Path.Combine(firmwareFolder, Path.GetFileNameWithoutExtension(pathToDownload));
                if (!Directory.Exists(pathToFolder))
                {
                    ExecuteDownloadInternal(toolVM, pathToDownload, cancellationToken, false, pathToFolder).GetAwaiter().GetResult();
                    File.Delete(pathToDownload);
                }
            }

            UpdateLocalTool();
        });


    }

    private string GetFirmwareVersionFile(string overrideLocal = "")
    {
        string value = overrideLocal == string.Empty ? Path.GetDirectoryName(LocalPath) : overrideLocal;

        string fileName = Path.Combine(Path.Combine(value), this.PlatformFamily.ToString() + "_Firmware.zip.customertools.version");
        if (File.Exists(fileName))
            return File.ReadAllText(fileName).TrimEnd();
        else
            return null;
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
