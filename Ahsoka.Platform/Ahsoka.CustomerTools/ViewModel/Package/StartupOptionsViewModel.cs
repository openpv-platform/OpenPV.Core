using Ahsoka.Installer;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Ahsoka.CustomerTools;

internal class StartupOptionsViewModel : ChildViewModelBase<PackageViewModel>
{
    StartupEnvironmentValue environmentValue;

    public StartOptions[] StartupOptions { get; init; } = Enum.GetValues<StartOptions>();

    public StartupEnvironmentValue SelectedVariable
    {
        get { return environmentValue; }
        set { environmentValue = value; OnPropertyChanged(); }
    }

    public ObservableCollection<StartupEnvironmentValue> EnvironmentValues
    {
        get;
        init;
    } = new();


    public int[] StartupDelays { get; init; } = { 3, 5, 10 };

    public int StartDelay
    {
        get
        {
            var retVal = ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.StartupDelay;
            if (StartupDelays.Contains(retVal))
                return retVal;
            else
                return StartupDelays.First();
        }
        set
        {
            ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.StartupDelay = value;
            OnPropertyChanged();
        }
    }

    public StartOptions StartupOption 
    {
        get { return ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.ServiceStartup; }
        set
        {
            ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.ServiceStartup = value;
            OnPropertyChanged();
        }
    }

    public StartOptions ReleaseOption
    {
        get { return ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.ReleaseOption; }
        set
        {
            ParentViewModel.PackageInfo.ServiceInfo.RuntimeConfiguration.ReleaseOption = value;
            OnPropertyChanged();
        }
    }

    public StartupOptionsViewModel(PackageViewModel parentViewModel)
        : base(parentViewModel)
    {
        foreach (var item in ParentViewModel.PackageInfo.ServiceInfo.EnvironmentValues)
            EnvironmentValues.Add(item);
    }

    public void AddEnvItem()
    {
        StartupEnvironmentValue variable = new()
        {
            VariableName = "New Variable",
            VariableValue = "Value"
        };

        ParentViewModel.PackageInfo.ServiceInfo.EnvironmentValues.Add(variable);
        EnvironmentValues.Add(variable);
    }

    public void RemoveEnvItem()
    {
        if (SelectedVariable != null)
        {
            ParentViewModel.PackageInfo.ServiceInfo.EnvironmentValues.Remove(SelectedVariable);
            EnvironmentValues.Remove(SelectedVariable);
        }
    }
}