using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Security;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Ahsoka.DeveloperTools;

internal class ServiceViewModel : ChildViewModelBase<PackageViewModel>
{
    ServiceConfigurationViewModel selectedService;
    bool securityShown = false;
    string apiKey = string.Empty;

    public ServiceEnableDisable[] ServiceEnableDisableItems { get; init; } = Enum.GetValues<ServiceEnableDisable>();

    public ChallengeOptions[] RequireChallengeOptions { get; init; } = Enum.GetValues<ChallengeOptions>();

    public ChallengeOptions RequireChallenge
    {
        get
        {
            return ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.RequireChallenge;
        }
        set
        {
            ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.RequireChallenge = value;
            OnPropertyChanged();
        }

    }

    public string ApiKey
    {
        get
        {
            return apiKey;
        }
        set
        {
            if (apiKey != value)
                apiKey = value;

            OnPropertyChanged();
        }

    }

    public List<string> ServiceKeys
    {
        get
        {
            string path = Path.GetDirectoryName(PackageInformation.GetPackageCertPath("Test", KeyTypes.ServiceKey));
            List<string> returnList = new List<string>() { "(None)" };

            if (Path.Exists(path))
                returnList.AddRange(Directory.GetFiles(path, "*.servicecert").Select(Path.GetFileNameWithoutExtension));

            return returnList;
        }
        set { }
    }

    public string PrivateServiceKeyFile
    {
        get
        {
            string path = PackageInformation.GetPackageCertPath(PrivateServiceKey, KeyTypes.ServiceKey);
            if (File.Exists(path))
                return path;

            return string.Empty;
        }

    }

    public string PrivateServiceKey
    {
        get
        {

            if (ServiceKeys.Contains(ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.ServiceKey))
                return ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.ServiceKey;
            else
                return "(None)";
        }
        set
        {
            ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.ServiceKey = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ApiKey));
        }
    }
    
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

    public bool SecurityShown { get => securityShown; set { securityShown = value; OnPropertyChanged(); } }

    public ServiceViewModel(PackageViewModel parentViewModel)
        : base(parentViewModel)
    {
        LoadServiceInfo();
    }

    private void LoadServiceInfo()
    {
        var items = ClassLoader.GetItemsWithInterface(typeof(AhsokaMessagesBase));

        foreach (var config in ConfigurationLoader.CreateDefaultConfiguration(false).Services) // Don't load extensions they get loaded later
        {
            var viewModel = new ServiceConfigurationViewModel(this, config);
            Services.Add(viewModel);
        }

        OnPropertyChanged(nameof(Services));
        SelectedService = Services.FirstOrDefault();
    }

    public void GenerateKey()
    {
        string path = PackageInformation.GetPackageCertPath(PrivateServiceKey, KeyTypes.ServiceKey);

        if (!File.Exists(path))
            return;

        var teamID = ParentViewModel.PackageInfo.ApplicationId;
        if (teamID == Guid.Empty)
        {
            ParentViewModel.ParentViewModel.Dialog.ShowDialog("Select a Project For this Package", "Please select a project on the Packaging Info page.  This Project ID will be used when validating the API key at runtime.", "Ok");
            return;
        }

        var info = JsonUtility.Deserialize<DeviceKeyInfo>(File.ReadAllText(path));

        var source = new AppKeyTokenSource(new()
        {
            TokenPublicKey = info.PublicKey,
            ApplicationId = teamID.ToByteArray(),
            TokenPrivateKey = info.PrivateKey
        });

        ApiKey = source.CreateApiKey(teamID.ToByteArray());
    }

    public void ShowSecurity()
    {
        SecurityShown = !SecurityShown;
    }

}

internal class ServiceConfigurationViewModel : ChildViewModelBase<ServiceViewModel>
{
    ExtensionViewModel extension;
    readonly  ServiceConfiguration defaultValues;
    string extentionName;
   
    public bool IsExtension { get; set; }
  
    public string ServiceName { get; private set; }

    public string ServiceNameFull { get; }

    public ServiceConfiguration CurrentServiceConfiguration
    {
        get { return ParentViewModel.ParentViewModel.ServiceConfiguration.Services.FirstOrDefault(x => x.ServiceName == this.ServiceName); }
    }

    public bool LocalOnly
    {
        get
        {
            if (CurrentServiceConfiguration != null)
                return CurrentServiceConfiguration.TcpListenAddress == "localhost";
            else
                return true;
        }
        set
        {
            if (CurrentServiceConfiguration != null)
            {
                if (value)
                    CurrentServiceConfiguration.TcpListenAddress = "localhost";
                else
                    CurrentServiceConfiguration.TcpListenAddress = "*";
            }
            OnPropertyChanged();
        }
    }

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
                    DataChannel = defaultValues.DataChannel,
                    IsExtension = defaultValues.IsExtension,
                    Behaviors = defaultValues.Behaviors,
                    SocketType = defaultValues.SocketType,
                };

                ParentViewModel.ParentViewModel.ServiceConfiguration.Services.Add(item);
            }
            else
            {
                ParentViewModel.ParentViewModel.ServiceConfiguration.Services.Remove(CurrentServiceConfiguration);
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(ServicePort)); 
        }
    }

    public bool UseSecurity
    {
        get
        {
            if (CurrentServiceConfiguration != null)
                return CurrentServiceConfiguration.Behaviors.HasFlag(EndPointBehaviors.SecurityEnabled);
            else
                return false;
        }
        set
        {
            if (value)
                CurrentServiceConfiguration.Behaviors |= EndPointBehaviors.SecurityEnabled;
            else
                CurrentServiceConfiguration.Behaviors &= ~EndPointBehaviors.SecurityEnabled;

            OnPropertyChanged();
        }
    }

    public bool ServiceAutoStart
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
                CurrentServiceConfiguration.Behaviors |= EndPointBehaviors.AutoStart;
            else
                CurrentServiceConfiguration.Behaviors &= ~EndPointBehaviors.AutoStart;

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

        OnPropertyChanged(nameof(ServicePort));
    }

    public void IncludeChanged()
    {
        IsIncluded = !IsIncluded;
        OnPropertyChanged(nameof(IsIncluded));
    }

    public ServiceConfigurationViewModel(ServiceViewModel model, ServiceConfiguration config, ExtensionViewModel extension = null)
        : base(model)
    {
        this.defaultValues = config;
        if (extension != null)
        {
            this.extension = extension;
            this.IsExtension = true;
            this.extentionName = extension.ExtensionConfiguration.ExtensionName;
        }
        this.ServiceName = $"{config.ServiceName}";
        this.ServiceNameFull = $"{config.ServiceName} ({(config.IsExtension ? $"from {extentionName}" : "Core Module")})";
    }


 
}
