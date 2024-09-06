using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.Installer;
using Ahsoka.System.Hardware;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools.Views;

public partial class ExtensionMainWindow : Window, ICustomerToolViewModel, INotifyPropertyChanged
{
    string extensionName;
    Type viewModelType;
    PackageInformation packageInformation;

    public static readonly StyledProperty<ExtensionViewModelBase> ViewModelProperty = AvaloniaProperty.Register<ExtensionMainWindow, ExtensionViewModelBase>(nameof(ViewModel), defaultValue: null);
    public static readonly StyledProperty<Grid> PopupGridProperty = AvaloniaProperty.Register<ExtensionMainWindow, Grid>(nameof(PopupUserControl), defaultValue: null);
    public static readonly StyledProperty<UserControl> UserControlProperty = AvaloniaProperty.Register<ExtensionMainWindow, UserControl>(nameof(UserControl), defaultValue: null);

    #region Properties
    public Grid PopupUserControl
    {
        get => GetValue(PopupGridProperty);
        set => SetValue(PopupGridProperty, value);
    }

    public ExtensionViewModelBase ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public UserControl UserControl
    {
        get => GetValue(UserControlProperty);
        set => SetValue(UserControlProperty, value);
    }
    #endregion

    public ExtensionMainWindow()
    {
        InitializeComponent();
    }

    public ExtensionMainWindow(string extensionName, Type viewModelType)
    {
        InitializeComponent();

        this.PopupUserControl = new Grid() ;

        this.extensionName = extensionName;
        this.viewModelType = viewModelType;

        // Reload Hardware next time its requested.
        HardwareInfo.LoadHardwareInfo(CustomerToolPaths.GetToolPath("PlatformSupport"));
        this.DataContext = this;
    }

    public async void OpenConfiguration()
    {
         await SelectPackage();
    }

    public void SaveConfiguration()
    {
        ViewModel?.Save(Path.GetDirectoryName(packageInformation?.GetPackageInfoPath()));
    }

    public void CloseConfiguration()
    {
        packageInformation = null;
        this.UserControl = new UserControl();
    }

    private async Task SelectPackage()
    {
        var dialog = new FilePickerOpenOptions
        {
            Title = "Select an Ahsoka Package File",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Package Info")
                {
                    Patterns = new[]{$"*.PackageInfo.json" }
                }
            }
        };

        var result = await this.StorageProvider.OpenFilePickerAsync(dialog);

        if (result != null)
            foreach (var item in result)
            {
                try
                {
                    packageInformation = PackageInformation.LoadPackageInformation(item.TryGetLocalPath());
                    this.ViewModel = Activator.CreateInstance(viewModelType) as ExtensionViewModelBase;
                    this.ViewModel.InitExtension(this,
                                            HardwareInfo.GetHardwareInfo(packageInformation.PlatformFamily, packageInformation.PlatformQualifier),
                                            Path.GetDirectoryName(packageInformation.GetPackageInfoPath()),
                                            packageInformation.ServiceInfo.RuntimeConfiguration.ExtensionInfo.FirstOrDefault(x => x.ExtensionName == extensionName)?.ConfigurationFile);

                    this.UserControl = this.ViewModel.GetView();
                    
                }
                catch (Exception)
                {  }
            }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    #region Popup Management
    public void ShowPopup(UserControl popupControl)
    {
        PopupUserControl.Children.Add(popupControl);
    }

    public void DismissPopup(UserControl popupControl = null)
    {
        if (popupControl != null)
            PopupUserControl?.Children.Remove(popupControl);
        else
            foreach (var item in PopupUserControl.Children.ToArray())
                PopupUserControl?.Children.Remove(item);

    }

    public UserControl RootPopup
    {
        get
        {
            return PopupUserControl.Children.Count > 0 ? PopupUserControl.Children.First() as UserControl : new UserControl();
        }
    }

    internal DialogViewModel Dialog
    {
        get; 
        set;
    } = new();

    #endregion

    #region Customer View Model 

    public async Task<string> FolderLocatorAsync(string dialogTitle, string defaultPath = null)
    {
        var file = await this.StorageProvider.TryGetFolderFromPathAsync(defaultPath);

        string path = null;
        var dialog = new FolderPickerOpenOptions
        {
            SuggestedStartLocation = file,
            Title = dialogTitle
        };

        var result = await this.StorageProvider.OpenFolderPickerAsync(dialog);

        if (result != null)
            path = result.FirstOrDefault()?.TryGetLocalPath();

        return path;
    }

    public async Task<string> FileLocatorAsync(string dialogTitle, string extension, string fileType, string currentFile = "")
    {
        var folder = await this.StorageProvider.TryGetFolderFromPathAsync(Path.GetDirectoryName(currentFile));

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
        var result = await this.StorageProvider.OpenFilePickerAsync(dialog);

        if (result != null)
            path = result.FirstOrDefault()?.TryGetLocalPath();

        return path;
    }

    public Task<bool> ShowDialog(string dialogTitle, string dialogText, string dialogAcceptText, string dialogRejectText = "")
    {
        return this.Dialog.ShowDialog(dialogTitle,dialogText, dialogAcceptText, dialogRejectText);
    }

    public void StartProcess(bool clearConsole = true)
    {
        this.IsEnabled = false;
    }

    public void FinishProcess(string result, bool closeConsole)
    {
        this.IsEnabled = true;
    }

    public void AddConsoleLine(string consoleLine)
    {
        Debug.WriteLine(consoleLine);
    }

    public void AddConsoleText(string consoleLine)
    {
        Debug.Write(consoleLine);

    }

    public void SdkNeedsUpdate()
    {
    }

    public void RunCommandLineProcess(string processArgs, string workingDirectory, IProgress<CommandLineProgressInfo> progressString, CancellationToken token)
    {
    }


    #endregion
}
