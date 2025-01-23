using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.Installer;
using Ahsoka.Spark;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using EnovationCloud.Core.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace Ahsoka.DeveloperTools;

internal class PackageViewModel : ChildViewModelBase<MainViewModel>, ICanCancel, IToolViewModel, IProvisionDisplayRequest
{
    #region Fields
    public const string opvpackageExtension = "opvPackage";
    public const string packageInfoExtension = "PackageInfo.json";

    PackageInformation packageInfo;
    string originalJsonText = string.Empty;
    CancellationTokenSource source;
    private DotNetViewModel dotNetModel;
    private StartupOptionsViewModel serviceStartModel;
    private ServiceViewModel dataServiceModel;
    HardwareViewModel hardwareViewModel;
    ExtensionsViewModel extensionsViewModel;
    DisplayViewModel displayViewModel;
    private CppViewModel cppViewModel;
    private bool customShown = false; 
    SparkViewModel sparkViewModel;
    DependenciesCheckViewModel deps;
    int selectedTab = 0;
    bool hasScreen = true;
    #endregion

    #region Properties
    public int SelectedTab
    {
        get { return selectedTab; }
        set { selectedTab = value; OnPropertyChanged(); }
    }

    public bool CustomShown
    {
        get
        {
            return customShown;
        }
        set { customShown = value; OnPropertyChanged(); }
    }

    public bool IsOpenView
    {
        get
        {
            return PlatformFamily != PlatformFamily.PowerVision;
        }
        set { }
    }

    public bool HasIVIShell
    {
        get
        {
            return packageInfo.PlatformFamily == PlatformFamily.OpenViewLinuxPro;
        }
        set { }
    }

    public bool HasScreen
    {
        get
        {
            return hasScreen;
        }

        set
        {
            hasScreen = value;
            OnPropertyChanged();
        }
    }

    public bool GenerateOptimizedServiceManager
    {
        get
        {
            return packageInfo.OptimizedServiceManager;
        }

        set
        {
            packageInfo.OptimizedServiceManager = value;
            OnPropertyChanged();
        }
    }

    public bool SupportsOptimized
    {
        get
        {
            return OperatingSystem.IsWindows();
        }
    }

    public DependenciesCheckViewModel Deps
    {
        get
        {
            return deps;
        }

        set
        {
            deps = value;
            OnPropertyChanged();
        }
    }

    public PackageInformation PackageInfo
    {
        get { return packageInfo;}
        private set
        {
            packageInfo = value;
            originalJsonText = JsonUtility.Serialize(PackageInfo);
        }
    }

    public bool IsLaunchCompatible
    {
        get { return OperatingSystem.IsWindows() && ( ApplicationType == ApplicationType.Cpp || ApplicationType == ApplicationType.Dotnet); }
        set { }
    }

    public string PackageFileName { get { return Path.GetFileName(PackageInfoPath); } }

    public string PackageInfoPath { get; private set; }

    public string PackageName
    {
        get
        {
            return PackageInfo.PackageName;
        }

        set
        {
            PackageInfo.PackageName = value;
            OnPropertyChanged();
        }
    }

    public bool SdkNeedsUpdate
    {
        get
        {
            return (cppViewModel != null && cppViewModel.SdkNeedsUpdate) || (dotNetModel != null && dotNetModel.SdkNeedsUpdate);
        }
    }

    public string PackageDescription
    {
        get
        {
            return PackageInfo.PackageDescription;
        }

        set
        {
            PackageInfo.PackageDescription = value;
            OnPropertyChanged();
        }
    }

    public string ApplicationVersion
    {
        get
        {
            return PackageInfo.ApplicationVersion;
        }

        set
        {
            PackageInfo.ApplicationVersion = value;
            OnPropertyChanged();
        }
    }

    public PlatformFamily PlatformFamily
    {
        get
        {
            return PackageInfo.PlatformFamily;
        }
        set
        {

            bool isChanged = PackageInfo.PlatformFamily != value;
            PackageInfo.PlatformFamily = value;
            OnPropertyChanged();

            OnPropertyChanged(nameof(PlatformDescription));
            Deps.CheckDependencies(SystemInfo.CurrentPlatform, value);

            if (isChanged)
                NeedsSDKUpdate();
        }
    }

