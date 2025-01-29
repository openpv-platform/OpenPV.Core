using Ahsoka.DeveloperTools.Views;
using Ahsoka.Spark;
using Ahsoka.Utility;
using Avalonia.Controls;
using Avalonia.Threading;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

/// <summary>
/// Container - Applications
/// </summary>
internal partial class SparkProjectApplicationViewModel : ChildViewModelBase<SparkProjectViewModel>
{
    UserControl selectView;
    public List<SparkTrackType> TrackList { get; set; } = new List<SparkTrackType>() { SparkTrackType.Development, SparkTrackType.Canary, SparkTrackType.Beta, SparkTrackType.Release };

    public ObservableCollection<ApplicationViewModel> Applications
    {
        get;
        init;

    } = new();

    public ApplicationViewModel SelectedApplication
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
                    this.Applications.Add(new ApplicationViewModel(item, this.ParentViewModel.Model.TeamID.ToString().Substring(0, 8)));
                }

                SelectedApplication = this.Applications.FirstOrDefault(x => x.DTO.ContainerName == tempApp?.DTO.ContainerName && x.DTO.ContainerVersion == tempApp?.DTO.ContainerVersion);
                OnPropertyChanged(nameof(SelectedApplication));
                ParentViewModel.Devices.LoadComplete();
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
                    SparkProjectTools.PromoteApplication(this.ParentViewModel.Model, SelectedApplication.DTO);
                else
                    SparkProjectTools.DemoteApplication(this.ParentViewModel.Model, SelectedApplication.DTO);


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
                SparkProjectTools.RemoveTeamContainer(this.ParentViewModel.Model, SelectedApplication.DTO);

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

internal class ApplicationViewModel
{
    ContainerDTO dto;
    string teamId;

    public string ContainerName
    {
        get
        {
            return dto.ContainerName.Replace($"_{teamId}", "");
        }
    }

    internal ContainerDTO DTO { get { return dto; } }

    internal ApplicationViewModel(ContainerDTO dto, string teamId)
    {
        this.dto = dto;
        this.teamId = teamId;
    }

    public override string ToString()
    {
        return dto.ContainerName;
    }
}