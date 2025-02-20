using Ahsoka.Core;
using Ahsoka.Core.Utility;
using EnovationCloud.Core.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal interface IToolViewModel
{
    int DownloadPercent { get; set; }

    MainViewModel MainViewModel { get; }
    string PercentStatus { get; set; }
}

internal class OpenPVToolBase : ViewModelBase
{
    bool downloadInGroup = false;

    public virtual PlatformFamily PlatformFamily { get; set; }

    public string IncludedProfiles { get; set; } = "All";

    public string LocalPath { get; set; }

    public string InstallText { get; set; }

    public string Name { get; set; }

    public string PackageType { get; set; }

    public string Description { get; set; }

    public PlatformFamily DevelopmentPlatform { get; set; }

    public string BaseClass { get; set; }

    public bool HasInfo { get; set; }

    public bool CanLaunch { get; set; }

    public bool CanInstall { get; set; }

    public string SparkChannelName { get; set; }

    public string SparkChannelFilter { get; set; }

    public virtual string Version { get; set; }

    public List<Resource> Resources { get; set; }

    public bool IsDownloaded
    {
        get { return GetIsDownloaded(); }
        set { }
    }

    public bool DownloadInGroup
    {
        get { return downloadInGroup; }
        set { downloadInGroup = value; }
    }


    public string NameDescription
    {
        get { return $"{Description} ({Version?.TrimEnd()})"; }
        set { }
    }

    public async Task DownloadTool(IToolViewModel toolVM, CancellationTokenSource token)
    {
        await ExecuteDownload(toolVM, token);
        OnPropertyChanged(nameof(IsDownloaded));
    }

    public virtual string GetInfo(IToolViewModel vm) { return string.Empty; }

    protected virtual bool GetIsDownloaded()
    {
        bool isDownloaded = Directory.Exists(LocalPath);
        if (isDownloaded && File.Exists(GetVersionFilePath()))
        {
            string fileVersion = File.ReadAllText(GetVersionFilePath());
            return fileVersion == Version;
        }

        return false;
    }

    protected virtual async Task ExecuteDownload(IToolViewModel toolVM, CancellationTokenSource token)
    {
        foreach (var item in Resources)
        {
            string path = Path.Combine(LocalPath, item.DownloadPath.Substring(item.DownloadPath.LastIndexOf("/") + 1));

            DownloadUtility download = new(item.DownloadPath, path);
            download.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
            {
                toolVM.DownloadPercent = (int)progressPercentage;
            };

            await download.StartDownload(token);
        }

        File.WriteAllText(GetVersionFilePath(), Version);
    }

    protected virtual string GetVersionFilePath()
    {
        return LocalPath + ".customertools.version";
    }

    public void RemoveTool(IToolViewModel toolVM)
    {
        OnRemove(LocalPath);

        OnPropertyChanged(nameof(IsDownloaded));
    }

    protected virtual void OnRemove(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);

        if (File.Exists(path))
            File.Delete(path);
    }

    public virtual Task InstallTool(IToolViewModel toolVM)
    {
        return null;
    }

    public virtual Task LaunchTool(IToolViewModel toolVM)
    {
        return null;
    }

    internal void CheckDownload()
    {
        OnPropertyChanged(nameof(IsDownloaded));
    }
}

public class Resource
{
    public string DownloadPath { get; set; }
}
