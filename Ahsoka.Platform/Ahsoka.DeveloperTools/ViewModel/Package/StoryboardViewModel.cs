using Ahsoka.Installer;
using Ahsoka.Utility;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Ahsoka.DeveloperTools;

internal class StoryboardViewModel : ChildViewModelBase<PackageViewModel>
{
    string storyboardProgramFolder, storyboardProjectFolder, storyboardExportFolder, startupArguments, applicationName, storyboardVersion;
    readonly ObservableCollection<String> versions = new();

    public bool StoryboardProjectEnabled { get { return File.Exists(StoryboardProjectInfo); } set { } }

    public string StoryboardProjectInfo
    {
        get {  return ParentViewModel.DotNetViewModel != null ? Path.Combine(Path.GetDirectoryName(ParentViewModel.PackageInfo.GetPackageInfoPath()), 
                                  Path.GetDirectoryName(ParentViewModel.DotNetViewModel.ProjectPath), "Storyboard.Build.targets") : string.Empty; }
    }

    public string StoryboardProjectInfoRelative
    {
        get { return ParentViewModel.DotNetViewModel != null ? Path.Combine(Path.GetDirectoryName(ParentViewModel.DotNetViewModel.ProjectPath), "Storyboard.Build.targets") : string.Empty; }
    }

    public string StoryboardProgramFolder
    {
        get { return storyboardProgramFolder; }
        set
        {
            storyboardProgramFolder = value;
            OnPropertyChanged();
        }
    }

    public string StoryboardExportFolder
    {
        get { return storyboardExportFolder; }
        set
        {
            storyboardExportFolder = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<String> StoryboardVersions { get { return versions; } set { } }

    public string StoryboardVersion
    {
        get { return storyboardVersion; }
        set
        {
            storyboardVersion = value;
            OnPropertyChanged();
        }
    }

    public string StoryboardProjectFile
    {
        get { return StoryboardProjectFolder != null && ApplicationName != null ? Path.Combine(StoryboardProjectFolder, ApplicationName + ".gde") : String.Empty; }
        set
        {

        }
    }

    public string StoryboardProjectFolder
    {
        get { return storyboardProjectFolder; }
        set
        {
            storyboardProjectFolder = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StoryboardProjectFile));
        }
    }

    public string ApplicationName
    {
        get { return applicationName; }
        set
        {
            applicationName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StoryboardProjectFile));
        }
    }

    public string StartupArguements
    {
        get { return startupArguments; }
        set
        {
            startupArguments = value;
            OnPropertyChanged();
        }
    }

    public StoryboardViewModel(PackageViewModel parentViewModel)
        :base(parentViewModel)
    {
        ParentViewModel.PackageInfo.StoryboardInfo ??= new();
        ReadPathsFromProject();
    }

    internal async void SetStoryboardProgramFolder()
    {
        var path = await FindParent<MainViewModel>().FolderLocatorAsync("Pick Storyboard Application Folder", StoryboardProgramFolder);
        if (path != null)
            StoryboardProgramFolder = path;

        UpdateFileInfo();
    }

    internal async void SetStoryboardExportFolder()
    {
        string currentPath = string.Empty;
        if (StoryboardProjectFile != null)
            currentPath = Path.Combine(Path.GetDirectoryName(ParentViewModel.PackageInfo.GetPackageInfoPath()), StoryboardExportFolder);

        var path = await FindParent<MainViewModel>().FolderLocatorAsync("Pick Storyboard Export Folder", currentPath);
        if (path != null)
            StoryboardExportFolder = path;
    }

    internal async void SetStoryboardProject()
    {
        string currentPath = string.Empty;
        if (StoryboardProjectFile != null)
            currentPath = Path.Combine(Path.GetDirectoryName(ParentViewModel.PackageInfo.GetPackageInfoPath()), StoryboardProjectFile);

        var path = await FindParent<MainViewModel>().FileLocatorAsync("Select your Storyboard Project", "gde", "Storyboard Project", currentPath);
        if (path != null)
        {
            StoryboardProjectFolder = Path.GetDirectoryName(Path.GetRelativePath(Path.GetDirectoryName(ParentViewModel.PackageInfo.GetPackageInfoPath()), path));
            ApplicationName = Path.GetFileNameWithoutExtension(path);
        }
    }

    internal void ReadPathsFromProject()
    {
        if (File.Exists(StoryboardProjectInfo))
        {
            string projectFile = File.ReadAllText(StoryboardProjectInfo);
            XDocument doc = XDocument.Parse(projectFile);
            StoryboardProgramFolder = doc.Descendants("StoryboardProgramFolder").FirstOrDefault()?.Value;
            ApplicationName = doc.Descendants("StoryboardAppName").FirstOrDefault()?.Value;
            StoryboardProjectFolder = doc.Descendants("StoryboardProjectFolder").FirstOrDefault()?.Value;
            string versionTemp = doc.Descendants("StoryboardVersion").FirstOrDefault()?.Value;
            StoryboardExportFolder = doc.Descendants("StoryboardExportFolder").FirstOrDefault()?.Value;

            UpdateFileInfo();

            if (versions.Contains(versionTemp))
                StoryboardVersion = versionTemp;
        }

        OnPropertyChanged(nameof(StoryboardProjectInfo));
        OnPropertyChanged(nameof(StoryboardProjectEnabled));
    }

    private void UpdateFileInfo()
    {
        if (!Directory.Exists(StoryboardProgramFolder))
        {
            if (OperatingSystem.IsMacOS())
                StoryboardProgramFolder = "/Applications/Crank_Software";
            else if (OperatingSystem.IsWindows())
                StoryboardProgramFolder = "C:\\Program Files\\Crank_Software";
        }

        if (StoryboardProgramFolder != null)
        {
            versions.Clear();
            string engineFolder = Path.Combine(StoryboardProgramFolder, "Storyboard_Engine");
            if (Directory.Exists(engineFolder))
                foreach (var item in Directory.GetDirectories(engineFolder))
                    versions.Add(Path.GetFileName(item));

            if (String.IsNullOrEmpty(StoryboardVersion))
                StoryboardVersion = versions.FirstOrDefault();

        }

        OnPropertyChanged(nameof(StoryboardVersions));
        OnPropertyChanged(nameof(StoryboardVersion));

    }

    internal void Save(string storyboardProjectInfo)
    {
        if (File.Exists(storyboardProjectInfo))
        {
            try
            {
                string projectFile = File.ReadAllText(StoryboardProjectInfo);
                XDocument doc = XDocument.Parse(projectFile, LoadOptions.PreserveWhitespace);

                var node = doc.Descendants("StoryboardProgramFolder").FirstOrDefault();
                if (node != null) node.Value = StoryboardProgramFolder;

                node = doc.Descendants("StoryboardAppName").FirstOrDefault();
                if (node != null) node.Value = ApplicationName;

                node = doc.Descendants("StoryboardProjectFolder").FirstOrDefault();
                if (node != null) node.Value = StoryboardProjectFolder;

                node = doc.Descendants("StoryboardVersion").FirstOrDefault();
                if (node != null) node.Value = StoryboardVersion;

                doc.Save(storyboardProjectInfo);
            }
            catch
            {
                FindParent<MainViewModel>().Dialog.ShowDialog("Error Saving", "An error occured saving your storyboard configuration.  Please verify your settings andd save again.", "OK");
            }
        }
    }
}
