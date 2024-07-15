using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class ExtensionsViewModel : ChildViewModelBase<PackageViewModel>, ICustomerToolViewModel
{
    Dictionary<string, ExtensionViewModelBase> extensionUXExtensions = new();
    bool selectionShown, isWorking;
    bool preRelease = true;
    ExtensionViewModel selectedExtension, selectedAvailableExtension;
    public ObservableCollection<ExtensionViewModel> AllExtensions { get; private set; } = new();
    public ObservableCollection<ExtensionViewModel> SelectedExtensions { get; private set; } = new();
    ExtensionContainerView currentPopup;

    public bool AddExtensionsShown
    {
        get { return selectionShown; }
        set
        {
            selectionShown = value;
            OnPropertyChanged();
        }
    }

    public bool IsWorking
    {
        get { return isWorking; }
        set
        {
            isWorking = value;
            OnPropertyChanged();
        }
    }
    public ExtensionViewModel SelectedExtension
    {
        get { return selectedExtension; }
        set
        {
            selectedExtension = value;
            OnPropertyChanged();
        }
    }

    public ExtensionViewModel SelectedAvailableExtension
    {
        get { return selectedAvailableExtension; }
        set
        {
            selectedAvailableExtension = value;
            OnPropertyChanged();
        }
    }

    public bool PreRelease
    {
        get { return preRelease; }
        set
        {
            preRelease = value;
            OnPropertyChanged();
            _ = GetVersions();
        }
    }

    public ExtensionsViewModel(PackageViewModel parentViewModel) : base(parentViewModel)
    {
        LoadExtensionList();
    }

    void ICustomerToolViewModel.SdkNeedsUpdate()
    {
        ParentViewModel.NeedsSDKUpdate();
    }

    private void LoadExtensionList()
    {
        // Load Extension List if neded.
        if (ParentViewModel.ParentViewModel.AvailableExtensions.Count == 0)
            PlatformExtensionsTool.LoadExtensionList(ParentViewModel);

        SelectedExtensions.Clear();
        AllExtensions.Clear();

        try
        {
            if (ParentViewModel.ServiceConfiguration.ExtensionInfo == null)
                ParentViewModel.ServiceConfiguration.ExtensionInfo = new();

            // Add View Model for Each Extension thats Available
            foreach (var item in ParentViewModel.ParentViewModel.AvailableExtensions)
            {
                var extension = new ExtensionViewModel(this, item);

                if (ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.ExtensionInfo.Any(x => x.ExtensionName == item.ExtensionName))
                    SelectedExtensions.Add(extension);
                else
                    AllExtensions.Add(extension);
            }

            ReloadExtensions();
        }
        catch (Exception ex)
        {
            _ = ParentViewModel.ParentViewModel.Dialog.ShowDialog("Error Loading Extensions", ex.Message, "Ok");
        }


        // Load Service Extensions Service Info
        foreach (var extension in SelectedExtensions)
        {
            foreach (var service in extension.ExtensionConfiguration.ServiceConfigurations)
            {
                var config = ParentViewModel.ServiceConfiguration.Services.FirstOrDefault(x => x.ServiceName == service.ServiceName);
                if (config != null)
                {
                    service.IsExtension = true;

                    var viewModel = new ServiceConfigurationViewModel(ParentViewModel.ServiceViewModel, service, null, extension);
                    ParentViewModel.ServiceViewModel.Services.Add(viewModel);
                }
            }
        }

        _ = GetVersions();
    }

    internal void ReloadExtensions()
    {
        extensionUXExtensions.Clear();
        Extensions.LoadExtensions(ParentViewModel.PackageInfo.GetPackageExtentionFolder(), true);

        var extensionList = Extensions.GetExtensionLibraries();

        foreach (var item in SelectedExtensions)
        {
            foreach (var extension in extensionList)
            {
                if (extension.TryGetTarget(out var asm))
                    if (Path.GetFileName(asm.Location) == item.ExtensionConfiguration.UXLibraryFilename)
                        foreach (var defType in asm.DefinedTypes)
                        {
                            if (defType.IsSubclassOf(typeof(ExtensionViewModelBase)) && !defType.IsAbstract &&
                                defType.Name == item.ExtensionConfiguration.UxViewModelName)
                            {
                                var vm = ClassLoader.CreateType(defType, null) as ExtensionViewModelBase;
                                extensionUXExtensions[item.ExtensionName] = vm;
                            }
                        }

            }
        }
    }

    private async Task GetVersions()
    {
        IsWorking = true;
        List<Task> tasks = new List<Task>();
        foreach (var item in SelectedExtensions.Union(AllExtensions))
        {
            var taskItem = PlatformExtensionsTool.GetVersionToDownload(item.ExtensionConfiguration,
                    item.ExtensionConfiguration.PackageName,
                    this.preRelease).ContinueWith((s) =>
                    {
                        if (s != null)
                        {
                            item.AvailableVersion = s.Result;
                        }
                    });


            tasks.Add(taskItem);
        }

        await Task.WhenAll(tasks.ToArray());

        IsWorking = false;
    }

    public void AddExtension()
    {
        AddExtensionsShown = true;
    }

    public void CancelAdd()
    { 
        AddExtensionsShown = false; 
    }

    public async void AddExtensionContinued()
    {
        if (selectedAvailableExtension == null)
            return;

        var selectedExtension = selectedAvailableExtension;
        CancellationTokenSource source = new CancellationTokenSource();
        IsWorking = true;
        await Task.Run(() =>
        {
            selectedExtension.DownloadExtension(PreRelease, source);
            selectedExtension.IsInstalled = true;

        }).ContinueWith((t) =>
        {

            if (!ParentViewModel.ServiceConfiguration.ExtensionInfo.Any(x => x.ExtensionName == selectedExtension.ExtensionName))
            {
                ParentViewModel.ServiceConfiguration.ExtensionInfo.Add(new() { ExtensionName = selectedExtension.ExtensionName });

                foreach (var serviceConfig in selectedExtension.ExtensionConfiguration.ServiceConfigurations)
                {
                    if (!ParentViewModel.ServiceViewModel.Services.Any(x => x.ServiceName == serviceConfig.ServiceName))
                    {
                        serviceConfig.IsExtension = true;
                        var viewModel = new ServiceConfigurationViewModel(ParentViewModel.ServiceViewModel, serviceConfig, null, selectedExtension);
                        ParentViewModel.ServiceViewModel.Services.Add(viewModel);
                        viewModel.IsIncluded = true;
                    }
                }
            }

           
            ParentViewModel.NeedsSDKUpdate();
            SelectedExtensions.Add(selectedExtension);
            AllExtensions.Remove(selectedExtension);

            AddExtensionsShown = false;
            IsWorking = false;
        });

    }

    public void RemoveExtension()
    {
        var extension = SelectedExtension;
        if (extension != null)
        {
            AllExtensions.Add(extension);
            SelectedExtensions.Remove(extension);

            string packageExtensionFolder = extension.GetExtensionDirectory();
            packageExtensionFolder = Path.Combine(packageExtensionFolder, extension.ExtensionName);
            extension.InstalledVersion = "0.0";
            if (Directory.Exists(packageExtensionFolder))
                Directory.Delete(packageExtensionFolder, true);

            ParentViewModel.ServiceConfiguration.ExtensionInfo.Remove(ParentViewModel.ServiceConfiguration.ExtensionInfo.FirstOrDefault(x=>x.ExtensionName == extension.ExtensionName));

            foreach (var service in ParentViewModel.ServiceViewModel.Services.ToArray())
                if (extension.ExtensionConfiguration.ServiceConfigurations.Any(x => x.ServiceName == service.ServiceName))
                {
                    service.IsIncluded = false;
                   
                    ParentViewModel.ServiceViewModel.Services.Remove(service);
                }
        }
    }

    internal void InitExtension(string extensionName)
    {
        if (extensionUXExtensions.TryGetValue(extensionName, out ExtensionViewModelBase extVM))
        {
            var hw = HardwareInfo.GetHardwareInfo(ParentViewModel.PackageInfo.PlatformFamily, ParentViewModel.PackageInfo.PlatformQualifier);

            var info = ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.ExtensionInfo.FirstOrDefault(x => x.ExtensionName == extensionName);

            extVM.InitExtension(this, hw, Path.GetDirectoryName(ParentViewModel.PackageInfoPath), info.ConfigurationFile);
        }
    }

  
    public void ShowUx(string extensionName)
    {
        InitExtension(extensionName);

        currentPopup = new ExtensionContainerView() { DataContext = this };
        var container = currentPopup.FindControl<Border>("Container");
        var view = extensionUXExtensions[extensionName]?.GetView();
        container.Child = view;

        ParentViewModel.ParentViewModel.Popup.ShowPopup(currentPopup);
    }

    #region Extension File Handling

    internal void CloseAndDiscardChanges()
    {
        foreach (var item in extensionUXExtensions)
            item.Value.Close();

        ParentViewModel.ParentViewModel.Popup.DismissPopup(currentPopup);
    }

    internal void CloseExtension()
    {
        ParentViewModel.ParentViewModel.Popup.DismissPopup(currentPopup);
        currentPopup = null;
    }

    internal void SaveViewModels(string packageInfoFolder)
    {
        foreach (var item in SelectedExtensions)
        {
            var info = ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.ExtensionInfo.FirstOrDefault(x => x.ExtensionName == item.ExtensionName);
            if (info != null)
            {
                if (extensionUXExtensions.TryGetValue(item.ExtensionName, out var extension))
                {
                    string newFileName = extension.Save(packageInfoFolder);
                    info.ConfigurationFile = newFileName;
                }
            }
        }
    }

    #endregion

    #region Main ViewModel Interface
    public Task<string> FileLocatorAsync(string dialogTitle, string extension, string fileType, string currentFile = "")
    {
        return ParentViewModel.ParentViewModel.FileLocatorAsync(dialogTitle, extension, fileType, currentFile);
    }

    public Task<string> FolderLocatorAsync(string dialogTitle, string defaultPath = "")
    {
        return ParentViewModel.ParentViewModel.FolderLocatorAsync(dialogTitle, defaultPath);
    }

    public Task<bool> ShowDialog(string dialogTitle, string dialogText, string dialogAcceptText, string dialogRejectText = "")
    {
        return ParentViewModel.ParentViewModel.Dialog.ShowDialog(dialogTitle, dialogText, dialogAcceptText, dialogRejectText);
    }

    public void StartProcess(bool clearConsole = true)
    {
        ParentViewModel.ParentViewModel.Console.ShowConsole();
        if (clearConsole)
            ParentViewModel.ParentViewModel.Console.ClearConsole();
    }

    public void FinishProcess(string result, bool closeConsole)
    {
        ParentViewModel.ParentViewModel.FinishProcess(result,closeConsole);
    }

    public void AddConsoleLine(string consoleLine)
    {
        ParentViewModel.ParentViewModel.Console.AddConsoleLine(consoleLine);
    }

    public void AddConsoleText(string consoleLine)
    {
        ParentViewModel.ParentViewModel.Console.AddConsoleText(consoleLine);
    }

    public void RunCommandLineProcess(string processArgs, string workingDirectory, IProgress<CommandLineProgressInfo> progressString, CancellationToken token)
    {
        ParentViewModel.ParentViewModel.RunCommandLineProcess(processArgs, workingDirectory, progressString, token, ParentViewModel.ParentViewModel.SelectedPackage.GetCommandLinePath());
    }

    public void ShowPopup(UserControl popupControl)
    {
        ParentViewModel.ParentViewModel.Popup.ShowPopup(popupControl);
    }

    public void DismissPopup(UserControl popupControl = null)
    {
        ParentViewModel.ParentViewModel.Popup.DismissPopup(popupControl);
    }

    [Obsolete]
    public string GetConfigurationPath(string extensionName)
    {
        string returnValue = ParentViewModel.ServiceConfiguration.ExtensionInfo.FirstOrDefault(x => x.ExtensionName == this.selectedExtension.ExtensionName).ConfigurationFile;
        if (!String.IsNullOrEmpty(returnValue))
            return returnValue;

       return ParentViewModel.ServiceViewModel.Services.FirstOrDefault(x => x.ServiceName == extensionName)?.CurrentServiceConfiguration.ConfigurationFile;
    }

    internal bool HasChanges()
    {
        foreach (var item in extensionUXExtensions)
            if (item.Value.HasChanges())
            {
                return true;
            }

        return false;
    }
    #endregion
}


