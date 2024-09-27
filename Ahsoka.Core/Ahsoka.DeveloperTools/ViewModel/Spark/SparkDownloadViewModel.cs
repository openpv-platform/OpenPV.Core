using Ahsoka.Utility;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using File = System.IO.File;
using Path = System.IO.Path;

namespace Ahsoka.DeveloperTools.ViewModel;

internal class UpdatesViewModel : ChildViewModelBase<MainViewModel>
{
    List<SparkReleaseViewModel> releaseList;
    SparkReleaseViewModel selectedViewModel;
    bool show = false;
    bool updateAvailable = false;
    float opacity = 0.0f;

    internal bool UpdatesVisible
    {
        get { return show; }
        set
        {
            show = value;
            OnPropertyChanged();
        }
    }

    internal bool UpdateAvailable
    {
        get { return updateAvailable; }
        set
        {
            updateAvailable = value;
            OnPropertyChanged();
        }
    }

    internal float UpdatesOpacity
    {
        get { return opacity; }
        set
        {
            opacity = value;
            OnPropertyChanged();
        }
    }

    internal SparkReleaseViewModel SelectedRelease
    {
        get { return selectedViewModel; }
        set
        {
            if (selectedViewModel != null)
                selectedViewModel.IsSelected = false;

            selectedViewModel = value;

            if (selectedViewModel != null)
                selectedViewModel.IsSelected = true;

            OnPropertyChanged();
        }
    }

    public ObservableCollection<SparkReleaseViewModel> DownloadItems
    {
        get;
        set;
    } = new ObservableCollection<SparkReleaseViewModel>();

    public bool FiltersVisible
    {
        get => ParentViewModel.Settings.SparkFiltersVisible;
        set { ParentViewModel.Settings.SparkFiltersVisible = value; OnPropertyChanged(); ReFilter(); }
    }

    public string FilterText
    {
        get => ParentViewModel.Settings.SparkFilterString;
        set { ParentViewModel.Settings.SparkFilterString = value; OnPropertyChanged(); ReFilter(); }
    }

    public SparkTrackType FilterTrack
    {
        get => ParentViewModel.Settings.SparkFilterTrack;
        set { ParentViewModel.Settings.SparkFilterTrack = value; OnPropertyChanged(); ReFilter(); }
    }

    public List<SparkTrackType> TrackList { get; set; } = new List<SparkTrackType>() { SparkTrackType.None, SparkTrackType.Development, SparkTrackType.Canary, SparkTrackType.Beta, SparkTrackType.Release };

    public UpdatesViewModel(MainViewModel parentViewModel) :
        base(parentViewModel)
    {
        RefreshList();
    }

    internal void ShowUpdates()
    {
        UpdatesVisible = true;
        UpdatesOpacity = 1.0f;
    }

    internal async void HideUpdates(int timeBeforeHideMs = 0)
    {
        await Task.Delay(timeBeforeHideMs);
        UpdatesOpacity = 0.0f;
        await Task.Delay(1000);
        UpdatesVisible = false;
    }

    internal void ClosePopup()
    {
        if (!ParentViewModel.ProgressVisible)
            UpdatesVisible = false;

    }

    internal void RefreshList()
    {
        ParentViewModel.IsWorking = true;
        var releases = new List<SparkReleaseViewModel>();
        _ = Task.Run(() =>
        {
            try
            {
                if (SparkAccountClient.SharedSparkClient.CurrentAuthorization == null)
                    return;

                string channelName = string.Empty;

                if (OperatingSystem.IsMacOS())
                    channelName = "OpenPV for macOS";
                else if (OperatingSystem.IsLinux())
                    channelName = "OpenPV for Ubuntu";
                else
                    channelName = "OpenPV for Windows";

                var search = new ClientContainerSearchDTO() { ChannelName = channelName };
                var results = SparkAccountClient.SharedSparkClient.SearchContainers(search, int.MaxValue);

                if (results != null)
                {
                    DateTime buildDate = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;

                    string productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

                    foreach (var release in results)
                    {
                        releases.Add(new SparkReleaseViewModel(this, release, channelName)
                        {
                            IsInstalled = productVersion.StartsWith(release.Version),
                            IsAvailableOnline = true
                        });
                    }

                    if (!releases.Any(x => x.IsInstalled))
                    {
                        SparkContainerDTO dtoLocal = new()
                        {
                            Version = productVersion.Substring(0, productVersion.IndexOf("+")),
                            CreationDate = buildDate,
                            ReleaseID = Guid.Empty,
                            TrackType = SparkTrackType.None
                        };

                        releases.Add(new SparkReleaseViewModel(this, dtoLocal, channelName) { IsInstalled = true, IsAvailableOnline = false });
                    }
                }

                releases.Sort((x, y) => -x.UploadDate.CompareTo(y.UploadDate));
                releaseList = releases;

                ReFilter();
            }
            catch { throw; }

        });
    }

