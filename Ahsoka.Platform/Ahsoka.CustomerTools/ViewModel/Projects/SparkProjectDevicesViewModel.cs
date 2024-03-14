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
using System;
using Ahsoka.Services.System;

namespace Ahsoka.CustomerTools;

/// <summary>
/// Device Methods
/// </summary>
internal partial class SparkProjectDevicesViewModel : ChildViewModelBase<SparkProjectViewModel>
{
    ApplicationViewModel noSubscribeVM =  new ApplicationViewModel(new ContainerDTO() { ContainerName = "Don't Subcribe to OTA" });

    NewItemView addItemView;
    UserControl selectView;
    UserControl provisionView;

    public List<SparkTrackType> TrackList { get; set; } = new List<SparkTrackType>() { SparkTrackType.Development, SparkTrackType.Canary, SparkTrackType.Beta, SparkTrackType.Release };

    public ObservableCollection<DeviceViewModel> Devices
    {
        get;
        init;

    } = new();

    public DeviceViewModel SelectedDevice
    {
        get;
        set;
    }

    public DeviceSearchDTO DeviceSearchInfo
    {
        get;
        set;
    } = new DeviceSearchDTO();

    public List<ApplicationViewModel> ApplicationList
    {
        get
        {
            var list = ParentViewModel.Applications.Applications.DistinctBy(a => a.DTO.ContainerName).ToList();
            list.Add(noSubscribeVM);          
            return list;
        }
    }

    public ApplicationViewModel SelectedApplication
    {
        get;
        set;
    }

    public ObservableCollection<ProvisionPackageViewModel> AvailableModules
    {
        get;
        set;
    } = new ObservableCollection<ProvisionPackageViewModel>();

    public SparkProjectDevicesViewModel(SparkProjectViewModel teamVM) : base(teamVM)
    {
        SelectedApplication = noSubscribeVM;
    }

    internal void SearchDevices()
    {
        var teamVM = this.FindParent<SparkProjectEditorViewModel>();
        teamVM.WorkingText = "Fetching Devices...";
        teamVM.IsWorking = true;
        _ = Task.Run(() =>
        {
            LoadDevices();

        }).ContinueWith(result =>
        {
            teamVM.IsWorking = false;
        });
    }

