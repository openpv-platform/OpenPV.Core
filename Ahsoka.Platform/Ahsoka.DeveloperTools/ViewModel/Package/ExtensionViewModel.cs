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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class ModuleExtensionsViewModel : ChildViewModelBase<PackageViewModel>, ICustomerToolViewModel
{
    Dictionary<string, ExtensionViewModelBase> extensionUXModules = new();
    Dictionary<string, Assembly> extensionUXLibraries = new();
    ExtensionViewModel selectedExtension;
    public ObservableCollection<ExtensionViewModel> AvailableModules { get; private set; } = new();
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


    private void LoadModules()
    {
        AvailableModules.Clear();

        if (!Design.IsDesignMode)
        {
            try
            {
                AhsokaExtensions.LoadExtensions(CustomerToolPaths.GetToolPath(AhsokaExtensions.ModulesFolder));
                foreach (var item in AhsokaExtensions.Configurations)
                {
                    var module = new ExtensionViewModel(this, item)
                    {
                        IsIncluded = ParentViewModel.ServiceConfiguration.Extensions.Contains(item.ExtensionName)
                    };
                    
                    if (module.ModuleInfoConfiguration.HasUx)
                    {
                        LoadUX(module.ModuleInfoConfiguration);
                    }
                    
                    AvailableModules.Add(module);
                }
            }
            catch (Exception ex)
            {
                ParentViewModel.ParentViewModel.Dialog.ShowDialog("Error Loading Extensions",ex.Message, "Ok");
            }
        }


        // Load Service Extensions Service Info
        foreach (var module in AvailableModules)
        {
            foreach (var service in module.ModuleInfoConfiguration.ServiceConfigurations)
            {
                var config = ParentViewModel.ServiceConfiguration.Services.FirstOrDefault(x => x.ServiceName == service.ServiceName);
                if (config != null)
                {
                    service.IsModuleExtension = true;

                    var viewModel = new ServiceConfigurationViewModel(ParentViewModel.ServiceViewModel, service, null, module);
                    ParentViewModel.ServiceViewModel.Services.Add(viewModel);
                }

            }
        }
    }

    private void LoadUX(ModuleInfoConfiguration item)
    {
        string modulePath = Path.Combine(Path.GetDirectoryName(item.FilePath),"ux",item.UXLibraryFilename);
        if (File.Exists(modulePath))
        {
            try
            {
                if (!extensionUXLibraries.TryGetValue(item.UxPackageName, out Assembly asm))
                    extensionUXLibraries[item.UxPackageName] = asm = Assembly.LoadFile(modulePath);

                foreach (var defType in asm.DefinedTypes)
                {
                    if (defType.IsSubclassOf(typeof(ExtensionViewModelBase)) && !defType.IsAbstract &&
                        defType.Name == item.UxViewModelName)
                    {
                        var vm = ClassLoader.CreateType(defType, null) as ExtensionViewModelBase;
                        extensionUXModules[item.ExtensionName] = vm;
                    }
                }
            }
            catch
            {
                // TODO Notify User of Bad Module
            }
        }
    }

    public void RemoveServiceModule()
    {

    }

    public ModuleExtensionsViewModel(PackageViewModel parentViewModel) : base(parentViewModel)
    {
        LoadModules();
    }

    public void ShowUx(string extensionName)
    {
        var hw = HardwareInfo.GetHardwareInfo(ParentViewModel.PackageInfo.PlatformFamily, ParentViewModel.PackageInfo.PlatformQualifier);
        if (extensionUXModules.TryGetValue(extensionName, out var vm))
             vm.InitExtension(this, hw, Path.GetDirectoryName(ParentViewModel.PackageInfoPath));

        currentPopup = new ExtensionContainerView() { DataContext = this };
        var container = currentPopup.FindControl<Border>("Container");
        var view = extensionUXModules[extensionName]?.GetView();
        container.Child = view;

        ParentViewModel.ParentViewModel.Popup.ShowPopup(currentPopup);
    }

    public void CloseAndDiscardChanges()
    {
        foreach (var item in extensionUXModules)
            item.Value.Close();

        ParentViewModel.ParentViewModel.Popup.DismissPopup(currentPopup);
    }

    public void CloseExtension()
    {
        ParentViewModel.ParentViewModel.Popup.DismissPopup(currentPopup);
    }


    internal void SaveViewModels(string packageInfoFolder)
    {
        foreach (var item in AvailableModules)
        {
            foreach(var service in item.ModuleInfoConfiguration.ServiceConfigurations)
            {
                var config = ParentViewModel.ServiceConfiguration.Services.FirstOrDefault(x => x.ServiceName == service.ServiceName);

                if ( extensionUXModules.TryGetValue(item.ModuleExtensionName, out var vm))
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
        foreach (var item in extensionUXModules)
            if (item.Value.HasChanges())
            {
                return true;
            }

        return false;
    }

    #endregion

}


internal class ExtensionViewModel : ChildViewModelBase<ModuleExtensionsViewModel>
{
    ModuleInfoConfiguration configuration;

    public string ModuleExtensionName { get { return configuration.ExtensionName; } }

    public bool HasUx { get { return configuration.HasUx; } }

    public bool IsSelected { get; set; }

    public bool IsIncluded
    {
        get
        {
            return ParentViewModel.ParentViewModel.ServiceConfiguration.Extensions.Contains(ModuleExtensionName);
        }
        set
        {
            if (value)
            {
                // Add Service Extensions
                if (!ParentViewModel.ParentViewModel.ServiceConfiguration.Extensions.Contains(ModuleExtensionName))
                {
                    ParentViewModel.ParentViewModel.ServiceConfiguration.Extensions.Add(ModuleExtensionName);

                    foreach (var serviceConfig in ModuleInfoConfiguration.ServiceConfigurations)
                    {
                        if (!ParentViewModel.ParentViewModel.ServiceViewModel.Services.Any(x => x.ServiceName == serviceConfig.ServiceName))
                        {
                            serviceConfig.IsModuleExtension = true;
                            var viewModel = new ServiceConfigurationViewModel(ParentViewModel.ParentViewModel.ServiceViewModel, serviceConfig, null, this);
                            ParentViewModel.ParentViewModel.ServiceViewModel.Services.Add(viewModel);
                            viewModel.IsIncluded = true;
                        }
                    }
                }
            }
            else
            {
                ParentViewModel.ParentViewModel.ServiceConfiguration.Extensions.Remove(ModuleExtensionName);

                foreach (var service in ParentViewModel.ParentViewModel.ServiceViewModel.Services.ToArray())
                    if (ModuleInfoConfiguration.ServiceConfigurations.Any(x => x.ServiceName == service.ServiceName))
                    {
                        service.IsIncluded = false;
                        ParentViewModel.ParentViewModel.ServiceViewModel.Services.Remove(service);
                    }
            }
            OnPropertyChanged();
        }
    }

    public ModuleInfoConfiguration ModuleInfoConfiguration { get => configuration; set => configuration = value; }

    public ExtensionViewModel(ModuleExtensionsViewModel model, ModuleInfoConfiguration moduleInfo)
        : base(model)
    {
        configuration = moduleInfo;
    }

    public void IncludeChanged()
    {
        IsIncluded = !IsIncluded;
    }

    internal void ShowUx()
    {
        ParentViewModel.ShowUx(this.ModuleExtensionName);
    }
}