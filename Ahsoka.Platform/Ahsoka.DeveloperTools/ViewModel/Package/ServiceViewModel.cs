using Ahsoka.ServiceFramework;
using Ahsoka.Services.Data;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;

namespace Ahsoka.DeveloperTools;

internal class ServiceViewModel : ChildViewModelBase<PackageViewModel>
{
    ServiceConfigurationViewModel selectedService;
    bool keysShown = false;

    public ServiceEnableDisable[] ServiceEnableDisableItems { get; init; } = Enum.GetValues<ServiceEnableDisable>();
    
    public DataAccessLevel[] DataAccessLevels { get; init; } = Enum.GetValues<DataAccessLevel>();

    public TransportSocketTypes[] SocketTypes { get; init; } = Enum.GetValues<TransportSocketTypes>();


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

    public bool KeyListShown { get => keysShown; set { keysShown = value; OnPropertyChanged(); } }

    public ServiceViewModel(PackageViewModel parentViewModel)
        : base(parentViewModel)
    {
        LoadServiceInfo();
    }

    private void LoadServiceInfo()
    {
        var items = ClassLoader.GetItemsWithAttribute<AhsokaServiceDataValues>();
     
        foreach (var config in ConfigurationLoader.CreateDefaultConfiguration(false).Services) // Don't load extensions they get loaded later
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

    public void ShowKeys()
    {
        KeyListShown = !KeyListShown;
    }

}

internal class ServiceConfigurationViewModel : ChildViewModelBase<ServiceViewModel>
{
    ExtensionViewModel extension;
    readonly  ServiceConfiguration defaultValues;
    readonly  KnownDataKeys keyValues;
    DataKeyItemViewModel selectedKey;
    string extentionName;
   
    public bool IsExtension { get; set; }
    
    public bool HasUx { get; set; }

    public string ServiceName { get; private set; }

    public string ServiceNameFull { get; }

    public ServiceConfiguration CurrentServiceConfiguration
    {
        get { return ParentViewModel.ParentViewModel.ServiceConfiguration.Services.FirstOrDefault(x => x.ServiceName == this.ServiceName); }
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
                var item = new ServiceConfiguration()
                {
                    ServiceName = defaultValues.ServiceName,
                    TcpConnectionAddress = defaultValues.TcpConnectionAddress,
                    TcpListenAddress = defaultValues.TcpListenAddress,
                    BleServiceUuidBase = defaultValues.BleServiceUuidBase,
                    DataChannel = defaultValues.DataChannel,
                    NotificationChannel = defaultValues.NotificationChannel,
                    IsExtension = defaultValues.IsExtension,
                    Behaviors = defaultValues.Behaviors,
                    SocketType = defaultValues.SocketType
                };

                ParentViewModel.ParentViewModel.ServiceConfiguration.Services.Add(item);
                LoadKeys();
            }
            else
            {
                ParentViewModel.ParentViewModel.ServiceConfiguration.Services.Remove(CurrentServiceConfiguration);
                Keys.Clear();
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(ServicePort)); 
            OnPropertyChanged(nameof(ServiceUUID));
        }
    }

    private void LoadKeys()
    {
        Keys.Clear();
        if (CurrentServiceConfiguration != null && keyValues != null)
            foreach (var key in keyValues.GetDataValues())
                Keys.Add(new DataKeyItemViewModel(this, key));
    }


    public bool ClientAutoStart
    {
        get
        {
            if (CurrentServiceConfiguration != null)
                return CurrentServiceConfiguration.Behaviors.HasFlag(EndPointBehaviors.AutoStart);
            else
                return false;
        }
        set
        {
            if (value)
                CurrentServiceConfiguration.Behaviors = EndPointBehaviors.AutoStart;
            else
                CurrentServiceConfiguration.Behaviors = EndPointBehaviors.AutoStartDisabled;

            OnPropertyChanged();
        }
    }


    public TransportSocketTypes SocketType
    {
        get
        {
            if (CurrentServiceConfiguration != null)
                return CurrentServiceConfiguration.SocketType;
            else
                return TransportSocketTypes.TcpSocket;
        }
        set
        {
            CurrentServiceConfiguration.SocketType = value;
            OnPropertyChanged();
        }
    }



    public int ServicePort
    {
        get
        {
            if (CurrentServiceConfiguration != null)
                return GetPort(CurrentServiceConfiguration?.DataListenURI);
            else
                return 0;
        }
        set
        {
            SetPort(CurrentServiceConfiguration, value);
            OnPropertyChanged();
        }
    }

    public string ServiceUUID
    {
        get
        {
            if (CurrentServiceConfiguration != null)
                return CurrentServiceConfiguration.BleServiceUuidBase;
            else
                return Guid.Empty.ToString();
        }
        set
        {
            CurrentServiceConfiguration.BleServiceUuidBase = value;
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

    public ServiceConfigurationViewModel(ServiceViewModel model, ServiceConfiguration config, KnownDataKeys keyValues, ExtensionViewModel extension = null)
        : base(model)
    {
        this.defaultValues = config;
        if (extension != null)
        {
            this.extension = extension;
            this.IsExtension = true;
            this.HasUx = extension.ExtensionConfiguration.HasUx;
            this.extentionName = extension.ExtensionConfiguration.ExtensionName;
        }
        this.ServiceName = $"{config.ServiceName}";
        this.ServiceNameFull = $"{config.ServiceName} ({(config.IsExtension ? $"from {extentionName}" : "Core Module")})";
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

                ParentViewModel.ParentViewModel.ParentViewModel.NeedsSDKUpdate();

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
