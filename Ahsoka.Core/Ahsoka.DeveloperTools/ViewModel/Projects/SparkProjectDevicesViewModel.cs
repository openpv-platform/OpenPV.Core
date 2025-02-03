using Ahsoka.Core;
using Ahsoka.Core.Spark;
using Ahsoka.Core.Utility;
using Ahsoka.DeveloperTools.Views;
using Avalonia.Controls;
using Avalonia.Threading;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

/// <summary>
/// Device Methods
/// </summary>
internal partial class SparkProjectDevicesViewModel : ChildViewModelBase<SparkProjectViewModel>
{
    ApplicationViewModel noSubscribeVM = new ApplicationViewModel(new ContainerDTO() { ContainerName = "Don't Subcribe to OTA" }, "00000000");

    string provisionDeviceFile, provisionSerialNumber, provisionPartNumber, provisionDeviceId = string.Empty;
    PlatformFamily provisionDeviceFamily = PlatformFamily.OpenViewLinux;

    NewItemView addItemView;
    UserControl selectView;
    UserControl provisionView;
    bool isSingleView = true;

    public List<SparkTrackType> TrackList { get; set; } = new List<SparkTrackType>() { SparkTrackType.Development, SparkTrackType.Canary, SparkTrackType.Beta, SparkTrackType.Release };

    public List<PlatformFamily> ProvisionPlatformFamilies { get; set; } = Enum.GetValues<PlatformFamily>().ToList();

    #region PreProvision
    public string ProvisionDeviceFile
    {
        get { return provisionDeviceFile; }
        set { provisionDeviceFile = value; OnPropertyChanged(); }
    }

    public string ProvisionSerialNumber
    {
        get { return provisionSerialNumber; }
        set { provisionSerialNumber = value; OnPropertyChanged(); }
    }

    public string ProvisionPartNumber
    {
        get { return provisionPartNumber; }
        set { provisionPartNumber = value; OnPropertyChanged(); }
    }

    public PlatformFamily ProvisionPlatformFamily
    {
        get { return provisionDeviceFamily; }
        set { provisionDeviceFamily = value; OnPropertyChanged(); }
    }

    public string ProvisionDeviceId
    {
        get { return provisionDeviceId; }
        set { provisionDeviceId = value; OnPropertyChanged(); }
    }

    public bool IsSingleDevice
    {
        get { return isSingleView; }
        set { isSingleView = value; OnPropertyChanged(); }
    }
    #endregion

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
        AvailableModules.Clear();
        ParentViewModel.Modules.GetTeamModules();
        foreach (var item in ParentViewModel.Modules.ModuleList)
            this.AvailableModules.Add(new ProvisionPackageViewModel(item));

        provisionView = new SparkDeviceProvisionView() { DataContext = this };
        FindParent<MainViewModel>().Popup.ShowPopup(provisionView);
    }

    internal void ProvisionDevice()
    {
        var vm = FindParent<MainViewModel>();

        IProgress<string> progress = new Progress<string>(info =>
        {
            vm.Console.AddConsoleLine(info);
        });

        _ = Task.Run(() =>
        {
            vm.StartProcess();
            vm.Console.ClearConsole();

            List<ProvisionItem> deviceList = null;
            if (IsSingleDevice)
                deviceList = new List<ProvisionItem>() { new ProvisionItem(ProvisionPlatformFamily, ProvisionPartNumber, ProvisionSerialNumber, ProvisionDeviceId) };

            var sparkInfo = new SparkConnectionInfo()
            {
                Url = vm.SparkLogin.SparkUrl
            };

            var request = new ProvisionDisplayRequest()
            {
                ApplicationId = Guid.Parse(ParentViewModel.TeamID),
                Licenses = new()
            };

            foreach (var module in AvailableModules)
                if (module.IsSelected)
                {
                    progress.Report($"Requesting License for : {module.Module.ModuleName} (Already Licensed Devices will NOT impact Project License Pool.)");
                    request.Licenses.Add(new ModuleLicense() { LicenseId = module.Module.ModuleID, LicenseName = module.Module.ModuleName });
                }

            if (!SparkCustomerTools.PreProvisionDevices(ProvisionDeviceFile, sparkInfo, request, progress, deviceList))
                throw new Exception();

        }).ContinueWith(result =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.Status != TaskStatus.RanToCompletion)
                {
                    if (result.Exception.InnerException is SparkException sparkEX)
                        progress.Report($"Provisioning Failed with Error: {sparkEX.Message}.");
                    else
                        progress.Report($"Provisioning Failed with Error: {result.Exception.Message}.");

                    _ = FindParent<MainViewModel>().Dialog.ShowDialog("Provision Failed", "An error occured provisioning the device", "Ok");
                }
                FindParent<MainViewModel>().FinishProcess("", result?.Status == TaskStatus.RanToCompletion);
            });

            // Clear out Items
            ProvisionPartNumber = ProvisionSerialNumber = ProvisionDeviceFile = ProvisionDeviceId = null;
            IsSingleDevice = true;
            ProvisionPlatformFamily = PlatformFamily.OpenViewLinux;

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

    internal void UpdateConnected()
    {
        if (FindParent<MainViewModel>().ActiveConnection.IsConnected)
        {
            var info = FindParent<MainViewModel>().ActiveConnection.ConnectedHardwareInfo;
            ProvisionPartNumber = info.PartNumber;
            ProvisionSerialNumber = info.SerialNumber;
            ProvisionDeviceId = info.DeviceId.Substring(0, 8);
            ProvisionPlatformFamily = info.PlatformFamilyEnum;
        }
    }

    internal async void SelectDeviceFile()
    {
        var path = await FindParent<MainViewModel>().FileLocatorAsync("Select a Device List File (.csv)", "csv", "CSV Device List");
        if (path != null)
            ProvisionDeviceFile = path;
    }
}