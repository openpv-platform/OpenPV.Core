using Ahsoka.DeveloperTools.Views;
using Ahsoka.Installer;
using Ahsoka.Spark;
using Ahsoka.Utility;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Threading;
using Microsoft.IdentityModel.Tokens;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Ahsoka.Spark.SparkProjectTools;

namespace Ahsoka.DeveloperTools;


/// <summary>
/// Artifacts
/// </summary>
internal partial class SparkProjectArtifactsViewModel : ChildViewModelBase<SparkProjectViewModel>
{
    UserControl selectView;
    NewItemView addItemView;
    KeyArtifactViewModel selectedKey;

    public ObservableCollection<KeyArtifactViewModel> DevelopmentKeys
    {
        get;
        init;

    } = new();

    public KeyArtifactViewModel SelectedDevelopmentKey
    {
        get { return selectedKey; }
        set { selectedKey = value; OnPropertyChanged(); }
    }

    public SparkProjectArtifactsViewModel(SparkProjectViewModel teamVM) : base(teamVM)
    {
    }


    internal void ShowArtifacts()
    {
        selectView = new SparkProjectArtifactView() { DataContext = this };
        this.FindParent<MainViewModel>().Popup.ShowPopup(selectView);
    }

    internal void Back()
    {
        if (selectView != null)
        {
            this.FindParent<MainViewModel>().Popup.DismissPopup(selectView);
            selectView = null;
        }
    }

    internal void LoadKeys()
    {
        var tempApp = SelectedDevelopmentKey;

        try
        {
            this.DevelopmentKeys.Clear();
            foreach (var item in SparkProjectTools.GetTeamDevelopmentKeys(this.ParentViewModel.Model))
                this.DevelopmentKeys.Add(new KeyArtifactViewModel(this.ParentViewModel, item));
        }
        catch { } // No Keys

        SelectedDevelopmentKey = this.DevelopmentKeys.FirstOrDefault(x => x.ArtifactName == tempApp?.ArtifactName);
        SelectedDevelopmentKey ??= this.DevelopmentKeys.FirstOrDefault();
    }

