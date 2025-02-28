using Ahsoka.Core;
using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility;
using Ahsoka.DeveloperTools.Core;
using Ahsoka.Installer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensions = Ahsoka.Core.Extensions;

#pragma warning disable CS0436 // Type conflicts with imported type for GitVersionInformation

namespace Ahsoka.DeveloperTools;

internal class DotNetViewModel : ViewModelBase
{
    private readonly MainViewModel parentViewModel;
    private readonly PackageInformation packageInfo;

    public bool SdkNeedsUpdate
    {
        get;
        set;
    }

    public void UpdateSDK()
    {
        Task.Run(() => { InstallCLI(false).GetAwaiter().GetResult(); });
    }

    public async Task NeedsUpdate(bool isGenerating = false)
    {
        if (parentViewModel.SelectedPackage.IsOpenView && packageInfo.ApplicationType == ApplicationType.Dotnet || SdkNeedsUpdate)
        {
            UpdateSDKUpdateState(parentViewModel);
            if (SdkNeedsUpdate)
            {
                var continueWork = await parentViewModel.Dialog.ShowDialog("SDK Update Needed", "The installed 'SDK Package' is out of date, do you wish to update?", "Yes", "No");
                if (continueWork)
                {
                    await InstallCLI(isGenerating);
                    SdkNeedsUpdate = false;
                }
            }
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

    public string ProjectPath
    {
        get { return packageInfo.DotNetInfo.ProjectPath; }
        set
        {
            packageInfo.DotNetInfo.ProjectPath = value;
            OnPropertyChanged();
        }
    }

    public string PublishProfile
    {
        get { return packageInfo.DotNetInfo.PublishProfile; }
        set
        {
            packageInfo.DotNetInfo.PublishProfile = value;
            OnPropertyChanged();
        }
    }

    public string InstalledFramework
    {
        get
        {
            var frameworkInfo = GetFrameworkInfo();

            if (frameworkInfo != null)
                return $"Installed CLI: {frameworkInfo.FrameworkVersion}";

            return "No Command Line Installed: To use OpenPV Code Generation in your application, you must install the OpenPV CommandLine into the project directory by using the button to the right.";
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
        string projectPath = Path.GetDirectoryName(Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), ProjectPath));
        return Path.Combine(projectPath, $"obj/{PlatformSupportPathInfo.PlatformSupportRootFolderName_App}/AhsokaFrameworkInfo.json");
    }

    public DotNetViewModel(MainViewModel parentViewModel, PackageInformation packageInfo)
    {
        this.parentViewModel = parentViewModel;
        this.packageInfo = packageInfo;

        packageInfo.DotNetInfo ??= new DotNetInformation();

        UpdateSDKUpdateState(parentViewModel);
    }

    private void UpdateSDKUpdateState(MainViewModel parentViewModel)
    {
        var cliExists = File.Exists(parentViewModel.SelectedPackage.GetCommandLinePath());
        var extensionsUpToDate = parentViewModel.SelectedPackage.ExtensionsViewModel.SelectedExtensions.Any(x => !x.IsInstalled) ||
                                 parentViewModel.SelectedPackage.ExtensionsViewModel.SelectedExtensions.Count() == 0;
        var frameworkInfo = GetFrameworkInfo();
        var frameworkNeedsUpdate = frameworkInfo?.FrameworkVersion != parentViewModel.GetVersion() || frameworkInfo?.PlatformFamily != parentViewModel.SelectedPackage.PlatformFamily;

        if (frameworkInfo != null)
            if (new Version(frameworkInfo.FrameworkVersion).Major < new Version(parentViewModel.GetVersion()).Major)
            {
                string packageExtensionFolder = parentViewModel.SelectedPackage.PackageInfo.GetExtensionFolder();
                if (Directory.Exists(packageExtensionFolder))
                    Directory.Delete(packageExtensionFolder, true);

                foreach (var item in parentViewModel.SelectedPackage.ExtensionsViewModel.SelectedExtensions)
                    item.InstalledVersion = "0.0";
            }

        if (!cliExists || !extensionsUpToDate || frameworkNeedsUpdate)
            SdkNeedsUpdate = true;
    }

    internal async void SetProject()
    {
        string currentPath = Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), ProjectPath);
        var path = await parentViewModel.FileLocatorAsync("Pick Output Directory", "csproj", "DotNet Project", currentPath);
        if (path != null)
            ProjectPath = Path.GetRelativePath(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), path);
    }

    internal async void SetPublishProfile()
    {
        string currentPath = Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), PublishProfile);
        var path = await parentViewModel.FileLocatorAsync("Pick Publish Profile", "pubxml", "Publish Profile", currentPath);
        if (path != null)
            PublishProfile = Path.GetRelativePath(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), path);
    }

    internal async Task InstallCLI(bool isGenerating = false)
    {
        if (!isGenerating)
            parentViewModel.StartProcess(null);

        bool finishedInstall = false;
        string result = "Process Ended";
        try
        {
            string projectPath = Path.GetDirectoryName(Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), ProjectPath));
            if (!Directory.Exists(Path.Combine(projectPath)))
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

            await ExtensionsViewModel.DownloadMissingExtensions(parentViewModel, progressString);

            //needs method for determining platforms to load
            List<PlatformFamily> platformsToSupport = new() { packageInfo.PlatformFamily };

            if (OperatingSystem.IsWindows())
                platformsToSupport.Add(PlatformFamily.Windows64);
            if (OperatingSystem.IsLinux())
                platformsToSupport.Add(PlatformFamily.Ubuntu64);
            if (OperatingSystem.IsMacOS())
                platformsToSupport.Add(PlatformFamily.MacOSArm64);

            foreach (var platform in platformsToSupport)
            {
                result = await Task.Run(() =>
                {
                    return InstallCommandLine(directoryForFramework, platform, parentViewModel, progressString);
                });
            }

            await Task.Run(() =>
            {
                var progress = new Progress<CommandLineProgressInfo>(info =>
                {
                    if (info.IsWriteLine)
                        parentViewModel.Console.AddConsoleLine(info.Message);
                    else
                        parentViewModel.Console.AddConsoleText(info.Message);
                });

                RunSdkCommands(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), parentViewModel, progress);
            });

            parentViewModel.Console.AddConsoleLine($" ");

            string fileInfo = JsonUtility.Serialize(new CppFrameworkInfo() { FrameworkVersion = parentViewModel.GetVersion(), PlatformFamily = packageInfo.PlatformFamily });
            File.WriteAllText(GetFrameworkInfoPath(), fileInfo);
            OnPropertyChanged(nameof(InstalledFramework));
            OnPropertyChanged(nameof(FrameworkButtonText));

            finishedInstall = true;
        }
        catch (IOException)
        {
            finishedInstall = false;
            parentViewModel.Console.AddConsoleLine($"Unable to Remove and Re-Add the SDK Folder");
        }
        finally
        {
            if (!isGenerating)
                parentViewModel.FinishProcess("SDK Updated", finishedInstall);
        }
    }

    public static void RunSdkCommands(string projectPath, MainViewModel mainViewModel, Progress<CommandLineProgressInfo> progress)
    {
        foreach (var info in mainViewModel.SelectedPackage.ServiceConfiguration.ExtensionInfo)
        {
            var list = Extensions.GetCommands(mainViewModel.SelectedPackage.PackageInfo, info.ExtensionName, CommandTypes.SdkGenerator | CommandTypes.ModelGenerators);
            foreach (var command in list)
                if (command.Value == GeneratorCommandType.AhsokaCommandLine)
                    mainViewModel.RunCommandLineProcess(command.Key, projectPath, progress, CancellationToken.None, mainViewModel.SelectedPackage.GetCommandLinePath());
                else
                    mainViewModel.RunShellProcess(command.Key, projectPath, progress, CancellationToken.None);
        }
    }

    internal static string InstallCommandLine(string directory, PlatformFamily platform, MainViewModel mainViewModel, IProgress<string> progressString)
    {
        string pathToCommandLine = Path.Combine(mainViewModel.GetPlatformPath(platform), "CommandLine");
        string pathToHardware = Path.Combine(mainViewModel.GetPlatformPath(platform), "Hardware");
        string pathToResources = Path.Combine(mainViewModel.GetPlatformPath(platform), "Resources");

        // Copy Command Line
        if (!Directory.Exists(pathToCommandLine))
        {
            progressString?.Report($"Could not find target support for {platform}.\n" +
                $"Please download the necessary files from the PlugIns tab.");
            return "No Target Found";
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        // Copy Command Line
        string pathToCLIOutput = Path.Combine(directory, platform.ToString() + "_TargetSupport", "CommandLine");
        string pathToHWOutput = Path.Combine(directory, platform.ToString() + "_TargetSupport", "Hardware");
        string pathToResourceOutput = Path.Combine(directory, platform.ToString() + "_TargetSupport", "Resources");
        progressString?.Report($" ");
        progressString?.Report($"Installing Ahsoka Services Application to {pathToCLIOutput}");
        progressString?.Report($" ");
        FileUtilities.CopyDirectory(pathToCommandLine, pathToCLIOutput, true, progressString);

        if (Directory.Exists(pathToHardware))
            FileUtilities.CopyDirectory(pathToHardware, pathToHWOutput, true, progressString);

        if (Directory.Exists(pathToResources))
            FileUtilities.CopyDirectory(pathToResources, pathToResourceOutput, true, progressString);

        progressString?.Report($" ");

        stopwatch.Stop();

        return "Command Line Install Complete";
    }

    internal void DeleteCLI()
    {
        string pathToFramework = GetFrameworkInfoPath();
        string directoryForFramework = Path.GetDirectoryName(pathToFramework);
        Directory.Delete(directoryForFramework, true);
        OnPropertyChanged(nameof(InstalledFramework));
        OnPropertyChanged(nameof(FrameworkButtonText));
    }
}