    internal void Install()
    {
        _ = Task.Run(() =>
        {
            try
            {
                ReleaseDTO token = SparkAccountClient.SharedSparkClient.GetDownloadToken(SelectedRelease.ReleaseDto.ReleaseID, DateTime.Now.AddMinutes(15));
                string fileExt = Environment.OSVersion.Platform == PlatformID.Unix ? ".deb" : ".msi";

                if (OperatingSystem.IsMacOS())
                    fileExt = ".zip";

                var fileItem = token.Contents.FirstOrDefault(x => x.Path.EndsWith(fileExt));
                string downloadUrl = fileItem.DownloadToken;

                string localPath = Path.GetTempFileName() + fileExt;

                ParentViewModel.Downloads.ExecuteDownload("Downloading OpenPV Update", localPath, fileItem.DownloadToken);

                if (ParentViewModel.Dialog.ShowDialog("Download Complete", "Your application is ready to install. Do you wish to close this version and begin the install?", "Yes", "No").GetAwaiter().GetResult())
                {
                    // Install the App 
                    if (File.Exists(localPath))
                    {
                        ParentViewModel.Settings.LastUpgradePath = localPath;
                        ParentViewModel.SaveSettings();

                        try
                        {
                            if (Environment.OSVersion.Platform == PlatformID.Unix)
                            {
                                ProcessStartInfo linuxInfo = new()
                                {
                                    UseShellExecute = true,
                                    FileName = "/bin/bash",
                                    Arguments = $"-c \"nohup xterm -fs 14 -fa DejaVuSansMono -e 'sudo apt install {localPath} && nohup OpenPV.DeveloperToolkit'\"",
                                };

                                Process.Start(linuxInfo);
                            }
                            if (OperatingSystem.IsMacOS())
                            {
                                string extactLocation = $"{Path.GetTempPath()}/.OpenPVInstall/Temp";

                                string installLocation = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/Applications";
                                if (!Directory.Exists(installLocation))
                                    Directory.CreateDirectory(installLocation);

                                string pathToApp = $"{installLocation}/OpenPV.DeveloperToolkit.app";
                                string pathToAppOld = $"{extactLocation}/.Old.OpenPV.DeveloperToolkit.app";
                                string pathToAppTemp = $"{extactLocation}/OpenPV.DeveloperToolkit.app";

                                // Delete Old Extract Folder
                                if (Directory.Exists(extactLocation))
                                    Directory.Delete(extactLocation, true);

                                // Extract the File and Clean any Junk if needed
                                ZipFile.ExtractToDirectory(localPath, extactLocation, true);

                                StringBuilder command = new StringBuilder("-c \"");

                                if (Directory.Exists(pathToApp))
                                    command.Append($"nohup mv {pathToApp} {pathToAppOld} && ");

                                command.Append($"nohup mv {pathToAppTemp} {pathToApp} && nohup open {pathToApp} && rm -rf {extactLocation}\"");

                                // We will now launch an install script and shut down.  THe script should relaunch the app.
                                // This script will move app to old / backup, move new app into place, launch app
                                ProcessStartInfo macStartInfo = new()
                                {
                                    UseShellExecute = true,
                                    FileName = "zsh",
                                    Arguments = command.ToString()
                                };

                                var process = Process.Start(macStartInfo);
                                Task.Delay(2000).Wait();

                            }
                            else
                            {
                                ProcessStartInfo windowsInfo = new()
                                {
                                    UseShellExecute = true,
                                    FileName = "msiexec",
                                    Arguments = $"/i \"{localPath}\" INSTALLDIR=\"{CreateExeFolderPath()}\" TARGETDIR=\"{CreateExeFolderPath()}\" /passive UPDATING_AUTOMATICALLY=1 /l* test.log",
                                };
                                var result = Process.Start(windowsInfo);
                            }
                        }
                        catch (Win32Exception ex)
                        {
                            const int ERROR_CANCELLED = 1223; //The operation was canceled by the user.

                            if (ex.NativeErrorCode == ERROR_CANCELLED)
                                ParentViewModel.Dialog.ShowDialog("Admin Required", "This installer requires Administrative rights to execute.", "Ok").GetAwaiter().GetResult();
                            else
                                throw;
                        }
                        catch (Exception ex)
                        {
                            ParentViewModel.Dialog.ShowDialog("Error Occurred During Install", ex.Message, "Ok").GetAwaiter().GetResult();
                            return;
                        }
                        
                        if (OperatingSystem.IsWindows())
                            Environment.Exit(0);
                        else
                            Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                                {
                                    lifetime.MainWindow.Close();
                                     
                                }
                            });
                    }
                }

                File.Delete(localPath);
            }
            catch (Exception) { }
        });
    }

    private string CreateExeFolderPath()
    {
        var exeFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "EnovationControls", SelectedRelease.ReleaseName.Replace(" ","_") + "_" + SelectedRelease.ContainerVersion.Replace(".","_"));

        // This should be the path to where the Binaries folder will go
        return exeFolder;
    }

    internal void Download()
    {
        _ = Task.Run(() =>
        {
            try
            {
                ReleaseDTO token = SparkAccountClient.SharedSparkClient.GetDownloadToken(SelectedRelease.ReleaseDto.ReleaseID, DateTime.Now.AddMinutes(15));
                string fileExt = Environment.OSVersion.Platform == PlatformID.Unix ? ".deb" : ".msi";
                var fileItem = token.Contents.FirstOrDefault(x => x.Path.EndsWith(fileExt));
                string downloadUrl = fileItem.DownloadToken;

                string localPath = ParentViewModel.FileSaveDialogAsync("Save File", fileExt, "Installer", fileItem.Path).GetAwaiter().GetResult();
                if (localPath != null)
                    ParentViewModel.Downloads.ExecuteDownload("Downloading OpenPV Update", localPath, fileItem.DownloadToken);
            }
            catch (Exception) { }
        });
    }

    internal void ShowNotes()
    {
        // Get the text from the ReleaseNotes field of the container
        ReleaseDTO token = SparkAccountClient.SharedSparkClient.GetDownloadToken(SelectedRelease.ReleaseDto.ReleaseID, DateTime.Now.AddMinutes(15));

        var notes = token.Contents.FirstOrDefault(x => x.Path == token.ReleaseNotes);

        if (notes != null)
        {
            _ = Task.Run(() =>
            {
                string tempFile = Path.GetTempFileName() + ".html";
                ParentViewModel.Downloads.ExecuteDownload("Downloading Help", tempFile, notes.DownloadToken);

                // Launch release notes HTML file using download token
                ParentViewModel.ShowHelpPath(tempFile, true);
            });
        }
        else
        {
            // ReleaseNotes are in the database instead of an HTML file.  Display them with Spark's RTF viewer
            ParentViewModel.Dialog.ShowDialog($"Release Notes for {SelectedRelease.ContainerVersion}",
                                            token.ReleaseNotes, "Ok");
        }
    }

    public string GetReleaseNotesURL(string fileName)
    {
        string releaseNotesURL = string.Empty;

        try
        {
            // Prepare DTO and download token
            ReleaseDTO release = SparkAccountClient.SharedSparkClient.GetDownloadToken(SelectedRelease.ReleaseDto.ReleaseID, DateTime.Now.AddMinutes(15));

            // Look in DTO for a file name that matches the ReleaseNotes text from the database
            foreach (ReleaseFileDTO files in release.Contents)
            {
                if (files.Path.Contains(fileName))
                {
                    // Found it, return the download token for the matching file
                    releaseNotesURL = files.DownloadToken;
                    return releaseNotesURL;
                }
            }
        }
        catch
        {
            // Do nothing ¯\_(ツ)_/¯

        }

        // Could not find a matching file, so return an empty string
        return releaseNotesURL;
    }

    internal void ToggleFilters()
    {
        FiltersVisible = !FiltersVisible;
    }

    private void ReFilter()
    {
        Dispatcher.UIThread.Post(() =>
        {
            DownloadItems.Clear();

            foreach (var release in releaseList)
                if (!FiltersVisible || release.IsInstalled ||
                   (release.ReleaseDto.TrackType >= FilterTrack &&
                   (String.IsNullOrEmpty(FilterText) || release.ContainerVersion.Contains(FilterText))))
                {
                    DownloadItems.Add(release);
                }

            ParentViewModel.Settings.SparkFilterTrack = FilterTrack;
            ParentViewModel.Settings.SparkFilterString = FilterText;
            ParentViewModel.Settings.SparkFiltersVisible = FiltersVisible;
            ParentViewModel.SaveSettings();

            if (releaseList.Count > 0)
                UpdateAvailable = !releaseList.FirstOrDefault().IsInstalled;

            ParentViewModel.IsWorking = false;
        });
    }
}