    public ApplicationType ApplicationType
    {
        get
        {
            return PackageInfo.ApplicationType;
        }
        set
        {
            PackageInfo.ApplicationType = value;
            UpdateApplicationViewModel();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLaunchCompatible));
            OnPropertyChanged(nameof(IsDotNet));
            OnPropertyChanged(nameof(IsCODESYS));
            OnPropertyChanged(nameof(IsCPP));
        }
    }

    public bool IsDotNet
    {
        get { return ApplicationType == ApplicationType.Dotnet; }
        set { }
    }

    public bool IsCODESYS
    {
        get { return ApplicationType == ApplicationType.CODESYS; }
        set { }
    }

    public bool IsCPP
    {
        get { return ApplicationType == ApplicationType.Cpp; }
        set { }
    }

    public List<ModuleLicense> Licenses
    {
        get
        {
            return PackageInfo.Licenses;
        }
    }

    public bool IncludeInstallerUI
    {
        get
        {
            return PackageInfo.CustomPackageInfo.IncludeInstallerUI;
        }

        set
        {
            PackageInfo.CustomPackageInfo.IncludeInstallerUI = value;
            OnPropertyChanged();
        }
    }

    public bool IncludeBootLogo
    {
        get
        {
            return PackageInfo.CustomPackageInfo.IncludeBootLogo;
        }
        set
        {
            PackageInfo.CustomPackageInfo.IncludeBootLogo = value;
            OnPropertyChanged();
        }
    }

    public bool IncludeAppComponents
    {
        get
        {
            return PackageInfo.CustomPackageInfo.IncludeApplication;
        }
        set
        {
            PackageInfo.CustomPackageInfo.IncludeApplication = value;
            OnPropertyChanged();
        }
    }

    public bool IncludeFirmwareUpdate
    {
        get
        {
            return PackageInfo.CustomPackageInfo.IncludeFirmwareUpdate;
        }

        set
        {
            PackageInfo.CustomPackageInfo.IncludeFirmwareUpdate = value;
            OnPropertyChanged();
        }
    }

    public bool ProjectGenerated
    {
        get
        {
            string outputFile = PackageInfo.GetOutputFullPath();
            return File.Exists(outputFile);
        }
        set
        {

        }
    }

    public string OutputRelative
    {
        get
        {
            string outputFile = PackageInfo.GetOutputFullPath();
            if (File.Exists(outputFile))
            {
                return Path.GetRelativePath(PackageInfoPath, outputFile);
            }
            else
            {
                return "No Output - Generate to View Output Path";
            }
        }
        set
        {

        }
    }

    public string PlatformDescription
    {
        get
        {
            if (PackageInfo.PlatformFamily == PlatformFamily.PowerVision)
            {
                  return $"{PackageInfo.PlatformFamily} ({PackageInfo.PlatformQualifier})";
            }
            else
            {
                var hardwareInfo = HardwareInfo.GetHardwareInfo(PackageInfo.PlatformFamily, PackageInfo.PlatformQualifier);
                return hardwareInfo?.PlatformDescription;
            }
        }
        set
        {
            var hardwareInfo = HardwareInfo.GetHardwareInfoByDescription(value);

            if (hardwareInfo != null)
            {
                if (PackageInfo.PlatformFamily != hardwareInfo.PlatformFamily ||
                    PackageInfo.PlatformQualifier != hardwareInfo.PlatformQualifier)
                {
                    PackageInfo.PlatformFamily = hardwareInfo.PlatformFamily;
                    PackageInfo.PlatformQualifier = hardwareInfo.PlatformQualifier;

                    Deps.CheckDependencies(SystemInfo.CurrentPlatform, PackageInfo.PlatformFamily);

                    HandleHardwareInfoChanges();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsOpenView));
                    OnPropertyChanged(nameof(HasIVIShell));
                }
            }
        }
    }

    public Guid ApplicationId
    {
        get { return PackageInfo.ApplicationId; }
        set { PackageInfo.ApplicationId = value; OnPropertyChanged(); }
    }

    public string ExecCommand
    {
        get { return PackageInfo.ExecCommand; }
        set { PackageInfo.ExecCommand = value; OnPropertyChanged(); }
    }

    public string SecurityPassKey
    {
        get { return PackageInfo.SecurityPassKey; }
        set { PackageInfo.SecurityPassKey = value; OnPropertyChanged(); }
    }

    public string SecuritySerialNumber
    {
        get { return PackageInfo.SecuritySerialNumber; }
        set { PackageInfo.SecuritySerialNumber = value; OnPropertyChanged(); }
    }

    public string SecurityModelNumber
    {
        get { return PackageInfo.SecurityModelNumber; }
        set { PackageInfo.SecurityModelNumber = value; OnPropertyChanged(); }
    }

    public DotNetViewModel DotNetViewModel
    {
        get { return ApplicationType == ApplicationType.Dotnet ? dotNetModel : null; }
        set { dotNetModel = value; OnPropertyChanged(); }
    }

    public StartupOptionsViewModel StartupViewModel
    {
        get { return serviceStartModel; }
        set { serviceStartModel = value; OnPropertyChanged(); }
    }
    
    public ServiceViewModel ServiceViewModel
    {
        get { return dataServiceModel; }
        set { dataServiceModel = value; OnPropertyChanged(); }
    }

    public CppViewModel CodesysViewModel
    {
        get { return ApplicationType == ApplicationType.CODESYS ? cppViewModel : null; }
        set { cppViewModel = value; OnPropertyChanged(); }
    }

    public CppViewModel CppViewModel
    {
        get { return ApplicationType == ApplicationType.Cpp ? cppViewModel : null; }
        set { cppViewModel = value; OnPropertyChanged(); }
    }

    public SparkViewModel SparkViewModel
    {
        get { return sparkViewModel; }
        set { sparkViewModel = value; OnPropertyChanged(); }
    }
    
    public HardwareViewModel HardwareViewModel
    {
        get { return hardwareViewModel; }
        set { hardwareViewModel = value; OnPropertyChanged(); }
    }

    public ExtensionsViewModel ExtensionsViewModel 
    { 
        get => extensionsViewModel;
        set
        {
            extensionsViewModel = value;
            OnPropertyChanged();
        }
    }

    public DisplayViewModel DisplayViewModel
    {
        get => displayViewModel;
        set
        {
            displayViewModel = value;
            OnPropertyChanged();
        }
    }

    public RuntimeConfiguration ServiceConfiguration { get { return PackageInfo.ServiceInfo.RuntimeConfiguration; } }
    #endregion

    #region Package Changed
    public void PackageChanged()
    {
        OnPropertyChanged(nameof(OutputRelative));
        OnPropertyChanged(nameof(ProjectGenerated));
        OnPropertyChanged(nameof(PackageName));
        OnPropertyChanged(nameof(PlatformDescription));
        OnPropertyChanged(nameof(IsLaunchCompatible));
    }

    public void UpdatePackageInfo()
    {
        PackageInfo = PackageInformation.LoadPackageInformation(PackageInfoPath);
        PackageChanged();
    }
    #endregion

    #region Drop Downs
    
    public PackageType[] PackageTypes
    {
        get
        {
            return Enum.GetValues<PackageType>().Where(x => x == PackageType.opvEncrypted).ToArray();
        }
    }

    public ApplicationType[] ApplicationTypes
    {
        get
        {
            if (PlatformFamily == PlatformFamily.PowerVision)
                return Enum.GetValues<ApplicationType>().Where(x => x == ApplicationType.PowerVision).ToArray();

            return Enum.GetValues<ApplicationType>().Where(x => x != ApplicationType.Unknown).ToArray();
        }
    }

    public List<string> PlatformDescriptions
    {
        get
        {
            if (this.PlatformFamily == PlatformFamily.PowerVision)
            {
                return new List<string>() { $"{PackageInfo.PlatformFamily} ({PackageInfo.PlatformQualifier})" };
            }
            else
               return HardwareInfo.GetHardwareInfoDescriptions().Where(x => !x.StartsWith("PowerVision")).ToList();
        }
        set
        {
            // Not Used
        }
    }
    #endregion


    #region Folder Selectors
    public string PackageApplicationFolder
    {
        get { return PackageInfo.PackageApplicationFolder; }
        set { PackageInfo.PackageApplicationFolder = value; OnPropertyChanged(); }
    }

    public string PackageOutputDirectory
    {
        get { return PackageInfo.PackageOutputDirectory; }
        set { PackageInfo.PackageOutputDirectory = value; OnPropertyChanged(); }
    }

    int IToolViewModel.DownloadPercent { get; set; }

    MainViewModel IToolViewModel.MainViewModel => ParentViewModel;

    string IToolViewModel.PercentStatus { get; set; }

    internal async void SetOutputDir()
    {
        string currentPath = Path.Combine(Path.GetDirectoryName(PackageInfo.GetPackageInfoPath()), PackageOutputDirectory);
        var path = await ParentViewModel.FolderLocatorAsync("Pick Output Directory", currentPath);
        if (path != null)
            PackageOutputDirectory = Path.GetRelativePath(Path.GetDirectoryName(PackageInfo.GetPackageInfoPath()), path);
    }

    internal async void SetApplicationDir()
    {
        string currentPath = Path.Combine(Path.GetDirectoryName(PackageInfo.GetPackageInfoPath()), PackageApplicationFolder);
        var path = await ParentViewModel.FolderLocatorAsync("Pick Application Directory", currentPath);
        if (path != null)
            PackageApplicationFolder = Path.GetRelativePath(Path.GetDirectoryName(PackageInfo.GetPackageInfoPath()), path);
    }

    #endregion

    #region Methods
    public PackageViewModel(MainViewModel parentViewModel, string filePath) : 
        base(parentViewModel)
    {
        Deps = new DependenciesCheckViewModel(parentViewModel);

        this.ParentViewModel = parentViewModel;

        PackageInfo = PackageInformation.LoadPackageInformation(filePath);
        PackageInfoPath = filePath;
    }

    internal void InitModel()
    {
        this.sparkViewModel = new SparkViewModel(ParentViewModel);
        StartupViewModel = new StartupOptionsViewModel(this);
        ServiceViewModel = new ServiceViewModel(this);
        ExtensionsViewModel = new ExtensionsViewModel(this);
        DisplayViewModel = new DisplayViewModel(this);
        HardwareViewModel = new HardwareViewModel(this);

        Deps.CheckDependencies(SystemInfo.CurrentPlatform, PackageInfo.PlatformFamily);

        UpdateApplicationViewModel();
        
        HandleHardwareInfoChanges();

        DotNetViewModel?.NeedsUpdate();
        CppViewModel?.NeedsUpdate();
    }

    private void HandleHardwareInfoChanges()
    {
        var hardwareInfo = HardwareInfo.GetHardwareInfo(PackageInfo.PlatformFamily, PackageInfo.PlatformQualifier);
        if (hardwareInfo != null)
        {
            HasScreen = hardwareInfo.ScreenInfo.ScreenWidth > 0;
            DisplayViewModel.UpdateUI();
        }
    }

    internal void DeletePackage()
    {
        string pathToPackage = PackageInfo.GetOutputFullPath();
        if (File.Exists(pathToPackage))
            File.Delete(pathToPackage);

        PackageChanged();
    }

    internal void GoToPackage()
    {
        string pathToPackage = PackageInfo.GetOutputFullPath();
        if (File.Exists(pathToPackage))
        {
            if (OperatingSystem.IsWindows())
                Process.Start("explorer.exe", "/select, " + pathToPackage);
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", $"-R \"{pathToPackage}\"");
        }

    }

    internal bool Validate()
    {
        var filled = true;
        if (IsOpenView)
        {

            if (string.IsNullOrEmpty(PackageName))
                filled = false;

            if (string.IsNullOrEmpty(PackageOutputDirectory))
                filled = false;

            if (string.IsNullOrEmpty(PackageApplicationFolder))
                filled = false;
        }

        if (SparkViewModel.SelectedTeam != null)
            foreach (var item in Licenses.ToArray())
                if (!SparkViewModel.SelectedTeam.Modules.ModuleList.Any(x => x.ModuleID == item.LicenseId))
                    Licenses.Remove(item);

        return filled;
    }

    internal void LoadToTarget()
    {
        LoadToTarget(false);
    }

    internal void LoadToTargetCustom()
    {
        LoadToTarget(true);
    }

    internal async void LoadToTarget(bool isCustomInstall)
    {
        source = new CancellationTokenSource();

        ParentViewModel.Console.ClearConsole();

        bool forceInstallerAttempt = false;
        if (!ParentViewModel.ActiveConnection.IsConnected && IsOpenView)
        {
            var continueWork = await ParentViewModel.Dialog.ShowDialog("No Active Display", "There is not currently a connected display. This may be due to not having a display connected or running an application that does not allow System Services.  Would you like to attempt to load anyway?", "Yes", "Cancel");
            if (!continueWork) return;

            forceInstallerAttempt = true;
        }

        var itemToGenerate = ParentViewModel.SelectedPackage;
        if (itemToGenerate == null)
        {
            ParentViewModel.Console.AddConsoleLine("Please Select a Package");
            return;
        }

        // Handle OPV Packages
        string pathToLoad = string.Empty;
        if (PackageInfoPath.ToLower().EndsWith(opvpackageExtension.ToLower()))
        {
            // Extract Package Info to Temp File (delete when done
            using var zipFile = new FileStream(PackageInfoPath, FileMode.Open);
            using var archive = new ZipArchive(zipFile);
            var entry = archive.Entries.FirstOrDefault(x => x.Name.ToLower().EndsWith(".pv2"));
            if (entry == null)
            {
                await ParentViewModel.Dialog.ShowDialog("Missing Binary", "A PV2 file was not found in the opvPackage file.  Please verify that this file was produced by PowerVision 8.0 or greater ", "OK");
                return;
            }
            
            pathToLoad = Path.GetTempFileName();
            entry.ExtractToFile(pathToLoad, true);
        }
        else
        {
            pathToLoad = itemToGenerate.PackageInfo.GetOutputFullPath(isCustomInstall);
            if (!File.Exists(pathToLoad))
            {
                ParentViewModel.Console.AddConsoleLine("Generate this Package");
                return;
            }
        }


        ParentViewModel.Console.AddConsoleLine("Beginning Application Load to Target");

        var progressFileInfo = new Progress<FileTransmitProgressInfo>(info =>
        {
            ParentViewModel.Console.AddConsoleText(info.Message, true);
        });

        var progressString = new Progress<string>(info =>
        {
            ParentViewModel.Console.AddConsoleLine(info);
        });


        ParentViewModel.StartProcess(this);

        var task = Task.Run(() =>
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            if (!forceInstallerAttempt)
            {
                var hardwareInfo = ParentViewModel.ActiveConnection.GetHardwareInformation();
                if (hardwareInfo == null)
                    return "Could not communicate with the target.";

                string connectedInfo = hardwareInfo.PlatformFamily + "." + hardwareInfo.PlatformQualifier;
                string packageInfo = ParentViewModel.SelectedPackage.PackageInfo.PlatformFamily + "." + ParentViewModel.SelectedPackage.PackageInfo.PlatformQualifier;

                if (connectedInfo != packageInfo)
                {
                    return $"The selected package type '{connectedInfo}' does not match the connected hardware '{packageInfo}'";
                }
            }

            // Load Package
            using (var stream = new FileStream(pathToLoad, FileMode.Open, FileAccess.Read))
            {
                if (IsOpenView && (!ParentViewModel.ActiveConnection.IsInstaller || forceInstallerAttempt))
                {
                    var hwInfo = ParentViewModel.ActiveConnection.ReturnToInstaller(source, progressString);

                    // Didn't make it to the Installer....Error Out
                    if (hwInfo == null || !hwInfo.IsInstaller)
                    {
                        ParentViewModel.Console.AddConsoleLine("The target is currently running an application.");
                        ParentViewModel.Console.AddConsoleLine("You must exit the running application to return to the installer.");
                        ParentViewModel.Console.AddConsoleLine(" Hint: Hit the 'Stop Application' Button on the bottom right.");
                        return "Installer is Not Running.";
                    }
                }

                // Clear Journal Before App Install
                if (IsOpenView && !SystemInfo.IsDesktop())
                {
                    var prep = RemoteTargetToolFactory.GetToolsForPlatform(ParentViewModel.ActiveConnection.Connection.PlatformFamily);
                    prep.ClearJournal(ParentViewModel.ActiveConnection.Connection);
                }

                // Cancel will stop if needed.
                var result = ParentViewModel.ActiveConnection.LoadApplicationAsync(stream, true, progressFileInfo);
                var resultValue = result.GetAwaiter().GetResult();
             
                // Watch for Result
                if (!source.IsCancellationRequested)
                    if (IsOpenView)
                        ParentViewModel.Console.RunJournalCommand("Ahsoka.Installer",
                            "Install 'End Install' Completed with Result:", "Unknown Result");
            }

            stopwatch.Stop();

            return !source.IsCancellationRequested ? "Package Load Completed" : "Package Load Cancelled or Did Not Complete";

        }).ContinueWith(result =>
        {
            // Delete Temp files
            if (pathToLoad.EndsWith("tmp"))
                File.Delete(pathToLoad);

            if (result.IsFaulted)
                ParentViewModel.FinishProcess(result.Exception.Message, false);
            else
                ParentViewModel.FinishProcess(result?.Result, result?.Status == TaskStatus.RanToCompletion && result?.Result == "Package Load Completed");
        });
    }

    internal void LaunchDevTool()
    {
        if (ParentViewModel.Plugins.SupportInfo.Downloads.Count == 0)
            ParentViewModel.Plugins.RefreshTools();

        // Check Pre-Requisites
        var family = this.PlatformFamily;
        var tools = ParentViewModel.Plugins.SupportInfo;
        OpenPVToolBase launchTool = null;
        if (ApplicationType == ApplicationType.Cpp)
        {
            if (CppViewModel.ProjectType == CPPProjectType.QT)
            {
                if (family == PlatformFamily.OpenViewLinux)
                    launchTool = tools?.Downloads.FirstOrDefault(x => x is OpenViewQT5ContainerTool);
                else if (family == PlatformFamily.OpenViewLinuxPro)
                    launchTool = tools?.Downloads.FirstOrDefault(x => x is OpenViewProQT5ContainerTool);
            }
            else if (CppViewModel.ProjectType == CPPProjectType.VSCode)
            {
                if (family == PlatformFamily.OpenViewLinux)
                    launchTool = tools?.Downloads.FirstOrDefault(x => x is OpenViewSDKContainerTool);
                else if (family == PlatformFamily.OpenViewLinuxPro)
                    launchTool = tools?.Downloads.FirstOrDefault(x => x is OpenViewProSDKContainerTool);
            }
        }
        else if (ApplicationType == ApplicationType.Dotnet && OperatingSystem.IsWindows())
        {
            string projectFolder = Path.GetDirectoryName(PackageInfoPath);
            string projectFile = Path.Combine(projectFolder, DotNetViewModel.ProjectPath);
            if (Path.Exists(projectFile))
            {
                ProcessUtility.RunProcess("cmd", $"/c {projectFile}", projectFolder, out string result, out string error);
                return;
            }
            else
                ParentViewModel.Dialog.ShowDialog("Missing Project", $"The Project Path was not found.", "OK");

        }

        if (launchTool == null || !launchTool.IsDownloaded)
        {
            ParentViewModel.Dialog.ShowDialog("Missing Container", $"The Development Container Plugin not Found. Please download the QT / Ubuntu Workflow to use this feature", "OK");
            return;
        }

        launchTool.LaunchTool(this);
    }

    internal  void GenerateInstallPackage()
    {
       GeneratePackage(false);
    }

    internal void ShowCustomPackage()
    {
        CustomShown = true;
    }

    internal void CancelCustom()
    {
        CustomShown = false;
    }

    internal void GenerateCustomPackage()
    {
        GeneratePackage(true);
    }

    internal async void GeneratePackage(bool customInstall)
    {
        var itemToGenerate = ParentViewModel.SelectedPackage;
        if (itemToGenerate == null)
        {
            ParentViewModel.Console.AddConsoleLine("Please Select a Package");
            return;
        }

        // Save Before Generating
        ParentViewModel.SavePackage();

        ParentViewModel.Console.ClearConsole();
        ParentViewModel.Console.AddConsoleLine("Generating OPV Package File");

        var progress = new Progress<CommandLineProgressInfo>(info =>
        {
            if (info.IsWriteLine)
                ParentViewModel.Console.AddConsoleLine(info.Message);
            else
                ParentViewModel.Console.AddConsoleText(info.Message);
        });

        source = new CancellationTokenSource();

        ParentViewModel.StartProcess(this);

        await Task.Run(() =>
        {
            // Grab Latest Copy of Info.
            itemToGenerate.UpdatePackageInfo();

            if (SdkNeedsUpdate)
            {
                ParentViewModel.Console.AddConsoleLine("Updating SDK");

                if (ApplicationType == ApplicationType.Dotnet)
                    dotNetModel.NeedsUpdate(true).GetAwaiter().GetResult();
                else if (ApplicationType == ApplicationType.Cpp || ApplicationType == ApplicationType.CODESYS)
                    cppViewModel.NeedsUpdate(true).GetAwaiter().GetResult();
            }


            string workingDirectory = Path.GetDirectoryName(itemToGenerate.PackageInfoPath);
            string packageInfoPath = itemToGenerate.PackageInfoPath;
            string output = itemToGenerate.PackageInfo.GetOutputFullPath();

            if (File.Exists(output))
                File.Delete(output);

            string prefix = "";
            if (ParentViewModel.Settings.WaitForDebuggerInCLI)
                prefix = "--WaitForDebugger ";

            if (!customInstall)
                ParentViewModel.RunCommandLineProcess($"{prefix}--Package \"{workingDirectory}\" \"{packageInfoPath}\"", workingDirectory, progress, source.Token, ParentViewModel.SelectedPackage.GetCommandLinePath());
            else
                RunCustomPackage(progress, workingDirectory, packageInfoPath, prefix);

            output = itemToGenerate.PackageInfo.GetOutputFullPath(customInstall);
            if (File.Exists(output))
                return output;
            else
                return string.Empty; // fileName;

        }).ContinueWith(result =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.Status == TaskStatus.Faulted)
                {
                    ParentViewModel.Console.AddConsoleLine(result.Exception.Message);
                    ParentViewModel.Dialog.ShowDialog("Generation Error", $"{result.Exception?.InnerException?.Message}", "OK");
                }
                else if (result.Status == TaskStatus.RanToCompletion && !File.Exists(result.Result))
                {
                    ParentViewModel.Console.AddConsoleLine(result.Result);
                    ParentViewModel.Dialog.ShowDialog("Generation Error", $"An Error Occured While Generating the Package.", "OK");
                }
                else
                    itemToGenerate.PackageChanged();

                CustomShown = false;

                bool fileCreated = result?.Status == TaskStatus.RanToCompletion && File.Exists(result.Result);
                ParentViewModel.FinishProcess($"OPV Generation Completed: {fileCreated}", fileCreated);
            });
        });
    }

    private void RunCustomPackage(Progress<CommandLineProgressInfo> progress, string workingDirectory, string packageInfoPath, string prefix)
    {
        // Find Firmware File
        string firmwareFileName = string.Empty;
        string installerFolder = string.Empty;

        if (IncludeFirmwareUpdate)
        {
            string firmwareFolder = "";
            try { firmwareFolder = Path.Combine(PlatformSupportPathInfo.GetPlatformSupportPath(PlatformFamily)); }
            catch { }

            if (!Path.Exists(firmwareFolder))
                firmwareFolder = Path.Combine(CustomerToolPaths.GetToolPath(PlatformSupportPathInfo.PlatformSupportRootFolderName),
                                                PlatformFamily.ToString() + "_TargetSupport");

            string fileName = Path.Combine(Path.Combine(firmwareFolder), this.PlatformFamily.ToString() + "_Firmware.zip.customertools.version");
            if (File.Exists(fileName))
                fileName = File.ReadAllText(fileName).TrimEnd();

            if (fileName != null)
            {
                string firmwareLocation = firmwareFolder + "_Firmware";
                string imageFile = "upgrade.*." + fileName.Replace("Firmware_", "");
                firmwareFileName = Directory.EnumerateFiles(firmwareLocation, imageFile, SearchOption.AllDirectories).FirstOrDefault();
            }
        }

        if (IncludeInstallerUI)
        {
            try { installerFolder = Path.Combine(PlatformSupportPathInfo.GetPlatformSupportPath(PlatformFamily)); }
            catch { }

            if (!Path.Exists(installerFolder))
                installerFolder = Path.Combine(CustomerToolPaths.GetToolPath(PlatformSupportPathInfo.PlatformSupportRootFolderName),
                                                PlatformFamily.ToString() + "_TargetSupport");

            installerFolder = Path.Combine(installerFolder, "InstallerUI");
        }

        CustomPackageInfo info = new CustomPackageInfo()
        {
            IncludeApplication = IncludeAppComponents,
            IncludeFirmwareUpdate = IncludeFirmwareUpdate,
            FirmwareUpgradePath = firmwareFileName,
            IncludeBootLogo = IncludeBootLogo,
            IncludeInstallerUI = IncludeInstallerUI,
            InstallerUpgradePath = installerFolder,
        };

        string tempFile = Path.GetTempFileName() + ".CustomInfo.json";   
        File.WriteAllText(tempFile, JsonUtility.Serialize(info));

        ParentViewModel.RunCommandLineProcess($"{prefix}--PackageCustom \"{workingDirectory}\" \"{packageInfoPath}\" \"{tempFile}\"", workingDirectory, progress, source.Token, ParentViewModel.SelectedPackage.GetCommandLinePath());

        File.Delete(tempFile);
    }

    public void Cancel()
    {
        // Prevent Starting new Process
        ParentViewModel.ActiveConnection.CancelLoadApplication();
        source.Cancel();
        ParentViewModel.Console.Cancel();
    }

    private void UpdateApplicationViewModel()
    {
        switch (ApplicationType)
        {
            case ApplicationType.Dotnet:
                DotNetViewModel = new DotNetViewModel(ParentViewModel, PackageInfo);
                break;
            case ApplicationType.CODESYS:
                CodesysViewModel = new CppViewModel(ParentViewModel, PackageInfo);
                break;
            case ApplicationType.Cpp:
                CppViewModel = new CppViewModel(ParentViewModel, PackageInfo);
                break;
            case ApplicationType.PowerVision:
                break;
            default:
                break;
        }

        OnPropertyChanged(nameof(CppViewModel));
        OnPropertyChanged(nameof(DotNetViewModel));
    }

    internal void SavePackage()
    {
        if (PackageInfoPath.ToLower().EndsWith(opvpackageExtension.ToLower()))
        {
            // Extract Package Info to Temp File (delete when done
            using var zipFile = new FileStream(PackageInfoPath, FileMode.Open);
            using var archive = new ZipArchive(zipFile, ZipArchiveMode.Update);
            var entry = archive.Entries.FirstOrDefault(x => x.Name.ToLower().EndsWith(packageInfoExtension.ToLower()));
            if (entry == null)
            {
                ParentViewModel.Console.AddConsoleLine("A packageInfo file was not found in the opvPackage file.  Please verify that this file was produced by PowerVision 8.0 or greater ");
                return;
            }

            using var fileStream = entry.Open();
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.Serialize(PackageInfo));
            fileStream.Write(bytes,0,bytes.Length);
        }
        else
        {
            ExtensionsViewModel.SaveViewModels(Path.GetDirectoryName(PackageInfoPath));

            // Update this Last in case Child Values Changed
            string data = JsonUtility.Serialize(PackageInfo);
            File.WriteAllText(PackageInfoPath, data);
        }

        UpdatePackageInfo();
    }

    internal bool HasChanged()
    {
        string currentData = JsonUtility.Serialize(PackageInfo);
        if (originalJsonText != currentData)
            return true;

        if (ExtensionsViewModel.HasChanges())
            return true;
        
        return false;
    }

    internal void Close()
    {
        ExtensionsViewModel.CloseAndDiscardChanges();
    }

    internal void NeedsSDKUpdate()
    {
        if (cppViewModel != null)
            cppViewModel.SdkNeedsUpdate = true;
        
        if (dotNetModel != null)
            dotNetModel.SdkNeedsUpdate = true;
    }

    internal string GetCommandLinePath()
    {
        string pathToCommand = string.Empty;

        // Run Machine / Tools Downloads
        string pathToKits;  
        if (ApplicationType == ApplicationType.Dotnet)
        {
            string projectPath = Path.GetDirectoryName(Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), PackageInfo.DotNetInfo.ProjectPath));
            pathToKits = Path.Combine(projectPath, "obj");
        }
        else
        {
            pathToKits = Path.GetDirectoryName(Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), PackageInfo.CppInfo.ProjectPath));
        }


        pathToCommand = Path.Combine(pathToKits, PlatformSupportPathInfo.PlatformSupportRootFolderName_App, PlatformSupportPathInfo.GetPlatformFolderName(SystemInfo.CurrentPlatform), "CommandLine", "Ahsoka.CommandLine");

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            pathToCommand += ".exe";

        return pathToCommand;
    }
    #endregion
}

public class PackageErrorInfo : ViewModelBase
{
    bool error;

    public bool Error
    {
        get => error;
        set
        {
            error = value;
            OnPropertyChanged();
        }
    }

    public string ErrorText
    {
        get => "Please enter a value";
    }

    public ISolidColorBrush ErrorColor
    {
        get => Brushes.Red;
    }
}