using Ahsoka.DeveloperTools.Core;
using Ahsoka.Installer;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Extensions = Ahsoka.System.Extensions;

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
        InstallCLI(false);
    }

    public async Task NeedsUpdate(bool isGenerating = false)
    {
        if (parentViewModel.SelectedPackage.IsOpenView && packageInfo.ApplicationType == ApplicationType.Dotnet || SdkNeedsUpdate)
        {
            var frameworkInfo = GetFrameworkInfo();
            if (frameworkInfo?.FrameworkVersion != parentViewModel.GetVersion() || SdkNeedsUpdate)
            {
                var continueWork = await parentViewModel.Dialog.ShowDialog("SDK Update Needed", "The installed 'SDK Package' is out of date, do you wish to update?", "Yes", "No");
                if (continueWork)
                {
                    InstallCLI(isGenerating);
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
        return Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), "obj/Ahsoka.Client.Shared//AhsokaFrameworkInfo.json");
    }

    public DotNetViewModel(MainViewModel parentViewModel, PackageInformation packageInfo)
    {
        this.parentViewModel = parentViewModel;
        this.packageInfo = packageInfo;

        packageInfo.DotNetInfo ??= new DotNetInformation();
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

    internal async void InstallCLI(bool isGenerating = false)
    {
        if (!isGenerating)
            parentViewModel.StartProcess(null);

        string result = "Process Ended";
        try
        {
            string projectPath = Path.GetDirectoryName(packageInfo.GetPackageInfoPath());
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

            UpdateNuget();


            //needs method for determining platforms to load
            List<System.PlatformFamily> platformsToSupport = new() { packageInfo.PlatformFamily };

            if (OperatingSystem.IsWindows())
                platformsToSupport.Add(System.PlatformFamily.Windows64);
            if (OperatingSystem.IsLinux())
                platformsToSupport.Add(System.PlatformFamily.Ubuntu64);
            if (OperatingSystem.IsMacOS())
                platformsToSupport.Add(System.PlatformFamily.MacosArm64);

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

                RunSdkCommands(projectPath, parentViewModel, progress);

            });

            parentViewModel.Console.AddConsoleLine($" ");

            string fileInfo = JsonUtility.Serialize(new CppFrameworkInfo() { FrameworkVersion = parentViewModel.GetVersion() });
            File.WriteAllText(GetFrameworkInfoPath(), fileInfo);
            OnPropertyChanged(nameof(InstalledFramework));
            OnPropertyChanged(nameof(FrameworkButtonText));

            File.WriteAllText(Path.Combine(projectPath, "Directory.Build.props"), Properties.Resources.BuildProps);
        }
        finally
        {
            if (!isGenerating)
                parentViewModel.FinishProcess("SDK Updated", true);
        }
    }

    public static void RunSdkCommands(string projectPath,MainViewModel mainViewModel, Progress<CommandLineProgressInfo> progress)
    {
        foreach (var extensionName in mainViewModel.SelectedPackage.ServiceConfiguration.Extensions)
        {
            var list = Extensions.GetCommands(mainViewModel.SelectedPackage.PackageInfo, extensionName, CommandTypes.SdkGenerator | CommandTypes.ModelGenerators);
            foreach (var command in list)
                if (command.Value == GeneratorCommandType.AhsokaCommandLine)
                    mainViewModel.RunCommandLineProcess(command.Key, projectPath, progress, CancellationToken.None);
                else
                    mainViewModel.RunShellProcess(command.Key, projectPath, progress, CancellationToken.None);
        }
    }

    internal static string InstallCommandLine(string directory, PlatformFamily platform, MainViewModel mainViewModel, IProgress<string> progressString)
    {
        string pathToCommandLine = Path.Combine(mainViewModel.GetPlatformPath(platform), "CommandLine");

        // Copy Command Line
        if (!Directory.Exists(pathToCommandLine))
        {
            progressString.Report($"Could not find target support for {platform}.\n" +
                $"Please download the necessary files from the PlugIns tab.");
            return "No Target Found";
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        // Copy Command Line
        string pathToCLIOutput = Path.Combine(directory, platform.ToString(), "CommandLine");
        progressString.Report($" ");
        progressString.Report($"Installing Ahsoka Services Application to {pathToCLIOutput}");
        progressString.Report($" ");
        FileUtilities.CopyDirectory(pathToCommandLine, pathToCLIOutput, true, progressString);

        foreach (var extensionName in mainViewModel.SelectedPackage.ServiceConfiguration.Extensions)
            Extensions.InstallExtension(pathToCLIOutput, extensionName, progressString);
     
        progressString.Report($" ");

        stopwatch.Stop();

        return "Command Line Install Complete";
    }

    private void UpdateNuget()
    {
        string currentPath = Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), ProjectPath);
        if (File.Exists(currentPath))
        {
            string projectFile = File.ReadAllText(currentPath);
            XDocument doc = XDocument.Parse(projectFile);

            // Update Nuget to Match This Tool If we are not Debugging
            if (!Debugger.IsAttached)
            {
                // If this is a released (non developer) version, update nuget to match.
                string nugetVersion = GitVersionInformation.FullSemVer;
                foreach (var item in doc.Descendants("PackageReference"))
                {
                    if (item.Attribute("Include").Value.StartsWith("Ahsoka.Core"))
                        item.Attribute("Version").Value = nugetVersion;
                }

                doc.Save(currentPath);
            }
        }

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
