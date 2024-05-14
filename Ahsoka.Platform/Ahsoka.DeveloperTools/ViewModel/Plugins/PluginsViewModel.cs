using Ahsoka.DeveloperTools.OpenPVTools;
using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.System;
using Ahsoka.Utility;
using Avalonia.Media;
using Avalonia.Threading;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;
internal class PluginsViewModel : ViewModelBase, IToolViewModel
{
    public const string profileCPP = "C++";
    public const string profileCSharp = "C#";
    public const string profileWSL = "WSL";

    #region Fields
    bool showNewInstallHelp = false;
    bool newInstallVisible = false;
    bool installHelpVisible = false;
    bool downloadInProgress = false;
    string percentText = "";
    int downloadPercent = 0;
    readonly MainViewModel parentViewModel;
    OpenPVToolBase selectedTool;
    CancellationTokenSource token;
    bool toolListDownloaded = false;
    private PlatformSupportPackage selectedPlatformSupport;
    ProfileDefinition selectedProfile;
    private ObservableCollection<PlatformSupportPackage> platformInfo = new();
    private ObservableCollection<ProfileDefinition> worflows = new();
    private ObservableCollection<OpenPVToolBase> tools = new();
    #endregion

    #region Properties
    public string VersionString { get { return $"Welcome to OpenPV Developer Toolkit Version {ParentViewModel.GetVersion()}"; } }

    MainViewModel IToolViewModel.MainViewModel { get { return parentViewModel; } }

    internal ObservableCollection<ProfileDefinition> ProfileDefinitions { get => worflows; set => worflows = value; }

    internal ProfileDefinition SelectedProfile
    {
        get { return selectedProfile; }
        set
        {
            if (selectedProfile != null)
                selectedProfile.IsSelected = false;

            selectedProfile = value;

            if (selectedProfile != null)
                selectedProfile.IsSelected = true;

            LoadProfileTools();

            OnPropertyChanged();
        }
    }

    internal PlatformSupportPackage SelectedPlatformSupport
    {
        get { return selectedPlatformSupport; }
        set
        {
            selectedPlatformSupport = value;
            OnPropertyChanged();
        }
    }

    internal ObservableCollection<PlatformSupportPackage> PlatformInfo { get => platformInfo; set => platformInfo = value; }

    internal ObservableCollection<OpenPVToolBase> SelectedDownloads { get => tools; set => tools = value; }

