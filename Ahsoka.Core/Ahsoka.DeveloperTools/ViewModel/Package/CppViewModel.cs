using Ahsoka.DeveloperTools.Core;
using Ahsoka.Installer;
using Ahsoka.Installer.Components;
using Ahsoka.Installer.InstallEngine;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class CppViewModel : ViewModelBase
{
    private readonly MainViewModel parentViewModel;
    private readonly PackageInformation packageInfo;
 
    public void UpdateSDK()
    {
        Task.Run(() => { InstallSDK(false).GetAwaiter().GetResult(); });
    }

    public async Task NeedsUpdate(bool isGenerating = false)
    {
        if (parentViewModel.SelectedPackage.IsOpenView && (packageInfo.ApplicationType is  ApplicationType.Cpp or ApplicationType.CODESYS) || SdkNeedsUpdate)
        {
            UpdateSDKUpdateState(parentViewModel);

            if (SdkNeedsUpdate)
            {
                var continueWork = await parentViewModel.Dialog.ShowDialog("SDK Update Needed", "The installed 'SDK Package' is out of date, do you wish to update?", "Yes", "No");
                if (continueWork)
                {
                    await InstallSDK(isGenerating);
                    SdkNeedsUpdate = false;
                }
            }
        }
    }

    public CPPProjectType ProjectType
    {
        get { return packageInfo.CppInfo.ProjectType; }
        set
        {
            packageInfo.CppInfo.ProjectType = value;
            OnPropertyChanged();
        }
    }

    public CPPProjectType[] ProjectTypes
    {
        get
        {
            return Enum.GetValues<CPPProjectType>().ToArray();
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

    public bool SdkNeedsUpdate { get; set; }

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
        return Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), ".platform_support//AhsokaFrameworkInfo.json");
    }

    public CppViewModel(MainViewModel parentViewModel, PackageInformation packageInfo)
    {
        this.parentViewModel = parentViewModel;
        this.packageInfo = packageInfo;

        packageInfo.CppInfo ??= new CppInformation();

        UpdateSDKUpdateState(parentViewModel);
    }

    private void UpdateSDKUpdateState(MainViewModel parentViewModel)
    {
        var cliExists = File.Exists(parentViewModel.SelectedPackage.GetCommandLinePath());
        var extensionsUpToDate = parentViewModel.SelectedPackage.ExtensionsViewModel.SelectedExtensions.Any(x => !x.IsInstalled) ||
                                 parentViewModel.SelectedPackage.ExtensionsViewModel.SelectedExtensions.Count() == 0;
        var frameworkInfo = GetFrameworkInfo();
        var frameworkNeedsUpdate = frameworkInfo?.FrameworkVersion != parentViewModel.GetVersion();

        if (!cliExists || !extensionsUpToDate || frameworkNeedsUpdate)
            SdkNeedsUpdate = true;
    }

    internal async void SetProjectDirectory()
    {
        string currentPath = Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), ProjectPath);
        var path = await parentViewModel.FolderLocatorAsync("Pick Project Directory", currentPath);
        if (path != null)
            ProjectPath = Path.GetRelativePath(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), path);
    }

    internal async Task InstallSDK(bool isGenerating = false)
    {
        if (!isGenerating)
            parentViewModel.StartProcess(null);

        string result = "SDK Not Updated";
        bool finishedInstall = false;
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

            await ExtensionsViewModel.DownloadMissingExtensions(parentViewModel, progressString);

            System.PlatformFamily[] platformsToSupport = 
            { 
                packageInfo.PlatformFamily, 
                System.PlatformFamily.Ubuntu64, 
                PlatformFamily.Windows64 
            };
            parentViewModel.SelectedPackage.Deps.CheckDependencies(SystemInfo.CurrentPlatform, platformsToSupport);

            //needs method for determining platforms to load
            foreach (var platform in platformsToSupport)
            {
                string pathToSdk = Path.Combine(parentViewModel.GetPlatformPath(platform), "SDK");

                // Copy Command Line
                if ( !Directory.Exists(pathToSdk) && platform != PlatformFamily.Windows64) // We use Windows CLI to Build on Windows
                {
                    parentViewModel.Console.AddConsoleLine($"Could not find target support for {platform}.\n" +
                        $"Please download the necessary files from the PlugIns tab.");

                    return;
                }

                parentViewModel.Console.AddConsoleLine($"");
                      
                result = await Task.Run(() =>
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    // Copy SDK to Output
                    if (platform != PlatformFamily.Windows64)
                    {
                        string pathToSDKOutput = Path.Combine(directoryForFramework, platform.ToString() + "_TargetSupport", "SDK");
                        parentViewModel.Console.AddConsoleLine($"Installing SDK to {pathToSDKOutput}");
                        FileUtilities.CopyDirectory(pathToSdk, pathToSDKOutput, true, null);

                        ClientCodeGenerator.GenerateClients(parentViewModel.SelectedPackage.PackageInfo, Path.Combine(pathToSDKOutput));

                        AddRuntimeConfig(Path.Combine(directoryForFramework, ApplicationOutputConstants.AppInfoFolder));
                    }

                    return DotNetViewModel.InstallCommandLine(directoryForFramework, platform, parentViewModel, null);
                });
            }

            parentViewModel.Console.AddConsoleLine($"Running Extension Generators and Commands");
            parentViewModel.Console.AddConsoleLine($"");

            bool sdkCompleted = await Task.Run(() =>
            {
                var progress = new Progress<CommandLineProgressInfo>(info =>
                { 
                    if (info.IsWriteLine)
                        parentViewModel.Console.AddConsoleLine(info.Message);
                    else
                        parentViewModel.Console.AddConsoleText(info.Message);
                });

                try
                {
                    DotNetViewModel.RunSdkCommands(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), parentViewModel, progress);
                    return true;
                }
                catch (Exception ex) 
                {
                    parentViewModel.Console.AddConsoleLine(ex.Message);
                    return false;
                }
            });

            parentViewModel.Console.AddConsoleLine($" ");

            string fileInfo = JsonUtility.Serialize(new CppFrameworkInfo() { FrameworkVersion = parentViewModel.GetVersion() });
            File.WriteAllText(GetFrameworkInfoPath(), fileInfo);
            OnPropertyChanged(nameof(InstalledFramework));
            OnPropertyChanged(nameof(FrameworkButtonText));

            parentViewModel.Console.AddConsoleLine($" ");

            if (sdkCompleted)
            {
                result = "SDK's Updated Successfully";
                finishedInstall = true;
            }
        }
        catch (IOException)
        {
            finishedInstall = false;
            parentViewModel.Console.AddConsoleLine($"Unable to Remove and Re-Add the SDK Folder");
        }
        finally
        {
            if (!isGenerating)
                parentViewModel.FinishProcess(result, finishedInstall);
        }
    }

    private void AddRuntimeConfig(string pathToOutputFolder)
    {
        if (!Directory.Exists(pathToOutputFolder)) { Directory.CreateDirectory(pathToOutputFolder); }

        var stream = new MemoryStream();
        ProtoBuf.Serializer.Serialize(stream, parentViewModel.SelectedPackage.PackageInfo.ServiceInfo.RuntimeConfiguration);
        var jsonData = Encoding.UTF8.GetBytes(JsonUtility.Serialize(parentViewModel.SelectedPackage.PackageInfo.ServiceInfo.RuntimeConfiguration));

        File.WriteAllBytes(Path.Combine(pathToOutputFolder, StartupInstallerPlugin.AutoStartFileName), stream.ToArray());
        File.WriteAllBytes(Path.Combine(pathToOutputFolder, StartupInstallerPlugin.AutoStartFileName + ".json"), jsonData);
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