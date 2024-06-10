using Ahsoka.Commands;
using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.Storyboard;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Avalonia.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Ahsoka.DeveloperTools;

internal class StoryboardViewModel : ExtensionViewModelBase
{
    StoryboardDevelopmentConfiguration configuration;
    private HardwareInfo hardwareInfo;
    private string projectFolder;
    private string originalConfigText;
    readonly ObservableCollection<String> versions = new();

    public bool ModelGeneratorEnabled
    {
        get { return configuration.ModelGeneratorEnabled; }
        set { configuration.ModelGeneratorEnabled = value; OnPropertyChanged(); }
    }


    public string StoryboardProjectFile
    {
        get { return !String.IsNullOrEmpty(GeneratorProjectPath) && ApplicationName != null ? Path.Combine(GeneratorProjectPath, ApplicationName + ".gde") : String.Empty; }
        set
        {

        }
    }

    public string ApplicationName
    {
        get { return configuration.StoryboardAppName; }
        set
        {
            configuration.StoryboardAppName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StoryboardProjectFile));
        }
    }

    public string ConfigurationPath
    {
        get { return Path.GetFileName(GetStoryboardConfigPath()); }
    }

    public string GeneratorProjectPath
    {
        get { return configuration.GeneratorProjectPath; }
        set
        {
            configuration.GeneratorProjectPath = value;
            OnPropertyChanged();
        }
    }
    public string GeneratorNameSpace
    {
        get { return configuration.GeneratorNameSpace; }
        set
        {
            configuration.GeneratorNameSpace = value;
            OnPropertyChanged();
        }
    }

    public string GeneratorBaseClass
    {
        get { return configuration.GeneratorModelBaseClass; }
        set
        {
            configuration.GeneratorModelBaseClass = value;
            OnPropertyChanged();
        }
    }

    public string GeneratorOutputFolder
    {
        get { return configuration.GeneratorOutputFolder; }
        set
        {
            configuration.GeneratorOutputFolder = value;
            OnPropertyChanged();
        }
    }

    public bool AppGeneratorEnabled
    {
        get { return configuration.AppGeneratorEnabled; }
        set { configuration.AppGeneratorEnabled = value; OnPropertyChanged(); }
    }


    public string StoryboardAppExportFolder
    {
        get { return configuration.StoryboardAppExportFolder; }
        set
        {
            configuration.StoryboardAppExportFolder = value;
            OnPropertyChanged();
        }
    }
  
    public string StoryboardAppProgramFolder
    {
        get { return configuration.StoryboardAppProgramFolder; }
        set
        {
            configuration.StoryboardAppProgramFolder = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<String> StoryboardVersions 
    { 
        get { return versions; }
        set { } 
    }

    public string StoryboardAppVersion
    {
        get { return configuration.StoryboardAppVersion; }
        set
        {
            configuration.StoryboardAppVersion = value;
            OnPropertyChanged();
        }
    }
   
    public StoryboardViewModel()
    {

    }

    internal async void SetStoryboardProgramFolder()
    {
        var path = await CustomerToolViewModel.FolderLocatorAsync("Pick Storyboard Application Folder", StoryboardAppProgramFolder);
        if (path != null)
            StoryboardAppProgramFolder = path;

        UpdateFileInfo();
    }

    internal async void SetStoryboardExportFolder()
    {
        string currentPath = string.Empty;
        if (StoryboardAppExportFolder != null)
            currentPath = Path.Combine(projectFolder, StoryboardAppExportFolder);

        var path = await CustomerToolViewModel.FolderLocatorAsync("Select your Application Export", currentPath);
        if (path != null)
        {
            StoryboardAppExportFolder = Path.GetRelativePath(projectFolder, path);
        }
    }

    internal async void SetStoryboardProject()
    {
        string currentPath = string.Empty;
        if (StoryboardProjectFile != null)
            currentPath = Path.Combine(projectFolder, StoryboardProjectFile);

        var path = await CustomerToolViewModel.FileLocatorAsync("Select your Storyboard Project", "gde", "Storyboard Project", currentPath);
        if (path != null)
        {
            GeneratorProjectPath = Path.GetDirectoryName(Path.GetRelativePath(projectFolder, path));
            ApplicationName = Path.GetFileNameWithoutExtension(path);
        }
    }

    internal async void SetStoryboardOutput()
    {
        string currentPath = string.Empty;
        if (GeneratorOutputFolder != null)
            currentPath = Path.Combine(projectFolder, GeneratorOutputFolder);

        var path = await CustomerToolViewModel.FolderLocatorAsync("Select your Model Folder", currentPath);
        if (path != null)
        {
            GeneratorOutputFolder = Path.GetRelativePath(projectFolder, path);
        }
    }

    private void UpdateFileInfo()
    {
       if (!Directory.Exists(StoryboardAppProgramFolder))
        {
            if (OperatingSystem.IsMacOS())
                StoryboardAppProgramFolder = "/Applications/Crank_Software";
            else if (OperatingSystem.IsWindows())
                StoryboardAppProgramFolder = "C:\\Program Files\\Crank_Software";
        }

        if (StoryboardAppProgramFolder != null)
        {
            versions.Clear();
            string engineFolder = Path.Combine(StoryboardAppProgramFolder, "Storyboard_Engine");
            if (Directory.Exists(engineFolder))
                foreach (var item in Directory.GetDirectories(engineFolder))
                    versions.Add(Path.GetFileName(item));

            if (String.IsNullOrEmpty(StoryboardAppProgramFolder))
                StoryboardAppVersion = versions.FirstOrDefault();

        }

        OnPropertyChanged(nameof(StoryboardVersions));
        OnPropertyChanged(nameof(StoryboardAppVersion));

    }

    protected override void OnInitExtension(HardwareInfo hardwareInfo, string projectFolder)
    {
        this.hardwareInfo = hardwareInfo;
        this.projectFolder = projectFolder;
        var path = GetStoryboardConfigPath();
        if (File.Exists(path))
        {
            originalConfigText = File.ReadAllText(path);
            this.configuration = ConfigurationFileLoader.LoadFile<StoryboardDevelopmentConfiguration>(path);
        }
        else
        {
            this.configuration = new StoryboardDevelopmentConfiguration()
            {
                GeneratorOutputFolder = "Generated",
                GeneratorModelBaseClass = "DemoViewModelBase",
                GeneratorNameSpace = " Ahsoka.Demo.Storyboard",
                AppGeneratorEnabled = true,
            };
        }

        UpdateFileInfo();

    }

    private string GetStoryboardConfigPath()
    {
        return Path.Combine(projectFolder, StoryboardCodeGenerator.ConfigFile);
    }

    protected override string OnSave(string packageInfoFolder)
    {
        /// Generate FileName if We Edited the Data but have no path.
        string configPath = GetStoryboardConfigPath();

        if (!String.IsNullOrEmpty(configPath))
        {
            string data = JsonUtility.Serialize(configuration);

            bool hasChanges = data != originalConfigText;

            // Config Changed...may need to regenerate the Models.
            if (hasChanges)
            {
                originalConfigText = data;
                this.CustomerToolViewModel.SdkNeedsUpdate();
            }

            File.WriteAllText(configPath, data);
        }

        return configPath;
    }

    protected override UserControl OnGetView()
    {
        return new StoryboardView() { DataContext = this };
    }

    protected override bool OnHasChanges()
    {
        string newConfig = JsonUtility.Serialize(configuration);
        bool hasChanges = newConfig != originalConfigText;

        // Config Changed...may need to regenerate the Models.
        if (hasChanges)
            this.CustomerToolViewModel?.SdkNeedsUpdate();

        return hasChanges;
    }

    protected override void OnClose()
    {
        configuration = null;
    }
}
