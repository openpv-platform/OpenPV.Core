using Ahsoka.DeveloperTools.Views;
using Ahsoka.Spark;
using Ahsoka.Utility;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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
                SparkProjectTools.CreateDevelopmentKey(ParentViewModel.Model, SelectedDevelopmentKey.ArtifactName.Replace("_DeviceKey", ""));

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
            Title = "Create SSH Key",
            ItemAccepted = CreateKeyAccepted,
            ItemCancelled = Cancel
        };
        this.addItemView = new NewItemView() { DataContext = model };

        this.FindParent<MainViewModel>().Popup.ShowPopup(addItemView);
    }

    internal void Cancel()
    {
        if (addItemView != null)
            this.FindParent<MainViewModel>().Popup.DismissPopup(addItemView);

        addItemView = null;
    }

    internal void CreateKeyAccepted()
    {
        string keyName = ((NewItemViewModel)addItemView.DataContext).ItemText;

        // Need to move this
        string artifactName = SparkProjectTools.GetArtifactName(keyName);

        if (DevelopmentKeys.Any(x => x.ArtifactName == artifactName))
        {
            this.FindParent<MainViewModel>().Dialog.ShowDialog("Duplicate Key", "This Key Name is already used.", "OK");
            return;
        }

        this.FindParent<MainViewModel>().IsWorking = true;
        this.FindParent<MainViewModel>().Popup.DismissPopup(addItemView);

        Task.Run(() =>
        {
            SparkProjectTools.CreateDevelopmentKey(ParentViewModel.Model, keyName);

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
