using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Path = System.IO.Path;

namespace Ahsoka.CustomerTools;

internal class DependenciesCheckViewModel : ChildViewModelBase<MainViewModel>
{
    bool hasDependencies = false;
    bool invalidPlatform = false;
    ObservableCollection<String> dependencyText = new();

    public bool HasDependencies
    {
        get { return hasDependencies; }
        set
        {
            hasDependencies = value;
            OnPropertyChanged();
        }
    }

    public bool InvalidPlatform
    {
        get { return invalidPlatform; }
        set
        {
            invalidPlatform = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> MissingDependenciesText
    {
        get { return dependencyText; }
        set
        {
            dependencyText = value;
            OnPropertyChanged();
        }
    }

    public DependenciesCheckViewModel(MainViewModel customerToolsViewModel) : base(customerToolsViewModel)
    {

    }

    public void CheckDependencies(PlatformFamily currentPlatform, PlatformFamily platformFamily)
    {
        var thisVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

        MissingDependenciesText.Clear();

        if (currentPlatform == PlatformFamily.Any)
            MissingDependenciesText.Add($"- {currentPlatform} Is Not valid. Please check the Platform Family Drop Down.");
        if (platformFamily == PlatformFamily.Any)
            MissingDependenciesText.Add($"- {platformFamily} Is Not a valid Platform. Please check the Platform Family Drop Down.");

        if (MissingDependenciesText.Count > 0)
        {
            HasDependencies = false;
            InvalidPlatform = true;
            return;
        }

        InvalidPlatform = false;

        string clientDependencyRoot = ParentViewModel.GetPlatformPath(currentPlatform);
        string clientDependencyVersionFile = Path.Combine(clientDependencyRoot, Path.GetFileNameWithoutExtension(clientDependencyRoot) + ".zip.customertools.version");
        if (File.Exists(clientDependencyVersionFile))
        {
            string versionInfo = File.ReadAllText(clientDependencyVersionFile);
            var toolsVersion = versionInfo.Split(".");
            if (thisVersion.FileMajorPart != int.Parse(toolsVersion[0]) ||
                thisVersion.FileMinorPart != int.Parse(toolsVersion[1]))
                MissingDependenciesText.Add($"- {currentPlatform} Platform Support Package (Wrong Version).");
        }
        else
            MissingDependenciesText.Add($"- {currentPlatform} Platform Support Package.");


        if (platformFamily != PlatformFamily.PowerVision) // PowerVision Is Not Directly Supported
        {
            string targetDependencyPath = ParentViewModel.GetPlatformPath(platformFamily);
            string targetDependencyVersionFile = Path.Combine(targetDependencyPath, Path.GetFileNameWithoutExtension(targetDependencyPath) + ".zip.customertools.version");
            if (File.Exists(targetDependencyVersionFile))
            {
                string versionInfo = File.ReadAllText(targetDependencyVersionFile);
                var clientToolsVersion = versionInfo.Split(".");
                if (thisVersion.FileMajorPart != int.Parse(clientToolsVersion[0]) ||
                    thisVersion.FileMinorPart != int.Parse(clientToolsVersion[1]))
                    MissingDependenciesText.Add($"- {platformFamily} Platform Support Package (Wrong Version).");
            }
            else
                MissingDependenciesText.Add($"- {platformFamily} Platform Support Package.");
        }


        if (MissingDependenciesText.Count > 0)
        {
            HasDependencies = false;
            return;
        }

        HasDependencies = true;
    }

    internal void IgnorePluginManager()
    {
        HasDependencies = true;
    }

    internal void GoToAddIns()
    {
        ParentViewModel.Plugins.OpenPopup();
    }
}