    internal async void RemoveKey()
    {
        if (SelectedDevelopmentKey != null)
        {
            var teamVM = this.FindParent<SparkProjectEditorViewModel>();
            var continueRemove = await this.FindParent<MainViewModel>().Dialog.ShowDialog("Remove?", "Are you sure you want remove the selected key?", "Yes", "Cancel");
            if (!continueRemove)
                return;

            teamVM.IsWorking = true;

            _ = Task.Run(() =>
            {
                SparkProjectTools.RemoveDevelopmentKey(this.ParentViewModel.Model, SelectedDevelopmentKey.Model);

                // Delete Local if App Cert
                if (SelectedDevelopmentKey.ArtifactType == KeyTypes.PackageSigningKey || SelectedDevelopmentKey.ArtifactType == KeyTypes.ServiceKey)
                {
                    string path = PackageInformation.GetPackageCertPath(SelectedDevelopmentKey.DeviceKeyName, SelectedDevelopmentKey.ArtifactType);
                    if (File.Exists(path))
                        File.Delete(path);
                }


            }).ContinueWith(result =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (result.Status != TaskStatus.RanToCompletion && result.Exception?.InnerException != null)
                    {
                        teamVM.SetResultText(result.Exception.InnerException.Message, true);
                        return;
                    }

                    LoadKeys();

                    teamVM.SetResultText("Device Removed", false);

                });

                teamVM.IsWorking = false;
            });
        }
    }

    internal void DownloadKey()
    {
        if (SelectedDevelopmentKey != null)
        {
            var teamVM = this.FindParent<SparkProjectEditorViewModel>();
            teamVM.IsWorking = true;

            Task.Run(() =>
            {
                SparkProjectTools.CreateDevelopmentKey(ParentViewModel.Model, SelectedDevelopmentKey.DeviceKeyName, SelectedDevelopmentKey.ArtifactType);

            }).ContinueWith(result =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (result.Status != TaskStatus.RanToCompletion && result.Exception?.InnerException != null)
                    {
                        teamVM.SetResultText(result.Exception.InnerException.Message, true);
                        return;
                    }

                    LoadKeys();

                    teamVM.SetResultText("Key Downloaded", false);

                });

                teamVM.IsWorking = false;
            });
        }
    }

    internal void CreateKey()
    {
        var model = new NewItemViewModel()
        {
            Hint = "Key will be created and shared with the team as well as added to the .ssh folder.",
            Label = "Key Name:",
            Title = "Create Device Key",
            ItemAccepted = CreateAccepted,
            ItemCancelled = Cancel,
            Tag = KeyTypes.DeviceKey
        };
        this.addItemView = new NewItemView() { DataContext = model };

        this.FindParent<MainViewModel>().Popup.ShowPopup(addItemView);
    }


    internal void CreateAppCert()
    {
        var model = new NewItemViewModel()
        {
            Hint = "Certificate will be created and shared with the team for use when compiling applications.",
            Label = "Key Name:",
            Title = "Create Application Certificate",
            ItemAccepted = CreateAccepted,
            ItemCancelled = Cancel,
            Tag = KeyTypes.PackageSigningKey
        };
        this.addItemView = new NewItemView() { DataContext = model };

        this.FindParent<MainViewModel>().Popup.ShowPopup(addItemView);
    }

    internal void CreateServiceCert()
    {
        var model = new NewItemViewModel()
        {
            Hint = "Certificate will be created and shared with the team for use when compiling applications.",
            Label = "Key Name:",
            Title = "Create Service Certificate",
            ItemAccepted = CreateAccepted,
            ItemCancelled = Cancel,
            Tag = KeyTypes.ServiceKey
        };
        this.addItemView = new NewItemView() { DataContext = model };

        this.FindParent<MainViewModel>().Popup.ShowPopup(addItemView);
    }

    internal async void ImportKeys()
    {
        string certFilePath = Path.GetDirectoryName(PackageInformation.GetPackageCertPath("Blank", KeyTypes.DeviceKey));
        var fileInfo = await this.FindParent<MainViewModel>().FileLocatorAsync("Select Key (ssh, appcert, serviceCert)", "*", "Keys (ssh, *.appcert and *.servicecert)");
        if (fileInfo == null)
            return;

        bool isAppCert = fileInfo.ToLower().EndsWith("cert");
        string keyName = Path.GetFileNameWithoutExtension(fileInfo);
        KeyTypes type = KeyTypes.PackageSigningKey;

        if (isAppCert)
        {
            type = Path.GetExtension(certFilePath).ToLower() == ".appcert" ?  KeyTypes.PackageSigningKey : KeyTypes.ServiceKey;
            bool isAppCertLocal = Path.GetDirectoryName(fileInfo) == certFilePath;
           
            if (!isAppCertLocal)
                File.Copy(fileInfo, Path.Combine(certFilePath, keyName), true);
        }
        else
        {
            // Assumes the Key is in the .ssh folder
            type = KeyTypes.DeviceKey;
        }

        // Upload Key
        await Task.Run(() =>
        {
            SparkProjectTools.CreateDevelopmentKey(ParentViewModel.Model, keyName, type);

        }).ContinueWith(result =>
        {
            var teamVM = this.FindParent<SparkProjectEditorViewModel>();

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.Status != TaskStatus.RanToCompletion && result.Exception?.InnerException != null)
                {
                    teamVM.SetResultText(result.Exception.InnerException.Message, true);
                    return;
                }

                LoadKeys();

                teamVM.SetResultText("Key Created", false);

            });

            teamVM.IsWorking = false;
        });

    }


    internal void Cancel()
    {
        if (addItemView != null)
            this.FindParent<MainViewModel>().Popup.DismissPopup(addItemView);

        addItemView = null;
    }

    internal void CreateAccepted()
    {
        string keyName = ((NewItemViewModel)addItemView.DataContext).ItemText;
        KeyTypes type = (KeyTypes)((NewItemViewModel)addItemView.DataContext).Tag;

        // Need to move this
        string artifactName = SparkProjectTools.GetArtifactName(keyName, type);

        if (DevelopmentKeys.Any(x => x.ArtifactName == artifactName))
        {
            this.FindParent<MainViewModel>().Dialog.ShowDialog("Duplicate Key", "This Key Name is already used.", "OK");
            return;
        }

        this.FindParent<MainViewModel>().IsWorking = true;
        this.FindParent<MainViewModel>().Popup.DismissPopup(addItemView);

        Task.Run(() =>
        {
            SparkProjectTools.CreateDevelopmentKey(ParentViewModel.Model, keyName, type);

        }).ContinueWith(result =>
        {
            var teamVM = this.FindParent<SparkProjectEditorViewModel>();

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.Status != TaskStatus.RanToCompletion && result.Exception?.InnerException != null)
                {
                    teamVM.SetResultText(result.Exception.InnerException.Message, true);
                    return;
                }

                LoadKeys();

                teamVM.SetResultText("Key Created", false);

            });

            teamVM.IsWorking = false;
        });
    }

}