internal class ExtensionViewModel : ChildViewModelBase<ExtensionsViewModel>
{
    ExtensionConfiguration configuration;
    bool installed = false;

    public string ExtensionName { get { return configuration.ExtensionName; } }
    
    public string InstalledVersion { get { return configuration.InstalledVersion ?? "0.0"; } set { configuration.InstalledVersion = value; OnPropertyChanged(); } }

    public string AvailableVersion 
    { 
        get { return configuration.AvailableVersion ?? "0.0"; } 
        set 
        { 
            configuration.AvailableVersion = value; 
            OnPropertyChanged(); 
            IsInstalled = configuration.AvailableVersion == configuration.InstalledVersion;
            OnPropertyChanged(nameof(IsInstalled)); 
        } 
    }
    
    public bool HasUx { get { return configuration.HasUx; } }

    public bool IsInstalled { get { return installed; } set { installed = value; OnPropertyChanged(); }}

    public ExtensionConfiguration ExtensionConfiguration { get => configuration; set => configuration = value; }

    public ExtensionViewModel(ExtensionsViewModel model, ExtensionConfiguration moduleInfo)
        : base(model)
    {
        configuration = moduleInfo;

        string extensionFolder = GetExtensionDirectory();
        string extensionVersionFile = GetExtensionFile();

        // Use Local file if available
        bool isLocal = Path.Exists(extensionVersionFile);
        if (isLocal)
            configuration = JsonUtility.Deserialize<ExtensionConfiguration>(File.ReadAllText(extensionVersionFile));

    }