internal class SparkReleaseViewModel : ChildViewModelBase<UpdatesViewModel>
{
    #region Fields
    readonly SparkContainerDTO dto;
    private bool isSelected;
    private bool isAvailableOnline;
    private bool isInstalled;
    #endregion

    #region Properties
    public string LaunchPath
    {
        get
        {
            /*// Only works for side by side install paths
            if (Directory.Exists(Path.Combine(ParentViewModelObject?.RootFolder, ParentViewModelObject?.ReleaseFullName)))
            {
                if (Directory.GetFiles(Path.Combine(ParentViewModelObject?.RootFolder, ParentViewModelObject?.ReleaseFullName), "*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault() is string installerPath)
                {
                    return installerPath;
                }

            }*/
            return string.Empty;
        }
    }

    public bool IsAvailableOnline
    {
        get { return isAvailableOnline; }
        set { isAvailableOnline = value; OnPropertyChanged(); }
    }

    public bool IsSelected
    {
        get { return isSelected; }
        set
        {
            isSelected = value; OnPropertyChanged();
            OnPropertyChanged(nameof(IconColor));
            OnPropertyChanged(nameof(BackColor));
        }
    }

    public IBrush IconColor
    {
        get { return IsSelected ? Brushes.White : Brushes.DimGray; }
        set { }
    }

