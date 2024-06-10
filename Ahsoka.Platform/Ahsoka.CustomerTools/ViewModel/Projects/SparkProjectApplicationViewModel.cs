using Ahsoka.CustomerTools.Views;
using Ahsoka.Spark;
using EnovationCloud.Core.DataTransferObjects;
using Ahsoka.Utility;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using EnovationCloud.Core.Client;

namespace Ahsoka.CustomerTools;

/// <summary>
/// Container - Applications
/// </summary>
internal partial class SparkProjectApplicationViewModel : ChildViewModelBase<SparkProjectViewModel>
{
    UserControl selectView;
    public List<SparkTrackType> TrackList { get; set; } = new List<SparkTrackType>() { SparkTrackType.Development, SparkTrackType.Canary, SparkTrackType.Beta, SparkTrackType.Release };

    public ObservableCollection<ContainerDTO> Applications
    {
        get;
        init;

    } = new();

    public ContainerDTO SelectedApplication
    {
        get;
        set;
    }

    public ClientContainerSearchDTO ContainerSearchInfo
    {
        get;
        set;
    } = new ClientContainerSearchDTO();


    public SparkProjectApplicationViewModel(SparkProjectViewModel teamVM) : base(teamVM)
    {
    }

    internal void SearchContainers()
    {
        var parent = this.FindParent<SparkProjectEditorViewModel>();
        parent.WorkingText = "Fetching Application Containers...";
        parent.IsWorking = true;
        _ = Task.Run(() =>
        {
            LoadApps();
        }).ContinueWith(result =>
        {
            parent.IsWorking = false;
        });
    }

    internal void ShowApplications()
    {
        selectView = new SparkProjectAppListView() { DataContext = this };
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


    internal void LoadApps()
    {
        var tempApp = SelectedApplication;

        try
        {
            var result = SparkProjectTools.GetTeamContainers(this.ParentViewModel.Model.TeamID, this.ContainerSearchInfo).Containers;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.Applications.Clear();
                foreach (var item in result)
                {
                    item.ContainerName = item.ContainerName.Replace(item.ContainerVersion, "");
                    this.Applications.Add(item);
                }

                SelectedApplication = this.Applications.FirstOrDefault(x => x.ContainerName == tempApp?.ContainerName && x.ContainerVersion == tempApp?.ContainerVersion);
                OnPropertyChanged(nameof(SelectedApplication));
            });
        }
        catch { } // Not Containers
    }

    internal void PromoteApplication()
    {
        UpdateTrack(true);
    }

    internal void DemoteApplication()
    {
        UpdateTrack(false);
    }

    private void UpdateTrack(bool promote)
    {
        if (SelectedApplication != null)
        {
            var parent = this.FindParent<SparkProjectEditorViewModel>();

            parent.IsWorking = true;
            parent.WorkingText = "Updating Track...";

            Task.Run(() =>
            {
                if (promote)
                    SparkProjectTools.PromoteApplication(this.ParentViewModel.Model, SelectedApplication);
                else
                    SparkProjectTools.DemoteApplication(this.ParentViewModel.Model, SelectedApplication);


            }).ContinueWith(result =>
            {

                try
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (result.Status != TaskStatus.RanToCompletion && result.Exception?.InnerException != null)
                        {
                            parent.SetResultText(result.Exception.InnerException.Message, true);
                            return;
                        }

                        LoadApps();

                        parent.SetResultText("Track Changed", false);

                    });
                }
                finally { parent.IsWorking = false; }
            });
        }
    }

    internal async void RemoveApplication()
    {
        if (SelectedApplication != null)
        {
            var parent = this.FindParent<SparkProjectEditorViewModel>();

            var continueRemove = await parent.ParentViewModel.Dialog.ShowDialog("Remove?", "Are you sure you want remove the selected Application Container?", "Yes", "Cancel");
            if (!continueRemove)
                return;


            parent.IsWorking = true;
            parent.WorkingText = "Removing Application...";

            _ = Task.Run(() =>
            {
                SparkProjectTools.RemoveTeamContainer(this.ParentViewModel.Model, SelectedApplication);

            }).ContinueWith(result =>
            {
                try
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (result.Status != TaskStatus.RanToCompletion && result.Exception?.InnerException != null)
                        {
                            parent.SetResultText(result.Exception.InnerException.Message, true);
                            return;
                        }

                        LoadApps();

                        parent.SetResultText("Track Changed", false);

                    });

                }
                finally { parent.IsWorking = false; }
            });
        }
    }



}