    internal void LoadDevices()
    {
        var tempApp = SelectedDevice;

        try
        {
            var result = SparkProjectTools.GetTeamDevices(this.ParentViewModel.Model.TeamID, this.DeviceSearchInfo).Devices;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.Devices.Clear();
                foreach (var item in result)
                    this.Devices.Add(new DeviceViewModel(this.ParentViewModel, item));

                SelectedDevice = this.Devices.FirstOrDefault(x => x.Model.DeviceID == tempApp?.Model.DeviceID);
                OnPropertyChanged(nameof(SelectedDevice));

            });
        }
        catch { } // No Devices
    }

    internal void ShowDevices()
    {
        selectView = new SparkProjectDeviceListView() { DataContext = this };
        this.FindParent<MainViewModel>().Popup.ShowPopup(selectView);
    }

    internal void PromoteDevice()
    {
        UpdateDeviceTrack(true);
    }

    internal void DemoteDevice()
    {
        UpdateDeviceTrack(false);
    }

    internal void AddDevice()
    {
        if (!FindParent<MainViewModel>().ActiveConnection.IsConnected)
        {
            _ = FindParent<MainViewModel>().Dialog.ShowDialog("Device Not Found", "A device must be connected to add to this project", "Ok");
            return;
        }

        string versionString = FindParent<MainViewModel>().ActiveConnection.ConnectedHardwareInfo.VersionInfoes[SystemServiceDataKeys.SDKVersion];

        Version.TryParse(versionString, out Version version);
        var versionCompat = new Version("2.1");
        if (version < versionCompat)
        {
            _ = FindParent<MainViewModel>().Dialog.ShowDialog("Bad SDK", "The connected target can not be licensed until it has been upgraded to SDK version 2.1 or greater.  Use Prep or Upgrade the Installer to install licenses", "Ok");
            return;
        }

        AvailableModules.Clear();
        ParentViewModel.Applications.ContainerSearchInfo = new();
        ParentViewModel.Applications.SearchContainers();
        ParentViewModel.Modules.GetTeamModules();
        foreach (var item in ParentViewModel.Modules.ModuleList)
        {
            this.AvailableModules.Add(new ProvisionPackageViewModel(item));
        }

        provisionView = new SparkDeviceProvisionView() { DataContext = this };
        FindParent<MainViewModel>().Popup.ShowPopup(provisionView);
    }

    internal void ProvisionDevice()
    {
        _ = Task.Run(() =>
        {
            var sparkInfo = new SparkConnectionInfo()
            {
                Url = FindParent<MainViewModel>().SparkLogin.SparkUrl
            };

            ProvisionDisplayRequest provisioningInfo = new();

            provisioningInfo.ApplicationId = Guid.Parse(ParentViewModel.TeamID);
            if (SelectedApplication != null && SelectedApplication.DTO.ContainerID != Guid.Empty)
                provisioningInfo.PackageName = SelectedApplication.DTO.ContainerName.Trim();

            foreach (var module in AvailableModules)
                if (module.IsSelected)
                    provisioningInfo.Licenses.Add(new ModuleLicense() { LicenseId = module.Module.ModuleID, LicenseName = module.Module.ModuleName });

            FindParent<MainViewModel>().Console.ClearConsole();
            var progress = new Progress<string>(info =>
            {
                FindParent<MainViewModel>().Console.AddConsoleLine(info);
            });

            FindParent<MainViewModel>().StartProcess();

            // Provision the Display in the Cloud.
            return SparkCustomerTools.ProvisionDisplayInSpark(FindParent<MainViewModel>().ActiveConnection.Connection,
                                sparkInfo, provisioningInfo, progress);

        }).ContinueWith(result =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.Status != TaskStatus.RanToCompletion)
                    _ = FindParent<MainViewModel>().Dialog.ShowDialog("Provision Failed", "An error occured provisioning the device", "Ok");

                FindParent<MainViewModel>().FinishProcess("", result?.Status == TaskStatus.RanToCompletion);
            });
            LoadDevices();
        });

        CloseEditor();
    }

    internal void LoadComplete()
    {
        OnPropertyChanged(nameof(ApplicationList));
    }

    internal void CloseEditor()
    {
        FindParent<MainViewModel>().Popup.DismissPopup(provisionView);
        provisionView = null;
    }

    internal async void RemoveDevice()
    {
        if (SelectedDevice != null)
        {
            var continueRemove = await this.FindParent<MainViewModel>().Dialog.ShowDialog("Remove?", "Are you sure you want remove the selected Device from Spark?", "Yes", "Cancel");
            if (!continueRemove)
                return;

            var teamVM = this.FindParent<SparkProjectEditorViewModel>();

            teamVM.IsWorking = true;
            teamVM.WorkingText = "Removing Device...";

            _ = Task.Run(() =>
            {
                SparkProjectTools.RemoveTeamDevice(this.ParentViewModel.Model, SelectedDevice.Model);

            }).ContinueWith(result =>
            {
                try
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (result.Status != TaskStatus.RanToCompletion && result.Exception?.InnerException != null)
                        {
                            teamVM.SetResultText(result.Exception.InnerException.Message, true);
                            teamVM.IsWorking = false;
                            return;
                        }

                        LoadDevices();

                        teamVM.SetResultText("Device Removed", false);

                    });
                }
                finally { teamVM.IsWorking = false; }
            });
        }
    }

    internal void RenameDevice()
    {
        if (SelectedDevice != null)
        {
            var model = new NewItemViewModel()
            {
                Label = "Device Name:",
                Title = "Edit Device Name",
                ItemText = SelectedDevice.Description,
                ItemAccepted = UpdateDeviceName,
                ItemCancelled = Cancel
            };
            this.addItemView = new NewItemView() { DataContext = model };

            this.FindParent<MainViewModel>().Popup.ShowPopup(addItemView);
        }
    }


    internal void Back()
    {
        if (selectView != null)
        {
            this.FindParent<MainViewModel>().Popup.DismissPopup(selectView);
            selectView = null;
        }
    }

    internal void Cancel()
    {
        if (addItemView != null)
            this.FindParent<MainViewModel>().Popup.DismissPopup(addItemView);

        addItemView = null;
    }

    private void UpdateDeviceName()
    {
        if (SelectedDevice != null)
        {
            var teamVM = this.FindParent<SparkProjectEditorViewModel>();
            teamVM.IsWorking = true;
            teamVM.WorkingText = "Updating Device Name...";

            string name = ((NewItemViewModel)addItemView.DataContext).ItemText;

            Task.Run(() =>
            {
                SelectedDevice.Model.Description = name;
                SparkProjectTools.RenameDevice(this.ParentViewModel.Model, SelectedDevice.Model);

            }).ContinueWith(result =>
            {

                try
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (result.Status != TaskStatus.RanToCompletion && result.Exception?.InnerException != null)
                        {
                            teamVM.SetResultText(result.Exception.InnerException.Message, true);
                            return;
                        }

                        LoadDevices();

                        Cancel(); // Close Dialog

                        teamVM.SetResultText("Track Changed", false);

                    });
                }
                finally { teamVM.IsWorking = false; }
            });
        }
    }

    private void UpdateDeviceTrack(bool promote)
    {
        if (SelectedDevice != null)
        {
            var teamVM = this.FindParent<SparkProjectEditorViewModel>();
            teamVM.WorkingText = "Updating Track...";
            teamVM.IsWorking = true;

            Task.Run(() =>
            {
                if (promote)
                    SparkProjectTools.PromoteDevice(this.ParentViewModel.Model, SelectedDevice.Model);
                else
                    SparkProjectTools.DemoteDevice(this.ParentViewModel.Model, SelectedDevice.Model);


            }).ContinueWith(result =>
            {
                try
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (result.Status != TaskStatus.RanToCompletion && result.Exception?.InnerException != null)
                        {
                            teamVM.SetResultText(result.Exception.InnerException.Message, true);
                            return;
                        }

                        LoadDevices();

                        teamVM.SetResultText("Track Changed", false);

                    });
                }
                finally { teamVM.IsWorking = false; };
            });
        }
    }
}