    public IBrush BackColor
    {
        get { return IsSelected ? Brushes.SteelBlue : Brushes.WhiteSmoke; }
        set { }
    }

    public string ReleaseName
    {
        get; internal set;
    }

    public string ContainerVersion
    {
        get { return dto.Version; }
    }

    public string TrackName
    {
        get
        {
            switch (dto.TrackType)
            {
                case SparkTrackType.Development:
                    return "Dev";
                case SparkTrackType.Canary:
                    return "Canary";
                case SparkTrackType.Beta:
                    return "Beta";
                case SparkTrackType.Release:
                    return "Release";
                case SparkTrackType.None:
                    return "Local";
                default:
                    break;
            }

            return dto.TrackType.ToString();
        }
    }

    public bool HasReleaseNotes
    {
        get => dto.HasReleaseNotes;
    }

    public string AppStatus
    {
        get
        {
            if (IsAvailableOnline)
                return IsInstalled ? "Installed, and available online" : "Available online";
            else
                return IsInstalled ? "Installed but not available online" : "Error";
        }
    }

    public DateTime UploadDate
    {
        get
        {
            return dto.UploadDate;
        }
    }

    public bool IsInstalled
    {
        get => isInstalled;
        set
        {
            if (isInstalled != value)
            {
                isInstalled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AppStatus));
            }
        }
    }

    public string InstallButtonText => (IsInstalled ? "Uninstall Release..." : "Install Release...");

    public SparkContainerDTO ReleaseDto { get => dto; }

    #endregion

    #region Methods
    internal SparkReleaseViewModel(UpdatesViewModel parentViewModel, SparkContainerDTO dto, string name)
        : base(parentViewModel)
    {
        this.dto = dto;
        this.ReleaseName = name;
    }
    #endregion
}
