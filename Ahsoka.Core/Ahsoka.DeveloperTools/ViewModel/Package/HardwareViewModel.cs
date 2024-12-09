using Ahsoka.Services.Device;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Ahsoka.DeveloperTools;

internal class HardwareViewModel : ChildViewModelBase<PackageViewModel>
{
    HardwareInfoViewModel selectedService;
    
    public ObservableCollection<HardwareInfoViewModel> HardwareInfoList
    {
        get;
        set;
    } = new ObservableCollection<HardwareInfoViewModel>();

    public HardwareInfoViewModel SelectedHardwareInfo
    {
        get { return selectedService; }
        set
        {
            selectedService = value;
            OnPropertyChanged();
        }
    }

    public HardwareViewModel(PackageViewModel parentViewModel)
        : base(parentViewModel)
    {
        LoadInfo();
    }

    private void LoadInfo()
    {
        foreach (var item in ParentViewModel.PackageInfo.HardwareConfiguration.HardwareItems)
            HardwareInfoList.Add(new HardwareInfoViewModel(this, item));

        SelectedHardwareInfo = HardwareInfoList.FirstOrDefault();
    }

    public void ResetHardware()
    {
        ParentViewModel.PackageInfo.HardwareConfiguration.HardwareItems.Clear();
        HardwareInfoList.Clear();
        switch (ParentViewModel.PlatformFamily)
        {
            case PlatformFamily.OpenViewLinux:
                AddHardwareItem("WiFi Combo Module", DeviceTypes.OnboardWirelessComboModule);
                break;
            case PlatformFamily.OpenViewLinuxPro:
                AddHardwareItem("IO Control Module", DeviceTypes.IoControlModule);
                AddHardwareItem("WiFi Module", DeviceTypes.OnboardWifiModule);
                AddHardwareItem("Bluetooth Module", DeviceTypes.OnboardBtModule);
                AddHardwareItem("Ethernet", DeviceTypes.OnboardEthernet);
                break;
            default:
                AddHardwareItem("WiFi Module", DeviceTypes.OnboardWifiModule);
                AddHardwareItem("Bluetooth Module", DeviceTypes.OnboardBtModule);
                break;
        }
    }

    public void RemoveHardware()
    {
        if (SelectedHardwareInfo != null)
        {
            ParentViewModel.PackageInfo.HardwareConfiguration.HardwareItems.Remove(SelectedHardwareInfo.HardwareModel);
            HardwareInfoList.Remove(SelectedHardwareInfo);
            SelectedHardwareInfo = HardwareInfoList.FirstOrDefault();
        }
    }

    public void AddHardware()
    {
        AddHardwareItem("New Module", DeviceTypes.DeviceTypeUnknown);
    }

    private void AddHardwareItem(string name, DeviceTypes moduleType)
    {
        var item = new HardwareItem() { DeviceType = moduleType, Name = name };
        ParentViewModel.PackageInfo.HardwareConfiguration.HardwareItems.Add(item);
        HardwareInfoList.Add(new HardwareInfoViewModel(this, item));
        SelectedHardwareInfo = HardwareInfoList.Last();
    }
}

internal class HardwareInfoViewModel : ChildViewModelBase<HardwareViewModel>
{
    HardwareItem hardwareModel;
    string supportedServices = "(None)";

    public HardwareItem HardwareModel { get => hardwareModel; set => hardwareModel = value; }

    public DeviceTypes[] DeviceTypes { get; init; } = Enum.GetValues<DeviceTypes>();


    public DeviceTypes DeviceType
    {
        get
        {
            return HardwareModel.DeviceType;
        }
        set
        {
            HardwareModel.DeviceType = value;
            supportedServices = String.Join(',',DeviceInfo.GetSupportedServices(this.DeviceType));
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get
        {
            return HardwareModel.Name;
        }
        set
        {
            HardwareModel.Name = value;
            OnPropertyChanged();
        }
    }


    public string SupportedServices
    {
        get
        {
            return supportedServices;
        }
        set
        {
            supportedServices = value;
            OnPropertyChanged();
        }
    }

    public string CustomStartup
    {
        get
        {
            return HardwareModel.CustomStartup;
        }
        set
        {
            HardwareModel.CustomStartup = value;
            OnPropertyChanged();
        }

    }

    public HardwareInfoViewModel(HardwareViewModel parent, HardwareItem model)
        : base(parent)
    {
        this.HardwareModel = model;

        supportedServices = String.Join(',', DeviceInfo.GetSupportedServices(this.DeviceType));
    }



}
