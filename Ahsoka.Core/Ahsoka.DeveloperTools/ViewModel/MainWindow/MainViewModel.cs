using Ahsoka.Core;
using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility;
using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.ViewModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class MainViewModel : ViewModelBase
{
    #region Fields
    bool progressVisibility = false;
    bool loggedIn = false;
    ICanCancel cancelObject;
    PackageViewModel selectedPackage;
    string statusText;
    bool isOffline = false;
    bool isExit = false;
    public bool showNewVersionHelp = false;
    bool isTalkingToSpark = false;
    readonly bool modelReady = false;
    ObservableCollection<RecentItem> recentItems;
    #endregion

    #region Properties
    public static string StartupArg { get; internal set; }

    public TopLevel RootVisual { get; set; }

    public bool IsWorking
    {
        get { return isTalkingToSpark; }
        set { OnSetValue(ref isTalkingToSpark, value); }
    }

    internal bool AhsokaToolsReady
    {
        get { return true; }
        set { }
    }


    public ObservableCollection<ExtensionConfiguration> AvailableExtensions
    {
        get;
        init;
    } = new();

    public RecentItem SelectedProject { get; set; }

    public Settings Settings
    {
        get;
        private set;
    }

    internal ActiveConnectionViewModel ActiveConnection
    {
        get;
        private set;
    }

    internal PopupViewModel Popup
    {
        get;
        private set;
    }

    internal DownloadsViewModel Downloads
    {
        get;
        private set;
    }

    internal DialogViewModel Dialog
    {
        get;
        private set;
    }

    internal ConsoleViewModel Console
    {
        get;
        private set;
    }

    internal UpdatesViewModel Updates
    {
        get;
        private set;
    }

    public ConnectionsViewModel Connections
    {
        get;
        private set;
    }

    public SparkLoginViewModel SparkLogin
    {
        get;
        private set;
    }

    public PluginsViewModel Plugins
    {
        get;
        private set;
    }

    public AboutViewModel About
    {
        get;
        private set;
    }

    internal PackageViewModel SelectedPackage
    {
        get { return selectedPackage; }
        set
        {
            selectedPackage = value;

            selectedPackage?.InitModel();

            OnPropertyChanged();
            OnPropertyChanged(nameof(Title));
        }
    }

    public string Title
    {
        get { return "OpenPV Developer Toolkit" + " - " + GetVersion() + (SelectedPackage != null ? " (" + SelectedPackage.PackageName + ")" : ""); }
        set
        {
        }
    }

    public bool LoggedIn
    {
        get { return loggedIn; }
        set
        {
            loggedIn = value;
            OnPropertyChanged();
            LoginCompleted();
        }
    }

    private void LoginCompleted()
    {
        ProgressVisible = true;
        Task.Run(() =>
        {
            StatusText = "Loading Projects...";
            TeamsViewModel?.LoadTeams()?.Wait();

        }).ContinueWith((t) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Path.Exists(StartupArg))
                    OpenProject(StartupArg);
            });

            ProgressVisible = false;
        });
    }

    public string GetVersion()
    {
        return Assembly.GetEntryAssembly().GetName().Version.ToString();
    }

    public bool ProgressVisible
    {
        get { return progressVisibility; }
        private set
        {
            IsWorking = value;
            progressVisibility = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get { return statusText; }
        set { statusText = value; OnPropertyChanged(); }
    }

    public bool IsExit
    {
        get { return isExit; }
        set { isExit = value; OnPropertyChanged(); }
    }

    public SparkProjectEditorViewModel TeamsViewModel
    {
        get;
        set;
    }

    public bool IsOffline
    {
        get { return isOffline; }
        set { isOffline = value; OnPropertyChanged(); }
    }

    public ObservableCollection<RecentItem> RecentItems { get => recentItems; set => recentItems = value; }

    public bool UseInternalCommandLine
    {
        get { return Settings.UseInternalCLI; }
        set
        {
            Settings.UseInternalCLI = value;
            SaveSettings();
            OnPropertyChanged();
        }
    }

    public bool DebugCommandLine
    {
        get { return Settings.WaitForDebuggerInCLI; }
        set
        {
            Settings.WaitForDebuggerInCLI = value;
            SaveSettings();
            OnPropertyChanged();
        }
    }


    #endregion

    #region Progress Management
    public void StartProcess(ICanCancel cancelObject = null)
    {
        if (ProgressVisible)
            return;

        Console.ShowConsole();
        ProgressVisible = true;
        this.cancelObject = cancelObject;
    }

    public void FinishProcess(string result, bool closeConsole)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!string.IsNullOrEmpty(result))
                Console.AddConsoleLine(result);

            if (closeConsole)
                Console.HideConsole();

            ProgressVisible = false;
            this.cancelObject = null;
        });
    }

    internal void Cancel()
    {
        this.cancelObject?.Cancel();
    }

    internal void RunCommandLineProcess(string processArgs, string workingDirectory, IProgress<CommandLineProgressInfo> progressString, CancellationToken token, string pathToCommand = null)
    {
        if (pathToCommand == null)
            pathToCommand = GetCommandLinePath(); // Will not have extensions

        if (UseInternalCommandLine)
        {
            LocalCommandLine.RunWithLocalConsole(processArgs, workingDirectory, progressString);
        }
        else
        {

            IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
            {
                progressString.Report(new CommandLineProgressInfo() { Message = info.Data, IsWriteLine = true });
            });

            if (OperatingSystem.IsLinux())
                ProcessUtility.RunProcessAsync("chmod", "+x " + pathToCommand, workingDirectory, progressText, token);

            ProcessUtility.RunProcessAsync(pathToCommand, processArgs, workingDirectory, progressText, token);
        }
    }

    internal void RunShellProcess(string command, string workingDirectory, IProgress<CommandLineProgressInfo> progressString, CancellationToken token)
    {
        string pathToCommand = command.Substring(0, command.IndexOf('"', 2) + 1).Trim('"');
        string processArgs = command.Substring(command.IndexOf('"', 2) + 2);

        progressString.Report(new CommandLineProgressInfo() { Message = $"Executing Shell Command from...", IsWriteLine = true });
        progressString.Report(new CommandLineProgressInfo() { Message = $"   {pathToCommand}", IsWriteLine = true });
        progressString.Report(new CommandLineProgressInfo() { Message = $" ", IsWriteLine = true });
        progressString.Report(new CommandLineProgressInfo() { Message = $"With Arguments...", IsWriteLine = true });
        progressString.Report(new CommandLineProgressInfo() { Message = $"   {processArgs}", IsWriteLine = true });
        progressString.Report(new CommandLineProgressInfo() { Message = $" ", IsWriteLine = true });

        IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
        {
            progressString.Report(new CommandLineProgressInfo() { Message = info.Data, IsWriteLine = true });
        });

        if (OperatingSystem.IsLinux())
            ProcessUtility.RunProcessAsync("chmod", "+x " + pathToCommand, workingDirectory, progressText, token);

        ProcessUtility.RunProcessAsync(pathToCommand, processArgs, workingDirectory, progressText, token);
    }


    public string GetPlatformPath(PlatformFamily family)
    {
        string pathToPlatformRoot;
        if (Debugger.IsAttached)
        {
            try
            {
                // Run Publish Outputs found in code base
                pathToPlatformRoot = PlatformSupportPathInfo.GetPlatformSupportPath(family);

                if (Directory.Exists(pathToPlatformRoot))
                    return pathToPlatformRoot;
            }
            catch { }
        }

        // Run Machine / Tools Downloads
        return Path.Combine(CustomerToolPaths.GetToolPath(PlatformSupportPathInfo.PlatformSupportRootFolderName),
                                PlatformSupportPathInfo.GetPlatformFolderName(family));
    }

    public string GetCommandLinePath()
    {
        string pathToCommand = string.Empty;

        // Run Machine / Tools Downloads
        string pathToKits = CustomerToolPaths.GetToolPath(PlatformSupportPathInfo.PlatformSupportRootFolderName);

        // Run Local Command Line for Tests
        if (Debugger.IsAttached && Directory.Exists(PlatformSupportPathInfo.PlatformSupportRootFolder))
            pathToKits = PlatformSupportPathInfo.PlatformSupportRootFolder;

        pathToCommand = Path.Combine(pathToKits, PlatformSupportPathInfo.GetPlatformFolderName(SystemInfo.CurrentPlatform), "CommandLine", "Ahsoka.CommandLine");

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            pathToCommand += ".exe";

        return pathToCommand;
    }

    #endregion

    #region File and Folder Selection
    internal async Task<string> FolderLocatorAsync(string dialogTitle, string defaultPath = null)
    {
        var file = await RootVisual.StorageProvider.TryGetFolderFromPathAsync(defaultPath);
        string path = null;
        var dialog = new FolderPickerOpenOptions
        {
            SuggestedStartLocation = file,
            Title = dialogTitle
        };

        try
        {
            var result = await RootVisual.StorageProvider.OpenFolderPickerAsync(dialog);

            if (result != null)
                path = result.FirstOrDefault()?.TryGetLocalPath();
        }
        catch { }

        return path;
    }

    internal async Task<string> FileLocatorAsync(string dialogTitle, string extension, string fileType, string currentFile = "")
    {
        var folder = await RootVisual.StorageProvider.TryGetFolderFromPathAsync(Path.GetDirectoryName(currentFile));

        string path = null;
        var dialog = new FilePickerOpenOptions
        {
            Title = dialogTitle,
            SuggestedStartLocation = folder
        };

        if (extension != "")
            dialog.FileTypeFilter = new List<FilePickerFileType>{ new(fileType)
            {
                Patterns = new[]{$"*.{extension}"}
            }};

        dialog.AllowMultiple = false;
        var result = await RootVisual.StorageProvider.OpenFilePickerAsync(dialog);

        if (result != null)
            path = result.FirstOrDefault()?.TryGetLocalPath();

        return path;
    }

    internal async Task<string> FileSaveDialogAsync(string dialogTitle, string extension, string fileType, string currentFile = "")
    {
        var file = await RootVisual.StorageProvider.TryGetFolderFromPathAsync(currentFile);

        string path = null;
        var dialog = new FilePickerSaveOptions
        {
            Title = dialogTitle,
            SuggestedStartLocation = file,
            SuggestedFileName = Path.GetFileName(currentFile)
        };
        if (extension != "")
            dialog.FileTypeChoices = new List<FilePickerFileType>{ new(fileType)
            {
                Patterns = new[]{$"*.{extension}"}
            }};

        dialog.Title = dialogTitle;
        var result = await RootVisual.StorageProvider.SaveFilePickerAsync(dialog);
        if (result != null)
            return result.TryGetLocalPath();

        return path;

    }
    #endregion

    #region Methods
    public MainViewModel(TopLevel topLevel)
    {
        this.RootVisual = topLevel;

        // Reload Hardware next time its requested.
        HardwareInfo.LoadHardwareInfo(CustomerToolPaths.GetToolPath("PlatformSupport"));

        IsOffline = !IsSparkAvailable();

        LoadSettings();
        About = new(this);
        Console = new(this);
        Dialog = new();
        Popup = new(this);
        ActiveConnection = new(this);
        Plugins = new(this);
        TeamsViewModel = new(this);
        Connections = new(this);
        SparkLogin = new(this);
        Updates = new(this);
        Downloads = new(this);

        modelReady = true;

        if (!LoggedIn)
            Popup.ShowPopup(new Views.SparkLoginView() { DataContext = SparkLogin });
        else
            InitUI();
    }


    private bool IsSparkAvailable()
    {
        try
        {
            /// Ping Spark Prod to see if we have internet
            using var ping = new Ping();
            var reply = ping.Send("enovation.powervisionspark.com", 5000);
            if (reply != null && reply.Status == IPStatus.Success)
                return true;
        }
        catch { }

        return false;
    }

    internal async void InitUI()
    {
        if (modelReady)
        {
            if (IsOffline)
            {
                var result = await Dialog.ShowDialog("Offline Mode", "You are not currently connected to the internet or can not connect to our cloud services.  Do you wish to enable Offline Mode or Exit?", "Enable Offline", "Exit");
                if (!result)
                    Environment.Exit(0);
                else
                    await Dialog.ShowDialog("Offline Mode Enabled", "You can open and build Packages but can not acess Projects, Plugins or Account Information.  Please restart the Developer Toolkit to re-enable these features.", "OK");

            }
            else if (showNewVersionHelp)
                Plugins.OpenPopup(showNewVersionHelp);

            // Fix Crash on Initial Login
            if (!IsOffline)
                Updates.RefreshList();

        }
    }

    internal void ShowHelp()
    {
        string helpFile = Path.Combine(PlatformHelpTool.GetHelpPath(), "html", "index.html");
        if (!File.Exists(helpFile))
        {
            var result = Dialog.ShowDialog("Help Not Found", "Help file could not be found, Please download the 'OpenPV Help' Plugin", "OK");
            return;
        }

        ShowHelpPath(helpFile, true);
    }

    internal void ShowHelpPath(string helpFile, bool isLocal)
    {
        using var myProcess = new Process();
        string command = "explorer.exe";
        string args = $"\"{helpFile}\"";
        string folder = isLocal ? Path.Combine(Path.GetDirectoryName(helpFile)) : Environment.CurrentDirectory;

        if (OperatingSystem.IsLinux())
            command = "xdg-open";
        else if (OperatingSystem.IsMacOS())
            command = "open";

        try
        {
            ProcessUtility.RunProcessInWindow(command, args, false, folder);
        }
        catch (Exception)
        { _ = Dialog.ShowDialog("Launch Error", $"Unable to launch api help at location '{args}'. Default Browser may not be configured.", "OK"); }
    }

    internal void OpenPackage()
    {
        if (Popup.RootPopup is Views.OpenPackageView)
            Popup.DismissPopup();
        else
        {
            if (RecentItems.Count > 0)
            {
                // Clear Missing Items
                foreach (var item in RecentItems.ToArray())
                    if (!File.Exists(item.FilePath))
                        RecentItems.Remove(item);

                Popup.DismissPopup();
                Popup.ShowPopup(new Views.OpenPackageView() { DataContext = this });
            }
            else
                BrowsePackage();
        }
    }

    internal async void BrowsePackage()
    {
        var storageProvider = RootVisual.StorageProvider;

        var dialog = new FilePickerOpenOptions
        {
            Title = "Select an Ahsoka Package File",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("All Package Files")
                {
                    Patterns = new[]{$"*.{PackageViewModel.packageInfoExtension}", $"*.{PackageViewModel.opvpackageExtension}" }
                },
                new("OpenView Package")
                {
                    Patterns = new[]{$"*.{PackageViewModel.opvpackageExtension}" }
                }
                ,
                new("Package Info")
                {
                    Patterns = new[]{$"*.{PackageViewModel.packageInfoExtension}" }
                }
            }
        };

        var result = await storageProvider.OpenFilePickerAsync(dialog);

        if (result != null)
            foreach (var item in result)
            {
                PromptSave();
                try
                {
                    OpenProject(item.TryGetLocalPath());
                }
                catch (Exception)
                { _ = Dialog.ShowDialog("Invalid Project", "An error occured while opening this Package File. File is corrupted or not a valid PackageInfo file.", "OK"); }
            }


    }

    private void OpenProject(string path)
    {
        Popup.DismissPopup();

        SelectedPackage = new PackageViewModel(this, path);

        UpdateRecentItem(path);
    }

    private void UpdateRecentItem(string path)
    {
        // Clear Invalid Items
        foreach (var deleteItem in Settings.RecentItems.ToArray())
            if (!File.Exists(deleteItem.FilePath))
                Settings.RecentItems.Remove(deleteItem);

        var item = RecentItems.FirstOrDefault(x => x.FilePath == path);
        if (item == null)
        {
            Settings.RecentItems.Insert(0, new RecentItem() { FilePath = path, Name = SelectedPackage.PackageName, Description = $"{SelectedPackage.PackageDescription} ({SelectedPackage.ApplicationVersion})" });
            RecentItems = new ObservableCollection<RecentItem>(Settings.RecentItems.OrderByDescending(x => x.IsFavorite));
            this.SaveSettings();
        }
        else
        {
            item.Name = SelectedPackage.PackageName;
            item.Description = $"{SelectedPackage.PackageDescription} ({SelectedPackage.ApplicationVersion})";
            Settings.RecentItems.Remove(item);
            Settings.RecentItems.Insert(0, item);
            RecentItems = new ObservableCollection<RecentItem>(Settings.RecentItems.OrderByDescending(x => x.IsFavorite));
            this.SaveSettings();
        }
    }

    internal async void PromptSave()
    {
        if (CheckForPackageChanges())
        {
            bool result;
            if (isExit)
                result = await Dialog.ShowDialog("Save Package", "Do you want to save package changes before exiting?", "Save", "Exit");
            else result = await Dialog.ShowDialog("Save Package", "Do you want to save package changes before closing?", "Save", "Close");
            if (result)
            {
                SavePackage();
            }
        }
        if (isExit)
            Environment.Exit(0);

        SelectedPackage?.Close();
        SelectedPackage = null;
    }

    internal void SavePackage()
    {
        if (SelectedPackage.Validate())
        {
            SelectedPackage.SavePackage();

            UpdateRecentItem(selectedPackage.PackageInfoPath);
        }
    }

    internal bool CheckForPackageChanges()
    {
        if (selectedPackage == null)
            return false;

        return SelectedPackage.HasChanged();
    }

    internal void OpenNewPackage()
    {
        if (Popup.RootPopup is Views.NewPackageView)
            Popup.DismissPopup();
        else
        {
            Popup.DismissPopup();
            Popup.ShowPopup(new Views.NewPackageView() { DataContext = new NewPackageViewModel(this) });
        }
    }

    internal async void OpenExisting()
    {
        if (SelectedProject != null && File.Exists(SelectedProject.FilePath))
            OpenProject(SelectedProject.FilePath);
        else
        {
            var continueWork = await Dialog.ShowDialog("File Not Found", "The selected ProjectInfo file is not longer available, do you wish to remove it from the list?", "Yes", "Cancel");

            if (!continueWork)
                return;

            Settings.RecentItems.Remove(SelectedProject);
            RecentItems.Remove(SelectedProject);
        }

    }

    internal void OpenOptions()
    {
        if (Popup.RootPopup is Views.SparkOptionsView)
            Popup.DismissPopup();
        else
        {
            Popup.DismissPopup();
            Popup.ShowPopup(new Views.SparkOptionsView() { DataContext = SparkLogin });
        }
    }

    internal void OpenTeams()
    {
        if (Popup.RootPopup is Views.SparkProjectView)
            Popup.DismissPopup();
        else
        {
            Popup.DismissPopup();
            Popup.ShowPopup(new Views.SparkProjectView() { DataContext = TeamsViewModel });
        }
    }

    internal void ShowSparkUpdates()
    {
        if (Updates.UpdatesVisible)
            Updates.HideUpdates();
        else
            Updates.ShowUpdates();
    }

    internal void ToggleConsole()
    {
        if (Console.ConsoleVisible)
            Console.HideConsole(true);
        else
            Console.ShowConsole(true);
    }

    internal void LogIn()
    {
        LoggedIn = true;
        Popup.DismissPopup();
        InitUI();
    }

    internal void Logout()
    {
        LoggedIn = false;
        Popup.DismissPopup();
        Popup.ShowPopup(new Views.SparkLoginView() { DataContext = SparkLogin });
    }

    internal void LoadSettings()
    {
        string settingsPath = CustomerToolPaths.GetSettingsPath("Settings.json");
        if (!File.Exists(settingsPath))
            Settings = new();
        else
            Settings = JsonUtility.Deserialize<Settings>(File.ReadAllText(settingsPath));

        if (Settings.CurrentVersion != this.GetVersion())
        {
            Settings.CurrentVersion = this.GetVersion();
            showNewVersionHelp = true;
            SaveSettings();
        }

        if (File.Exists(Settings.LastUpgradePath))
        {
            File.Delete(Settings.LastUpgradePath);
            Settings.LastUpgradePath = null;
            SaveSettings();
        }

        RecentItems = new ObservableCollection<RecentItem>(Settings.RecentItems.OrderByDescending(x => x.IsFavorite));

    }

    internal void SaveSettings()
    {
        string settingsPath = CustomerToolPaths.GetSettingsPath("Settings.json");
        File.WriteAllText(settingsPath, JsonUtility.Serialize(Settings));
    }

    internal void FavoriteChanged(RecentItem recentItem)
    {
        recentItem.IsFavorite = !recentItem.IsFavorite;

        RecentItems = new ObservableCollection<RecentItem>(Settings.RecentItems.OrderByDescending(x => x.IsFavorite));
        OnPropertyChanged(nameof(RecentItems));
        this.SaveSettings();
    }

    internal void RemoveItem(RecentItem recentItem)
    {
        Settings.RecentItems.Remove(recentItem);

        RecentItems = new ObservableCollection<RecentItem>(Settings.RecentItems.OrderByDescending(x => x.IsFavorite));
        OnPropertyChanged(nameof(RecentItems));
        this.SaveSettings();
    }

    internal static void ErrorOccurred(object sender, UnhandledExceptionEventArgs e)
    {
        // Try to retrieve Spark account
        SparkAccountDTO account = null;
        try
        {
            account = SparkAccountClient.SharedSparkClient.GetAccount();
        }
        catch { }

        var error = new ErrorReportDTO()
        {
            Device = $"({Environment.MachineName})",
            Environment = Environment.OSVersion.VersionString,
            ErrorType = "Exception",
            ErrorDetails = e.ExceptionObject.ToString(),
            Is64Bit = Environment.Is64BitOperatingSystem,
            NameIdentifier = account?.AccountID,
            Software = $"{Process.GetCurrentProcess().ProcessName}",
            UserName = account?.Login ?? "Unknown",
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString()
        };

        try
        {
            _ = SparkAccountClient.SharedSparkClient.ReportError(error);
        }
        catch { }

        string criticalErrorFile = CustomerToolPaths.GetSettingsPath($"ErrorReport-{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}.json");
        string errorText = JsonUtility.Serialize(error);
        File.WriteAllText(criticalErrorFile, errorText);
    }

    #endregion
}