    public void UpdateExtension()
    {
        var token = new CancellationTokenSource();
        
        ParentViewModel.IsWorking = true;
        Task.Run(() =>
        {
            DownloadExtension(ParentViewModel.PreRelease, token);
            this.IsInstalled = true;
       
        }).ContinueWith((t) =>
        {
            ParentViewModel.IsWorking = false;
        });
    }

    public async void DownloadExtension(bool preRelease, CancellationTokenSource token)
    {
        string directory = GetExtensionDirectory();
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);

        Directory.CreateDirectory(directory);

        this.InstalledVersion = AvailableVersion;

        string extensionVersionFile = GetExtensionFile();
        File.WriteAllText(extensionVersionFile, JsonUtility.Serialize(ExtensionConfiguration));

        string libDirectory = Path.Combine(directory, "lib");

        if (!Directory.Exists(libDirectory))
            Directory.CreateDirectory(libDirectory);

        await PlatformExtensionsTool.DownloadPackage(ParentViewModel.ParentViewModel, token, configuration.PackageName, this.InstalledVersion, false, libDirectory, true);

        if (ExtensionConfiguration.HasUx)
        {
            string uxDirectory = Path.Combine(directory, "ux");

            if (!Directory.Exists(uxDirectory))
                Directory.CreateDirectory(uxDirectory);

            await PlatformExtensionsTool.DownloadPackage(ParentViewModel.ParentViewModel, token, configuration.UxPackageName, this.InstalledVersion, true, uxDirectory, false);
        }

        ParentViewModel.ReloadExtensions();
    }

    public string GetExtensionFile()
    {
        string extensionFolder = GetExtensionDirectory();
        return Path.Combine(extensionFolder, $"{configuration.PackageName}.extensioninfo.json");
    }

    public string GetExtensionDirectory()
    {
        string packageExtensionFolder = ParentViewModel.ParentViewModel.PackageInfo.GetPackageExtentionFolder();
        string extensionFolder = Path.Combine(packageExtensionFolder, ExtensionConfiguration.ExtensionName);
        return extensionFolder;
    }

    internal void ShowUx()
    {
        ParentViewModel.ShowUx(this.ExtensionName);
    }
}