using Ahsoka.Installer;
using Ahsoka.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Ahsoka.CustomerTools;

internal class CppViewModel : ViewModelBase
{
    private readonly MainViewModel parentViewModel;
    private readonly PackageInformation packageInfo;

    public async void NeedsUpdate()
    {
        if (parentViewModel.SelectedPackage.IsOpenView && packageInfo.ApplicationType == ApplicationType.Cpp)
        {
            var frameworkInfo = GetFrameworkInfo();
            if (frameworkInfo?.FrameworkVersion != parentViewModel.GetVersion())
            {
                var continueWork = await parentViewModel.Dialog.ShowDialog("SDK Mismatch", "The installed 'SDK Package'  for this project does not match this toolkit, do you wish to update?", "Yes", "No");
                if (continueWork)
                    InstallSDK();
            }

        }
    }

    public string ProjectPath
    {
        get { return packageInfo.CppInfo.ProjectPath; }
        set
        {
            packageInfo.CppInfo.ProjectPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(InstalledFramework));
        }
    }

    public string FrameworkButtonText
    {
        get
        {
            var frameworkInfo = GetFrameworkInfo();

            if (frameworkInfo == null)
                return "Add SDK Package";
            else
                return "Add SDK Package";
        }
        set
        {
            OnPropertyChanged();
        }
    }

    public string InstalledFramework
    {
        get
        {
            var frameworkInfo = GetFrameworkInfo();

            if (frameworkInfo != null)
                return $"Installed SDK: {frameworkInfo.FrameworkVersion}";

            return "No SDK Installed: To use OpenPV Services in your application, you must install the OpenPV C++ SDK into the project directory (and include it in your output) by using the button to the right.";
        }
        set
        {
            OnPropertyChanged();
        }
    }

    public string CurrentFramework
    {
        get
        {
            return $"Currently Available: {parentViewModel.GetVersion()}";
        }
        set
        {
            OnPropertyChanged();
        }
    }

    private CppFrameworkInfo GetFrameworkInfo()
    {
        string pathToInfoFile = GetFrameworkInfoPath();
        if (File.Exists(pathToInfoFile))
        {
            return JsonUtility.Deserialize<CppFrameworkInfo>(File.ReadAllText(pathToInfoFile));
        }

        return null;
    }

    private string GetFrameworkInfoPath()
    {
        return Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), "Ahsoka.Client.Shared//AhsokaFrameworkInfo.json");
    }

    public CppViewModel(MainViewModel parentViewModel, PackageInformation packageInfo)
    {
        this.parentViewModel = parentViewModel;
        this.packageInfo = packageInfo;

        packageInfo.CppInfo ??= new CppInformation();
    }

    internal async void SetProjectDirectory()
    {
        string currentPath = Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), ProjectPath);
        var path = await parentViewModel.FolderLocatorAsync("Pick Project Directory", currentPath);
        if (path != null)
            ProjectPath = Path.GetRelativePath(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), path);
    }

    internal async void InstallSDK()
    {
        parentViewModel.StartProcess(null);

        string result = "Process Ended";
        try
        {
            string projectPath = Path.GetDirectoryName(packageInfo.GetPackageInfoPath());
            if (!Directory.Exists(Path.Combine(projectPath, ProjectPath)))
            {
                parentViewModel.Console.AddConsoleLine("Please Select a Project Path.");
                return;
            }

            string pathToFramework = GetFrameworkInfoPath();
            string directoryForFramework = Path.GetDirectoryName(pathToFramework);

            // Clear and Begin Work
            parentViewModel.Console.ClearConsole();
            parentViewModel.Console.AddConsoleLine($"Beginning SDK Install to Project Path {directoryForFramework}");

            if (Directory.Exists(directoryForFramework))
                Directory.Delete(directoryForFramework, true);

            Directory.CreateDirectory(directoryForFramework);

            var progressString = new Progress<string>(info =>
            {
                parentViewModel.Console.AddConsoleLine(info);
            });

            //needs method for determining platforms to load
            System.PlatformFamily[] platformsToSupport = { System.PlatformFamily.OpenViewLinux, System.PlatformFamily.Ubuntu64 };
            foreach (var platform in platformsToSupport)
            {
                string pathToCommandLine = Path.Combine(parentViewModel.GetPlatformPath(platform), "CommandLine");
                string pathToSdk = Path.Combine(parentViewModel.GetPlatformPath(platform), "SDK");

                // Copy Command Line
                if (!Directory.Exists(pathToCommandLine) || !Directory.Exists(pathToSdk))
                {
                    parentViewModel.Console.AddConsoleLine($"Could not find target support for {platform}.\n" +
                        $"Please download the necessary files from the PlugIns tab.");
                    return;
                }

                result = await Task.Run(() =>
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    // Copy Command Line
                    string pathToCLIOutput = Path.Combine(directoryForFramework, platform.ToString(), "CommandLine");
                    parentViewModel.Console.AddConsoleLine($"Installing Ahsoka Services Application to {pathToCLIOutput}");
                    FileUtilities.CopyDirectory(pathToCommandLine, pathToCLIOutput, true, progressString);

                    // Copy SDK
                    string pathToSDKOutput = Path.Combine(directoryForFramework, platform.ToString(), "SDK");
                    parentViewModel.Console.AddConsoleLine($"Installing Ahsoka Services Application to {pathToSDKOutput}");
                    FileUtilities.CopyDirectory(pathToSdk, pathToSDKOutput, true, progressString);

                    stopwatch.Stop();

                    return "SDK Install Complete";

                });
            }


            string fileInfo = JsonUtility.Serialize(new CppFrameworkInfo() { FrameworkVersion = parentViewModel.GetVersion() });
            File.WriteAllText(GetFrameworkInfoPath(), fileInfo);
            OnPropertyChanged(nameof(InstalledFramework));
            OnPropertyChanged(nameof(FrameworkButtonText));
        }
        finally
        {
            parentViewModel.FinishProcess(result, true);
        }
    }

    internal void DeleteSDK()
    {
        string pathToFramework = GetFrameworkInfoPath();
        string directoryForFramework = Path.GetDirectoryName(pathToFramework);
        Directory.Delete(directoryForFramework, true);
        OnPropertyChanged(nameof(InstalledFramework));
        OnPropertyChanged(nameof(FrameworkButtonText));
    }
}

public class CppFrameworkInfo
{
    public string FrameworkVersion { get; set; }
}