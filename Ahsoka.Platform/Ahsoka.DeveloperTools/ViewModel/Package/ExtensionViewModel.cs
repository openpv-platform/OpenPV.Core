using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class ExtensionsViewModel : ChildViewModelBase<PackageViewModel>, ICustomerToolViewModel
{
    Dictionary<string, ExtensionViewModelBase> extensionUXExtensions = new();
    Dictionary<string, Assembly> extensionUXLibraries = new();
    ExtensionViewModel selectedExtension;
    public ObservableCollection<ExtensionViewModel> AvailableExtensions { get; private set; } = new();
    ExtensionContainerView currentPopup;

    public ExtensionViewModel SelectedExtension
    {
        get { return selectedExtension; }
        set
        {
            selectedExtension = value;
            OnPropertyChanged();
        }
    }

    void ICustomerToolViewModel.SdkNeedsUpdate()
    {
        ParentViewModel.NeedsSDKUpdate();
    }

    private void LoadExtensions()
    {
        AvailableExtensions.Clear();

        if (!Design.IsDesignMode)
        {
            try
            {
                // Find Tools Path
                string path = PlatformSupportPathInfo.FindPlatformSupportPath(AppContext.BaseDirectory, System.Extensions.ExtensionsFolder);
                if (!Directory.Exists(path))
                    path = CustomerToolPaths.GetToolPath(System.Extensions.ExtensionsFolder);


                System.Extensions.LoadExtensions(path);
                foreach (var item in System.Extensions.Configurations)
                {
                    var extension = new ExtensionViewModel(this, item)
                    {
                        IsIncluded = ParentViewModel.ServiceConfiguration.Extensions.Contains(item.ExtensionName)
                    };
                    
                    if (extension.ExtensionConfiguration.HasUx)
                        LoadUX(extension.ExtensionConfiguration);

                    AvailableExtensions.Add(extension);
                }
            }
            catch (Exception ex)
            {
                ParentViewModel.ParentViewModel.Dialog.ShowDialog("Error Loading Extensions",ex.Message, "Ok");
            }
        }


        // Load Service Extensions Service Info
        foreach (var extension in AvailableExtensions)
        {
            foreach (var service in extension.ExtensionConfiguration.ServiceConfigurations)
            {
                var config = ParentViewModel.ServiceConfiguration.Services.FirstOrDefault(x => x.ServiceName == service.ServiceName);
                if (config != null)
                {
                    service.IsExtension = true;

                    var viewModel = new ServiceConfigurationViewModel(ParentViewModel.ServiceViewModel, service, null, extension);
                    ParentViewModel.ServiceViewModel.Services.Add(viewModel);

                    if (extensionUXExtensions.TryGetValue(extension.ExtensionName,out ExtensionViewModelBase extVM))
                    {
                        var hw = HardwareInfo.GetHardwareInfo(ParentViewModel.PackageInfo.PlatformFamily, ParentViewModel.PackageInfo.PlatformQualifier);
                        extVM.InitExtension(this, hw, Path.GetDirectoryName(ParentViewModel.PackageInfoPath));
                    }
                }

            }
        }
    }

    private void LoadUX(ExtensionConfiguration item)
    {
        string extPath = Path.Combine(Path.GetDirectoryName(item.FilePath),"ux",item.UXLibraryFilename);
        if (File.Exists(extPath))
        {
            try
            {
                if (!extensionUXLibraries.TryGetValue(item.UxPackageName, out Assembly asm))
                    extensionUXLibraries[item.UxPackageName] = asm = Assembly.LoadFile(extPath);

                foreach (var defType in asm.DefinedTypes)
                {
                    if (defType.IsSubclassOf(typeof(ExtensionViewModelBase)) && !defType.IsAbstract &&
                        defType.Name == item.UxViewModelName)
                    {
                        var vm = ClassLoader.CreateType(defType, null) as ExtensionViewModelBase;
                        extensionUXExtensions[item.ExtensionName] = vm;
                    }
                }
            }
            catch
            {
                // TODO Notify User of Bad Extension
            }
        }
    }

    public void RemoveServiceExtension()
    {

    }

    public ExtensionsViewModel(PackageViewModel parentViewModel) : base(parentViewModel)
    {
        LoadExtensions();
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

    private void InitExtension(string extensionName)
    {
        var hw = HardwareInfo.GetHardwareInfo(ParentViewModel.PackageInfo.PlatformFamily, ParentViewModel.PackageInfo.PlatformQualifier);
        if (extensionUXExtensions.TryGetValue(extensionName, out var vm))
            vm.InitExtension(this, hw, Path.GetDirectoryName(ParentViewModel.PackageInfoPath));
    }

    public void CloseAndDiscardChanges()
    {
        foreach (var item in extensionUXExtensions)
            item.Value.Close();

        ParentViewModel.ParentViewModel.Popup.DismissPopup(currentPopup);
    }

    public void CloseExtension()
    {
        ParentViewModel.ParentViewModel.Popup.DismissPopup(currentPopup);
    }

    internal void SaveViewModels(string packageInfoFolder)
    {
        foreach (var item in AvailableExtensions)
        {
            foreach(var service in item.ExtensionConfiguration.ServiceConfigurations)
            {
                var config = ParentViewModel.ServiceConfiguration.Services.FirstOrDefault(x => x.ServiceName == service.ServiceName);

                if (config != null && extensionUXExtensions.TryGetValue(item.ExtensionName, out var vm))
                   config.ConfigurationFile = vm.Save(packageInfoFolder, config.ServiceName);
            }
        }
    }

    #region Main ViewModel Interface
    public Task<string> FileLocatorAsync(string dialogTitle, string extension, string fileType, string currentFile = "")
    {
        return ParentViewModel.ParentViewModel.FileLocatorAsync(dialogTitle, extension, fileType, currentFile);
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
        ParentViewModel.ParentViewModel.RunCommandLineProcess(processArgs, workingDirectory, progressString, token);
    }

    public void ShowPopup(UserControl popupControl)
    {
        ParentViewModel.ParentViewModel.Popup.ShowPopup(popupControl);
    }

    public void DismissPopup(UserControl popupControl = null)
    {
        ParentViewModel.ParentViewModel.Popup.DismissPopup(popupControl);
    }

    public string GetConfigurationPath(string serviceName)
    {
        return ParentViewModel.ServiceViewModel.Services.FirstOrDefault(x => x.ServiceName == serviceName)?.CurrentServiceConfiguration.ConfigurationFile;
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

    public string ExtensionName { get { return configuration.ExtensionName; } }

    public bool HasUx { get { return configuration.HasUx; } }

    public bool IsSelected { get; set; }

    public bool IsIncluded
    {
        get
        {
            return ParentViewModel.ParentViewModel.ServiceConfiguration.Extensions.Contains(ExtensionName);
        }
        set
        {
            if (value)
            {
                // Add Service Extensions
                if (!ParentViewModel.ParentViewModel.ServiceConfiguration.Extensions.Contains(ExtensionName))
                {
                    ParentViewModel.ParentViewModel.ServiceConfiguration.Extensions.Add(ExtensionName);

                    foreach (var serviceConfig in ExtensionConfiguration.ServiceConfigurations)
                    {
                        if (!ParentViewModel.ParentViewModel.ServiceViewModel.Services.Any(x => x.ServiceName == serviceConfig.ServiceName))
                        {
                            serviceConfig.IsExtension = true;
                            var viewModel = new ServiceConfigurationViewModel(ParentViewModel.ParentViewModel.ServiceViewModel, serviceConfig, null, this);
                            ParentViewModel.ParentViewModel.ServiceViewModel.Services.Add(viewModel);
                            viewModel.IsIncluded = true;
                        }
                    }
                }
            }
            else
            {
                ParentViewModel.ParentViewModel.ServiceConfiguration.Extensions.Remove(ExtensionName);

                foreach (var service in ParentViewModel.ParentViewModel.ServiceViewModel.Services.ToArray())
                    if (ExtensionConfiguration.ServiceConfigurations.Any(x => x.ServiceName == service.ServiceName))
                    {
                        service.IsIncluded = false;
                        ParentViewModel.ParentViewModel.ServiceViewModel.Services.Remove(service);
                    }
            }
            OnPropertyChanged();
        }
    }

    public ExtensionConfiguration ExtensionConfiguration { get => configuration; set => configuration = value; }

    public ExtensionViewModel(ExtensionsViewModel model, ExtensionConfiguration moduleInfo)
        : base(model)
    {
        configuration = moduleInfo;
    }

    public void IncludeChanged()
    {
        IsIncluded = !IsIncluded;
        ParentViewModel.ParentViewModel.NeedsSDKUpdate();
    }

    internal void ShowUx()
    {
        ParentViewModel.ShowUx(this.ExtensionName);
    }
}