using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static NetMQ.NetMQSelector;

namespace Ahsoka.CustomerTools;

internal class ServiceViewModel : ChildViewModelBase<PackageViewModel>
{
    ServiceConfigurationViewModel selectedService;

    public ServiceEnableDisable[] ServiceEnableDisableItems { get; init; } = Enum.GetValues<ServiceEnableDisable>();
    public DataAccessLevel[] DataAccessLevels { get; init; } = Enum.GetValues<DataAccessLevel>();

    public ObservableCollection<ServiceConfigurationViewModel> Services
    {
        get;
        set;
    } = new ObservableCollection<ServiceConfigurationViewModel>();

    public ServiceConfigurationViewModel SelectedService
    {
        get { return selectedService; }
        set
        {
            selectedService = value;
            OnPropertyChanged();
        }
    }

    public RuntimeConfiguration ServiceConfiguration { get { return ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration; } }

    public ServiceEnableDisable CustomServiceEnabled
    {
        get { return ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.CustomServiceEnabled; }
        set
        {
            ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.CustomServiceEnabled = value;
            OnPropertyChanged();
        }
    }

    public ServiceViewModel(PackageViewModel parentViewModel)
        : base(parentViewModel)
    {
        LoadServiceInfo();
    }


    private void LoadServiceInfo()
    {
       
        var items = ClassLoader.GetItemsWithAttribute<AhsokaServiceDataValues>();
     
        foreach (var config in ConfigurationLoader.CreateDefaultConfiguration().Services)
        {
            KnownDataKeys keyValues = null;
            var keyItem = items.Keys.FirstOrDefault(x => x.ServiceName == config.ServiceName);
            if (keyItem != null)
                keyValues = ClassLoader.CreateType(items[keyItem], null) as KnownDataKeys;
        
            var viewModel = new ServiceConfigurationViewModel(this, config, keyValues);

            Services.Add(viewModel);
        }

        OnPropertyChanged(nameof(Services));
        SelectedService = Services.FirstOrDefault();
    }

}

internal class ServiceConfigurationViewModel : ChildViewModelBase<ServiceViewModel>
{
    readonly  ServiceConfiguration defaultValues;
    readonly  KnownDataKeys keyValues;
    DataKeyItemViewModel selectedKey;

    public string ServiceName { get; private set; }

    public ServiceConfiguration CurrentServiceConfiguration
    {
        get { return ParentViewModel.ServiceConfiguration.Services.FirstOrDefault(x => x.ServiceName == this.ServiceName); }
    }

    public DataKeyItemViewModel SelectedKey
    {
        get { return selectedKey; }
        set
        {
            selectedKey = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<DataKeyItemViewModel> Keys
    {
        get; set;
    } = new ObservableCollection<DataKeyItemViewModel>();


    public bool IsIncluded
    {
        get
        {
            return CurrentServiceConfiguration != null;
        }
        set
        {
            if (value == IsIncluded)
                return;

            if (value && CurrentServiceConfiguration == null)
            {
                var item = new ServiceConfiguration() { 
                    ServiceName = defaultValues.ServiceName,
                    TcpConnectionAddress = defaultValues.TcpConnectionAddress, 
                    TcpListenAddress = defaultValues.TcpListenAddress,  
                    BleServiceUuidBase = defaultValues.BleServiceUuidBase,
                    DataChannel = defaultValues.DataChannel, 
                    NotificationChannel = defaultValues.NotificationChannel
                };

                ParentViewModel.ServiceConfiguration.Services.Add(item);
                LoadKeys();
            }
            else
            {
                ParentViewModel.ServiceConfiguration.Services.Remove(CurrentServiceConfiguration);
                Keys.Clear();
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(ServicePort));
        }
    }

    private void LoadKeys()
    {
        Keys.Clear();
        if (CurrentServiceConfiguration != null && keyValues != null)
            foreach (var key in keyValues.GetDataValues())
                Keys.Add(new DataKeyItemViewModel(this, key));
    }

    public int ServicePort
    {
        get { return GetPort(CurrentServiceConfiguration?.DataListenURI); }
        set
        {
            SetPort(CurrentServiceConfiguration, value);
            OnPropertyChanged();
        }
    }


    private int GetPort(string listenAddress)
    {
        try
        {
            return int.Parse(listenAddress.Substring(listenAddress.LastIndexOf(':') + 1));
        }
        catch
        {
            return -1;
        }
    }

    private void SetPort(ServiceConfiguration definition, int newPort)
    {
        definition.DataChannel = newPort;
        definition.NotificationChannel = newPort + 1000;

        OnPropertyChanged(nameof(ServicePort));
    }

    public void IncludeChanged()
    {
        IsIncluded = !IsIncluded;
        OnPropertyChanged(nameof(IsIncluded));
        OnPropertyChanged(nameof(Keys));
    }


    public void SelectAll()
    {
        foreach (var key in Keys)
            key.IsIncluded = true;
    }

    public void UnselectAll()
    {
        foreach (var key in Keys)
            key.IsIncluded = false;
    }

    public ServiceConfigurationViewModel(ServiceViewModel model, ServiceConfiguration config, KnownDataKeys keyValues)
        : base(model)
    {
        this.defaultValues = config;
        this.ServiceName = config.ServiceName;
        this.keyValues = keyValues;
       
        LoadKeys();

    }

    internal class DataKeyItemViewModel : ChildViewModelBase<ServiceConfigurationViewModel>
    {
        public DataAccessLevel DataAccessLevel
        {
            get { return CurrentKeyItem != null ? CurrentKeyItem.DataAccessLevel : DataAccessLevel.InternalOnly; }
            set { if (CurrentKeyItem != null) CurrentKeyItem.DataAccessLevel = value; }
        }

        public string Key { get; }

        public DataServiceKeyItem CurrentKeyItem
        {
            get { return ParentViewModel.CurrentServiceConfiguration.DataKeyValues.FirstOrDefault(x => x.KeyName == this.Key); }
        }

        public bool IsIncluded
        {
            get
            {
                return CurrentKeyItem != null;
            }
            set
            {
                if (value == IsIncluded)
                    return;

                if (value && CurrentKeyItem == null)
                {
                    ParentViewModel.CurrentServiceConfiguration.DataKeyValues.Add(new DataServiceKeyItem() {  KeyName = this.Key, DataAccessLevel = DataAccessLevel.Diagnostic });
                }
                else
                {
                    ParentViewModel.CurrentServiceConfiguration.DataKeyValues.Remove(CurrentKeyItem);
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(DataAccessLevel));
            }
        }

        public DataKeyItemViewModel(ServiceConfigurationViewModel model, string keyValue)
            : base(model)
        {
            this.Key = keyValue;
        }

        public void IncludeChanged()
        {
            IsIncluded = !IsIncluded;
            OnPropertyChanged(nameof(IsIncluded));
        }
    }

}