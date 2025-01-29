using Ahsoka.Utility;
using EnovationCloud.Core.Utilities;
using System.Threading;

namespace Ahsoka.DeveloperTools.ViewModel;

internal class DownloadsViewModel : ChildViewModelBase<MainViewModel>
{

    CancellationTokenSource source;
    bool downloadInProgress = false;
    int downloadPercent = 0;
    string downloadStatus = string.Empty;

    internal bool DownloadInProgress
    {
        get { return downloadInProgress; }
        set
        {
            downloadInProgress = value;
            OnPropertyChanged();
        }
    }

    internal int DownloadPercent
    {
        get { return downloadPercent; }
        set
        {
            downloadPercent = value;
            OnPropertyChanged();
        }
    }

    internal string PercentStatus
    {
        get { return downloadStatus; }
        set
        {
            downloadStatus = value;
            OnPropertyChanged();
        }
    }

    public DownloadsViewModel(MainViewModel parentViewModel) :
      base(parentViewModel)
    {

    }

    public void ExecuteDownload(string description, string localPath, string downloadUrl)
    {
        source = new CancellationTokenSource();
        DownloadInProgress = true;

        PercentStatus = description;

        DownloadUtility download = new(downloadUrl, localPath);
        download.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
        {
            DownloadPercent = (int)progressPercentage;
        };

        download.StartDownload(source).Wait();

        DownloadInProgress = false;
    }

    public void Cancel()
    {
        source?.Cancel();
    }

}
