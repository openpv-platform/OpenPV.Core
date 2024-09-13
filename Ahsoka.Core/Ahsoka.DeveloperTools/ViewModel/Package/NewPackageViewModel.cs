using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.Installer;
using Ahsoka.Utility;
using Avalonia.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;

namespace Ahsoka.DeveloperTools;

internal class NewPackageViewModel : ViewModelBase
{
    #region Fields
    private readonly MainViewModel parentViewModel;
    private bool isCreate, matchNames = true;
    private string projectName, projectPath, directoryName;

    #endregion

    #region Properties
    public ObservableCollection<CustomerDemoInfoViewModel> Demos
    {
        get; private set;
    }

    public CustomerDemoInfoViewModel SelectedDemo
    {
        get; private set;
    }

    public bool ShowNoProjects { get; }

    public bool IsCreate { get { return isCreate; } set { isCreate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsConfigure)); } }

    public bool IsConfigure { get { return !isCreate; } }

    public bool MatchNames { get { return matchNames; } set { matchNames = value; OnPropertyChanged(); OnPropertyChanged(nameof(DirectoryName)); } }

    public string ProjectName { get { return projectName; } set { projectName = value; OnPropertyChanged(); OnPropertyChanged(nameof(DirectoryName)); } }

    public string ProjectPath { get { return projectPath; } set { projectPath = value; OnPropertyChanged(); } }

    public string DirectoryName
    {
        get
        {
            if (MatchNames)
                return projectName;
            else
                return directoryName;
        }
        set { directoryName = value; OnPropertyChanged(); }
    }

    #endregion

    #region Methods
    public NewPackageViewModel(MainViewModel customerToolsViewModel)
    {
        this.parentViewModel = customerToolsViewModel;
        Demos = new();
        IsCreate = true;

        //load demos
        string demoFiles = GetTemplateFolder();

        if (!Design.IsDesignMode && Directory.Exists(demoFiles))
            foreach (var file in Directory.GetFiles(demoFiles, "*.zip"))
            {
                try
                {
                    using ZipArchive zip = ZipFile.Open(file, ZipArchiveMode.Read);
                    foreach (ZipArchiveEntry entry in zip.Entries)
                        if (entry.Name.ToLower().EndsWith(PackageViewModel.packageInfoExtension.ToLower()))
                        {
                            var data = new StreamReader(entry.Open());
                            var demoProject = JsonUtility.Deserialize<PackageInformation>(data.ReadToEnd());
                            Demos.Add(new CustomerDemoInfoViewModel(demoProject) { FileName = file });
                        }
                }
                catch { }
            }

        // Load Extension Demos
        string extensionFolder = CustomerToolPaths.GetToolPath(PlatformExtensionsTool.Folder);

        if (!Design.IsDesignMode && Directory.Exists(extensionFolder))
            foreach (var file in Directory.GetFiles(extensionFolder, "*.zip", SearchOption.AllDirectories))
            {
                if (file.Contains("demos", StringComparison.InvariantCultureIgnoreCase))
                {
                    try
                    {
                        using ZipArchive zip = ZipFile.Open(file, ZipArchiveMode.Read);
                        foreach (ZipArchiveEntry entry in zip.Entries)
                            if (entry.Name.ToLower().EndsWith(PackageViewModel.packageInfoExtension.ToLower()))
                            {
                                var data = new StreamReader(entry.Open());
                                var demoProject = JsonUtility.Deserialize<PackageInformation>(data.ReadToEnd());
                                Demos.Add(new CustomerDemoInfoViewModel(demoProject) { FileName = file });
                            }
                    }
                    catch { }
                }
            }

        ShowNoProjects = Demos.Count == 0;
    }

    private string GetTemplateFolder()
    {
        return CustomerToolPaths.GetToolPath("Demos");
    }

    internal async void SetProjectDir()
    {
        var path = await parentViewModel.FolderLocatorAsync("Select Location of Project Directory");
        if (path != null)
            ProjectPath = path;
    }

    internal void CreateProject()
    {
        if (ProjectName is null or "") { _ = parentViewModel.Dialog.ShowDialog("Invalid Project Information", "Project must have a valid name", "Ok"); return; }
        if (ProjectPath == null) { _ = parentViewModel.Dialog.ShowDialog("Invalid Project Information", "Project must have a valid path", "Ok"); return; }
        if (!MatchNames && DirectoryName == null || DirectoryName == "") { _ = parentViewModel.Dialog.ShowDialog("Invalid Project Information", "Project folder must have a valid name", "Ok"); return; }

        string pathToLoad = SelectedDemo?.FileName;
        if (!File.Exists(pathToLoad)) { _ = parentViewModel.Dialog.ShowDialog("Invalid Project Information", "The path to the selected demo could not be found", "Ok"); return; }

        var projectFolder = Path.Combine(ProjectPath, DirectoryName);
        if (!Directory.Exists(projectFolder))
            Directory.CreateDirectory(projectFolder);
        try
        {
            LocalProgress<Services.Install.InstallProgressInfo> progress = new(info =>
            {
                // Do Nothing for Now.
            });

            LocalProgress<Services.Install.InstallLogInfo> log = new(info =>
            {
                parentViewModel.Console.AddConsoleLine(info.LogMessage);
            });

            // Clear Project Directory?
            foreach (var item in Directory.GetFiles(projectFolder, "*", SearchOption.AllDirectories))
                File.Delete(item);

            PackagingUtility.ExtractPackage(pathToLoad, projectFolder, progress, log);
            string launchFile = string.Empty;

            foreach (var item in Directory.GetFiles(projectFolder, "*", SearchOption.AllDirectories))
            {
                if (item.Contains(SelectedDemo.ProjectName))
                {
                    string directory = Path.GetDirectoryName(item);
                    string fileName = Path.GetFileName(item);
                    string newPath = Path.Combine(directory, fileName.Replace(SelectedDemo.ProjectName, ProjectName));
                    if (newPath != item)
                        File.Move(item, newPath);
                }
                else if (item.Contains(SelectedDemo.ProjectName.Replace('.', '_')))
                {
                    string directory = Path.GetDirectoryName(item);
                    string fileName = Path.GetFileName(item);
                    string newPath = Path.Combine(directory, fileName.Replace(SelectedDemo.ProjectName.Replace('.', '_'), ProjectName.Replace('.', '_')));
                    if (newPath != item)
                        File.Move(item, newPath);
                }

                if (item.Contains(".PackageInfo") || item.Contains("CMakeLists"))
                {
                    if (launchFile == String.Empty && item.Contains(".PackageInfo")) { launchFile = item; }

                    var text = File.ReadAllText(item);
                    File.WriteAllText(item, text.Replace(SelectedDemo.ProjectName, ProjectName).Replace(SelectedDemo.ProjectName.Replace('.', '_'), ProjectName.Replace('.', '_')));
                }
            }

            parentViewModel.Popup.DismissPopup();
            if (File.Exists(launchFile))
            {
                parentViewModel.SelectedPackage = new PackageViewModel(parentViewModel, launchFile);

                if (parentViewModel.SelectedPackage.SdkNeedsUpdate)
                {
                    // Wait for Console Output to Finish.
                    parentViewModel.Console.PropertyChanged += ShowFinishedDialog;
                }
                else
                {
                    ShowFinishedDialog(this, new PropertyChangedEventArgs("ConsoleVisible"));
                }
            }
        }
        catch (Exception ex)
        {
            _ = parentViewModel.Dialog.ShowDialog("Error", ex.Message, "Ok");
        }
    }

    private void ShowFinishedDialog(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ConsoleVisible" && !parentViewModel.Console.ConsoleVisible )
        {
            parentViewModel.Dialog.ShowDialog("Project Created", $"Your project is now ready for development at... \r\n\r\n{Path.GetDirectoryName(this.ProjectPath)}", "Ok");
            parentViewModel.Console.PropertyChanged -= ShowFinishedDialog;
        }
    }

    internal void Next()
    {
        if (SelectedDemo != null)
        {
            IsCreate = false;
            ProjectName = SelectedDemo.ProjectName;
        }
        else
            _ = parentViewModel.Dialog.ShowDialog("Missing Information", "Must select project to continue", "Ok");
    }

    internal void Back()
    {
        IsCreate = true;
    }

    internal void GoToAddIns()
    {
        parentViewModel.Plugins.OpenPopup();
    }

    internal void ClosePopup()
    {
        parentViewModel.Popup.DismissPopup();
    }
    #endregion
}

public class CustomerDemoInfoViewModel : ViewModelBase
{
    public PackageInformation Model { get; init; }

    public string FileName { get; set; }

    public string ProjectName { get { return Model.PackageName; } set { Model.PackageName = value; OnPropertyChanged(); } }

    public string Description { get { return Model.PackageDescription; } set { Model.PackageDescription = value; OnPropertyChanged(); } }

    public string Language { get { return Model.ApplicationType == ApplicationType.Dotnet ? "C#" : "C++"; } set { Model.PackageName = value; OnPropertyChanged(); } }

    public bool IsLocal { get { return File.Exists(Model.GetPackageInfoPath()); } set { } }

    public CustomerDemoInfoViewModel(PackageInformation vm)
    {
        Model = vm;
    }
}