    public OpenPVToolBase SelectedTool
    {
        get { return selectedTool; }
        set
        {
            selectedTool = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanLaunch));
            OnPropertyChanged(nameof(CanInstall));
        }
    }

    public bool NewInstallVisible
    {
        get { return newInstallVisible; }
        set
        {
            newInstallVisible = value;
            OnPropertyChanged();
        }
    }

    public bool InstallHelpVisible
    {
        get { return installHelpVisible; }
        set
        {
            installHelpVisible = value;
            OnPropertyChanged();
        }

    }

    public bool DownloadInProgress
    {
        get { return downloadInProgress; }
        set
        {
            downloadInProgress = value;
            OnPropertyChanged();
        }
    }

    public int DownloadPercent
    {
        get { return downloadPercent; }
        set
        {
            downloadPercent = value;
            OnPropertyChanged();
        }
    }

    public string PercentStatus
    {
        get { return percentText; }
        set
        {
            percentText = value;
            OnPropertyChanged();
        }
    }

    public bool CanInstall { get { return SelectedTool != null && SelectedTool.CanInstall; } }

    public bool CanLaunch { get { return SelectedTool != null && SelectedTool.CanLaunch; } }

    public MainViewModel ParentViewModel => parentViewModel;

    public bool ToolListDownloaded { get => toolListDownloaded; set { toolListDownloaded = value; OnPropertyChanged(); } }

    #endregion

    #region Methods
    public PluginsViewModel(MainViewModel customerToolsViewModel)
    {
        this.parentViewModel = customerToolsViewModel;

        platformInfo.Add(JsonUtility.Deserialize<PlatformSupportPackage>(Properties.Resources.OpenViewPlatformSupport));

        ProfileDefinitions.Add(new ProfileDefinition()
        {
            ProfileName = "C# Developer Profile",
            ProfileTag = profileCSharp,
            ProfileDescription = "Develop in C# for Storyboard, GTK, Cairo and other User Interfaces.  Includes the OpenView Distribution along with Target and Development Support Plugins"
        });
        ProfileDefinitions.Add(new ProfileDefinition()
        {
            ProfileName = "C++ Developer Profile",
            ProfileTag = profileCPP,
            ProfileDescription = "Develop in C++ for OpenGL and Service Development scenarios. Includes Container Support (if Docker is installed) for Visual Studio Code as well as OpenView Distribution, Target and Development Support Plugins"
        });

        if (!OperatingSystem.IsMacOS()) // WSL is only supported on Windows / Ubuntu TODO: May be able to support this later.
            ProfileDefinitions.Add(new ProfileDefinition()
            {
                ProfileName = "WSL and Ubuntu Profile",
                ProfileTag = profileWSL,
                ProfileDescription = "Develop using QT Creator or WSL C++ Tools. Includes Container Support (if Docker is installed) for QT Creator as well as OpenView Distribution, Target and Development Support Plugins for Ubuntu and Windows"
            });

        selectedPlatformSupport = platformInfo.FirstOrDefault();
    }

    internal void OpenPopup() { OpenPopup(false); }

    internal void OpenPopup(bool showNewVersionHelp)
    {
        this.showNewInstallHelp = showNewVersionHelp;

        if (parentViewModel.Popup.RootPopup is Views.PluginsView)
            parentViewModel.Popup.DismissPopup();
        else
        {
            parentViewModel.Popup.DismissPopup();
            parentViewModel.Popup.ShowPopup(new Views.PluginsView() { DataContext = this });
        }


        if (parentViewModel.Popup.RootPopup is Views.PluginsView)
        {
            try
            {
                if (!ToolListDownloaded)
                    LoadItems();
            }
            catch (Exception ex)
            {
                parentViewModel.Dialog.ShowDialog("Error", ex.Message, "Ok");
            }
        }
    }

    internal void ClosePopup()
    {
        parentViewModel.Popup.DismissPopup();
    }

    internal void CloseNewInstallHelp()
    {
        NewInstallVisible = false;
    }
    #endregion

    #region DeveloperTools
    internal async void DownloadAll()
    {
        if (SelectedProfile == null) { _ = parentViewModel.Dialog.ShowDialog("Selection Error", "No Profile selected to download", "Ok"); return; }

        bool yesContinue = await parentViewModel.Dialog.ShowDialog("Continue?", "This process will download and install any missing components for the selected profile.  You may be prompted to install various components as the processes executes.  \r\nDo you wish to continue?", "Yes", "No");
        if (!yesContinue)
            return;


        DownloadInProgress = true;

        token = new CancellationTokenSource();

        var installs = new List<OpenPVToolBase>();
        foreach (var item in SelectedDownloads)
        {
            if (!item.IsDownloaded)
            {
                SelectedTool = item;
                PercentStatus = $"Downloading {item.Name}...";
                await item.DownloadTool(this, token);

                if (item.CanInstall) // Firmware Install Tool is Located in Displays.
                    installs.Add(item);
            }
        }

        foreach (var item in installs)
        {
            SelectedTool = item;
            PercentStatus = $"Installing {item.Name}...";
            await item.InstallTool(this);
        }

        foreach (var item in PlatformInfo)
            item.CheckDownload();

        DownloadInProgress = false;
    }

    internal async void DownloadTool()
    {
        if (SelectedTool == null) { _ = parentViewModel.Dialog.ShowDialog("Selection Error", "No tool selected to download", "Ok"); return; }

        DownloadInProgress = true;

        PercentStatus = $"Downloading {selectedTool.Name}...";

        token = new CancellationTokenSource();
        await SelectedTool.DownloadTool(this, token);

        foreach (var item in PlatformInfo)
            item.CheckDownload();

        DownloadInProgress = false;
    }

    internal async void RemoveTool()
    {
        if (SelectedTool == null) { _ = parentViewModel.Dialog.ShowDialog("Selection Error", "No tool selected to remove", "Ok"); return; }

        var continueRemove = await ParentViewModel.Dialog.ShowDialog("Remove Tool", "Are you sure you want remove the selected download?", "Yes", "Cancel");
        if (!continueRemove)
            return;

        SelectedTool.RemoveTool(this);
    }

    internal void RefreshTools()
    {
        LoadItems();
    }

    internal void ShowInstallHelp()
    {
        if (SelectedTool == null) { _ = parentViewModel.Dialog.ShowDialog("Selection Error", "No tool selected to install", "Ok"); return; }
        if (!SelectedTool.IsDownloaded) { _ = parentViewModel.Dialog.ShowDialog("Tool Not Ready", "Tool must be downloaded to install", "Ok"); return; }

        InstallHelpVisible = true;
    }

    internal void CancellInstall()
    {

        InstallHelpVisible = false;
    }

    internal async void InstallTool()
    {
        InstallHelpVisible = false;

        if (SelectedTool == null) { _ = parentViewModel.Dialog.ShowDialog("Selection Error", "No tool selected to install", "Ok"); return; }
        if (!SelectedTool.IsDownloaded) { _ = parentViewModel.Dialog.ShowDialog("Tool Not Ready", "Tool must be downloaded to install", "Ok"); return; }

        await SelectedTool.InstallTool(this);
    }

    internal async void LaunchTool()
    {
        if (SelectedTool == null) { _ = parentViewModel.Dialog.ShowDialog("Selection Error", "No tool selected to launch", "Ok"); return; }
        if (!SelectedTool.IsDownloaded) { _ = parentViewModel.Dialog.ShowDialog("Tool Not Ready", "Tool must be downloaded to launch", "Ok"); return; }

        await SelectedTool.LaunchTool(this);
    }

    internal async void LaunchProfile()
    {
        SelectedTool = SelectedDownloads.FirstOrDefault(x => x.CanLaunch);

        if (SelectedTool == null) { _ = parentViewModel.Dialog.ShowDialog("Selection Error", "No tool selected to launch", "Ok"); return; }
        if (!SelectedTool.IsDownloaded) { _ = parentViewModel.Dialog.ShowDialog("Tool Not Ready", "Tool must be downloaded to launch", "Ok"); return; }

        await SelectedTool.LaunchTool(this);
    }


    internal void Cancel()
    {
        token.Cancel();
    }

    #endregion

    #region Load Methods
    internal void LoadItems(bool wait = false)
    {
        ToolListDownloaded = false;
        ParentViewModel.IsWorking = true;

        var tasks = new List<Task>();
        foreach (var platformSupport in platformInfo)
        {
            platformSupport.Downloads.Clear();

            // Unpack Tools
            foreach (var item in platformSupport.Tools)
            {
                try
                {
                    var type = Type.GetType(item.BaseClass);
                    var selectedTool = (OpenPVToolBase)Activator.CreateInstance(type);

                    var version = VersionUtility.GetAppVersion();

                    selectedTool.LocalPath = Path.Combine(CustomerToolPaths.GetToolPath("DeveloperTools"), item.Name.Replace(" ", "_")); // Tool Download Path
                    selectedTool.Name = item.Name;
                    selectedTool.Description = item.Description;
                    selectedTool.Version = item.Version;
                    if (item.Resources != null)
                        selectedTool.Resources = new List<Resource>(item.Resources);

                    if (item.DevelopmentPlatform == PlatformFamily.Any)
                        AddTool(platformSupport, selectedTool);
                    else if (item.DevelopmentPlatform == PlatformFamily.MacosArm64 && OperatingSystem.IsMacOS())
                        AddTool(platformSupport, selectedTool);
                    else if (item.DevelopmentPlatform == PlatformFamily.Ubuntu64 && OperatingSystem.IsLinux())
                        AddTool(platformSupport, selectedTool);
                    else if (item.DevelopmentPlatform == PlatformFamily.Windows64 && OperatingSystem.IsWindows())
                        AddTool(platformSupport, selectedTool);
                }
                catch { } // Catch Tool Load Errors.
            }

            // Download Firmware Items
            tasks.Add(Task.Run(() =>
            {
                LoadSparkItems(platformSupport, platformSupport.DemoSparkChannelName, "Demos", platformSupport.DemoSparkVersionFilter);

                LoadSparkItems(platformSupport, platformSupport.ExtensionSparkChannelName, Extensions.ExtensionsFolder, platformSupport.ExtensionSparkVersionFilter);

                LoadSparkItems(platformSupport, platformSupport.PlatformSupportSparkChannelName, "Dependencies", platformSupport.PlatformSupportSparkVersionFilter);
            }));

        }

        var task = Task.Run(() =>
        {
            NewInstallVisible = showNewInstallHelp;

            Task.WaitAll(tasks.ToArray());

            ParentViewModel.IsWorking = false;
            ToolListDownloaded = true;
            showNewInstallHelp = false;
        });

        if (wait)
            task.Wait();
    }

    private void AddTool(PlatformSupportPackage platformSupport, OpenPVToolBase selectedTool)
    {
        if (SelectedProfile != null)
            if (selectedTool.IncludedProfiles.Contains(SelectedProfile.ProfileTag) || selectedTool.IncludedProfiles == "All")
                if (!SelectedDownloads.Any(x => x.Name == selectedTool.Name))
                    SelectedDownloads.Add(selectedTool);

        platformSupport?.Downloads.Add(selectedTool);
    }

    internal void LoadSparkItems(PlatformSupportPackage platformSupport, string channelName, string property, string filter)
    {
        var releases = new List<OpenPVToolBase>();
        try
        {
            var search = new ClientContainerSearchDTO() { ChannelName = channelName, ContainerVersionFilter = filter };
            var results = SparkAccountClient.SharedSparkClient.SearchContainers(search, 100);

            foreach (var result in results.OrderByDescending(x => x.UploadDate))
            {
                if (property == "Demos")
                {
                    ReleaseDTO token = SparkAccountClient.SharedSparkClient.GetDownloadToken(result.ReleaseID, DateTime.Now.AddMinutes(15));
                    releases.Add(new PlatformDemoTool(token));

                    break; // PUll Latest Demo's and Return;
                }
                else if (property == Extensions.ExtensionsFolder)
                {
                    ReleaseDTO token = SparkAccountClient.SharedSparkClient.GetDownloadToken(result.ReleaseID, DateTime.Now.AddMinutes(15));
                    releases.Add(new PlatformExtensionsTool(token));

                    break; // PUll Latest Demo's and Return;
                }
                else if (property == "Dependencies")
                {
                    ReleaseDTO token = SparkAccountClient.SharedSparkClient.GetDownloadToken(result.ReleaseID, DateTime.Now.AddMinutes(15));
                    AddDependencyItems(platformSupport, releases, token);
                }
            }
        }
        catch { throw; }

        Dispatcher.UIThread.Post(() =>
        {
            foreach (var release in releases)
                AddTool(platformSupport, release);
        });

    }

    private void AddDependencyItems(PlatformSupportPackage platformSupport, List<OpenPVToolBase> releases, ReleaseDTO token)
    {
        foreach (var item in token.Contents)
            releases.Add(new PlatformSupportTool(token, item));
    }

    internal void ShowSupportSite()
    {
        using var myProcess = new Process();
        string command = "explorer.exe";
        string args = "https://enovationcontrols.zendesk.com";
        if (OperatingSystem.IsLinux())
            command = "xdg-open";

        try
        {
            ProcessUtility.RunProcessInWindow(command, args, false);
        }
        catch (Exception)
        { _ = ParentViewModel.Dialog.ShowDialog("Launch Error", $"Unable to launch help at location '{args}'. Default Browser may not be configured.", "OK"); }
    }

    internal void LoadProfileTools()
    {
        SelectedDownloads.Clear();
        foreach (var item in SelectedPlatformSupport.Downloads)
            AddTool(null, item);

        SelectedProfile.CanLaunch = SelectedDownloads.Any(x => x.CanLaunch);


    }
    #endregion
}


internal class ProfileDefinition : ViewModelBase
{
    bool isSelected = false;
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

    bool canLaunch = false;
    public bool CanLaunch
    {
        get { return canLaunch; }
        set
        {
            canLaunch = value; OnPropertyChanged();
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



    internal string ProfileName { get; set; }
    internal string ProfileDescription { get; set; }
    internal string ProfileTag { get; set; }
}