#region Settings
public class Settings
{
    public string STMExecutable { get; set; }
    public string SparkFilterString { get; set; }
    public SparkTrackType SparkFilterTrack { get; set; }
    public bool SparkFiltersVisible { get; set; }
    public string LastUpgradePath { get; set; }
    public string CurrentVersion { get; set; }
    public List<RecentItem> RecentItems { get; set; } = new List<RecentItem>();
    public List<String> FavoriteTeams { get; set; } = new List<String>();
    public bool UseInternalCLI { get; set; }
    public bool WaitForDebuggerInCLI { get; set; }
    public List<OffLineData> OfflineData { get; set; } = new List<OffLineData>();
    public bool HideOpenViewPro { get; set; }
    public bool HideOpenView { get; set; }

}

public class RecentItem
{
    public bool IsFavorite { get; set; }

    public string Name { get; set; }

    public string FilePath { get; set; }

    public string FileName
    {
        get
        {
            return Path.GetFileName(FilePath);
        }
    }

    public string Description { get; set; }

    public string LocationShort
    {
        get
        {
            return ShortenPath(Path.GetDirectoryName(FilePath), 75);
        }
    }

    private string ShortenPath(string path, int maxLength)
    {
        string[] parts;
        parts = path.Split('\\');

        int startIndex = (parts.Length - 1) / 2;
        int index = startIndex;

        String output = String.Join("\\", parts, 0, parts.Length);

        decimal step = 0;
        int lean = 1;

        while (output.Length >= maxLength && index != 0 && index != -1)
        {
            parts[index] = "...";

            output = String.Join("\\", parts, 0, parts.Length);

            step += 0.5M;
            lean *= -1;

            index = startIndex + ((int)step * lean);
        }
        // result can be longer than maxLength
        return output.Substring(0, Math.Min(maxLength, output.Length));
    }
}

#endregion

#region OfflineData
public class OffLineData
{
    public TeamDTO Team { get; set; }
    public List<OfflineModule> Modules { get; set; } = new();
}

public class OfflineModule
{
    public string ModuleId { get; set; }
    public string ModuleName { get; set; }
}
#endregion