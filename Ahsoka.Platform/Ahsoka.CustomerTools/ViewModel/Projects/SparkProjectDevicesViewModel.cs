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
using static SkiaSharp.HarfBuzz.SKShaper;
using System.IO;
using Ahsoka.System;

namespace Ahsoka.CustomerTools;

/// <summary>
/// Device Methods
/// </summary>
internal partial class SparkProjectDevicesViewModel : ChildViewModelBase<SparkProjectViewModel>
{
    ApplicationViewModel noSubscribeVM =  new ApplicationViewModel(new ContainerDTO() { ContainerName = "Don't Subcribe to OTA" });

    string provisionDeviceFile, provisionSerialNumber, provisionPartNumber, provisionDeviceId = string.Empty;
    System.PlatformFamily provisionDeviceFamily = System.PlatformFamily.OpenViewLinux;

    NewItemView addItemView;
    UserControl selectView;
    UserControl provisionView;
    bool isSingleView = true;

    public List<SparkTrackType> TrackList { get; set; } = new List<SparkTrackType>() { SparkTrackType.Development, SparkTrackType.Canary, SparkTrackType.Beta, SparkTrackType.Release };

    public List<System.PlatformFamily> ProvisionPlatformFamilies { get; set; } = Enum.GetValues<System.PlatformFamily>().ToList();

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

    public System.PlatformFamily ProvisionPlatformFamily
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
          
        _ = Task.Run(() =>
        {
            vm.StartProcess();
            vm.Console.ClearConsole();
           
            var deviceList = new List<ProvisionItem>() { new ProvisionItem(ProvisionPlatformFamily, ProvisionPartNumber, ProvisionSerialNumber, ProvisionDeviceId) };
            if (!IsSingleDevice)
            {
                deviceList.Clear();
                int lineNumber = 0;
                     
                try
                {
                    if (File.Exists(ProvisionDeviceFile))
                    {
                        foreach (var fileLine in File.ReadAllLines(ProvisionDeviceFile))
                        {
                            lineNumber++;
                            var cols = fileLine.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                            if (cols.Length < 4)
                                vm.Console.AddConsoleLine($"Error Importing Line {lineNumber}");

                            deviceList.Add(new ProvisionItem(Enum.Parse<PlatformFamily>(cols[0]), cols[1], cols[2], cols[3]));

                            vm.Console.AddConsoleLine($"Found DeviceID: {cols[3]} to Provision");

                        }
                    }
                }
                catch
                {
                    vm.Console.AddConsoleLine($"Error Importing Line {lineNumber}");
                    throw;
                }

                vm.Console.AddConsoleLine($"Starting Offline Provisioning Using License File: {ProvisionDeviceFile}");
            }
            else
                vm.Console.AddConsoleLine($"Starting Offline Provisioning of Single Display: {ProvisionDeviceId}");

             string teamId = ParentViewModel.TeamID;

            ProvisioningInfo info = new()
            {
                ModuleLicenseList = new()
                {
                    Licenses = new(),
                    TeamId = Guid.Parse(teamId)
                    // Full Device ID Assigned on Server from Gathered Info.
                }
            };


            foreach (var module in AvailableModules)
                if (module.IsSelected)
                {
                    vm.Console.AddConsoleLine($"Requesting License for : {module.Module.ModuleName} (Already Licensed Devices will NOT impact Project License Pool.)");
                    info.ModuleLicenseList.Licenses.Add(new ModuleLicense() { LicenseId = module.Module.ModuleID, LicenseName = module.Module.ModuleName });
                }

            foreach (var item in deviceList)
            {
                info.DeviceInfo = new DeviceDTO()
                {
                    Description = $"Device ({item.ProvisionDeviceId})",
                    GroopID = ParentViewModel.Model.ReleaseDeviceGroup,
                    PartNumber = item.ProvisionPartNumber,
                    SerialNumber = item.ProvisionSerialNumber,
                    UseDeviceIDForLookup = true
                };

                var result = SparkAccountClient.SharedSparkClient.PreProvisionDevice(teamId, item.ProvisionPlatformFamily.ToString(), item.ProvisionDeviceId, info);
                vm.Console.AddConsoleLine($"Provisioning Completed - DeviceID: {result.DeviceInfo.DeviceID} Found and Licensed");
            }

        }).ContinueWith(result =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.Status != TaskStatus.RanToCompletion)
                {
                    if (result.Exception.InnerException is SparkException sparkEX)
                        vm.Console.AddConsoleLine($"Provisioning Failed with Error: {sparkEX.Message}.");
                    else
                        vm.Console.AddConsoleLine($"Provisioning Failed with Error: {result.Exception.Message}.");

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
            ProvisionPlatformFamily = info.PlatformFamily;
        }
    }

    internal async void SelectDeviceFile()
    {
        var path = await FindParent<MainViewModel>().FileLocatorAsync("Select a Device List File (.csv)", "csv", "CSV Device List");
        if (path != null)
            ProvisionDeviceFile = path;
    }
}

internal class ProvisionItem
{
    public PlatformFamily ProvisionPlatformFamily { get; init; }
    public string ProvisionPartNumber { get; init; }
    public string ProvisionSerialNumber { get; init; }
    public string ProvisionDeviceId { get; init; }

    public ProvisionItem(PlatformFamily provisionPlatformFamily, string provisionPartNumber, string provisionSerialNumber, string provisionDeviceId)
    {
        this.ProvisionPlatformFamily = provisionPlatformFamily;
        this.ProvisionPartNumber = provisionPartNumber;
        this.ProvisionSerialNumber = provisionSerialNumber;
        this.ProvisionDeviceId = provisionDeviceId;
    }
}