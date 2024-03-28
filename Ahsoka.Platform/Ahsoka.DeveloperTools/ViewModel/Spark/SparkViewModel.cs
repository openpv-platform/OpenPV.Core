using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.Services.System;
using Ahsoka.Spark;
using EnovationCloud.Core.DataTransferObjects;
using Ahsoka.Utility;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using EnovationCloud.Core.Client;

namespace Ahsoka.DeveloperTools;

internal class SparkViewModel : ChildViewModelBase<MainViewModel>
{
    ReleaseDTO uploadInfo;

    public bool HasModules
    {
        get { return AvailableModules.Count > 0; }
        set { }
    }

    public ReleaseDTO UploadInfo
    {
        get { return uploadInfo; }
        set
        {

            uploadInfo = value;
            OnPropertyChanged();
        }
    }

    public SparkProjectViewModel SelectedTeam
    {
        get
        {
            return ParentViewModel.TeamsViewModel.Teams.FirstOrDefault(x => x.TeamID == ParentViewModel.SelectedPackage.PackageInfo.ApplicationId.ToString());
        }
        set
        {
            ParentViewModel.SelectedPackage.PackageInfo.ApplicationId = value == null ? Guid.Empty : Guid.Parse(value.TeamID);
            OnPropertyChanged();

            LoadModuleList();
        }
    }

    public ObservableCollection<PackageModuleViewModel> AvailableModules
    {
        get;
        set;
    } = new ObservableCollection<PackageModuleViewModel>();

    public ObservableCollection<SparkProjectViewModel> TeamsList
    {
        get
        {
            return ParentViewModel.TeamsViewModel.Teams;
        }
        set
        {
        }
    }


    public ObservableCollection<SparkProjectViewModel> OTATeamsList
    {
        get
        {
            return new ObservableCollection<SparkProjectViewModel>(ParentViewModel.TeamsViewModel.Teams);
        }
    }

    public bool OTAEnabled
    {
        get { return OTATeamsList.Count > 0; }
    }

    public Guid ApplicationId
    {
        get { return ParentViewModel.SelectedPackage.ApplicationId; }
        set
        {
            ParentViewModel.SelectedPackage.ApplicationId = value;
            OnPropertyChanged();
        }
    }

    public SparkViewModel(MainViewModel parentViewModel) :
        base(parentViewModel)
    {
        LoadModuleList();
    }

    internal void LoadModuleList()
    {
        AvailableModules.Clear();
        if (SelectedTeam != null)
        {
            foreach (var item in SelectedTeam.Modules.ModuleList)
            {
                this.AvailableModules.Add(new PackageModuleViewModel(this, item));
            }

            OnPropertyChanged(nameof(HasModules));
        }
    }

    internal void UpdateApplicationID()
    {
        this.ApplicationId = Guid.NewGuid();
    }

    internal void Cancel()
    {
        ParentViewModel.Popup.DismissPopup();
    }

    internal async void ProvisionDisplay()
    {
        var licenseDisplay = await ParentViewModel.Dialog.ShowDialog("Aquire Licenses?", "This process will allocate the 'Required' licenses to the connected target.   If the target already has a license it will be restored.   Are you sure you wish to proceed?", "Yes","No");
        if (!licenseDisplay)
            return;

        ParentViewModel.Console.ClearConsole();

        if (ParentViewModel.SelectedPackage == null)
        {
            ParentViewModel.Console.AddConsoleLine("Please Select a Package to Provision Target");
            return;
        }

        if (ParentViewModel.SelectedPackage.PackageInfo.ApplicationId == Guid.Empty)
        {
            ParentViewModel.Console.AddConsoleLine("Please Create an Appliction ID for your Package.  This used to generate the Channel Subscription");
            return;
        }

        if (SelectedTeam == null)
        {
            ParentViewModel.Console.AddConsoleLine("Please Select a Team for this Device.  The App and Device will be added to this Team");
            return;
        }

        if (this.SelectedTeam.Model.MemberRole == MemberRole.Viewer)
        {
            await ParentViewModel.Dialog.ShowDialog("Invalid Access", "Members with the 'Viewer' role can not provision displays", "OK");
            return;
        }

        if (!ParentViewModel.ActiveConnection.IsConnected)
        {
            ParentViewModel.Console.AddConsoleLine("Target is not connected.  Please connect to provision display in Spark");
            return;
        }
        string versionString = ParentViewModel.ActiveConnection.ConnectedHardwareInfo.VersionInfoes[SystemServiceDataKeys.SDKVersion];
        
        Version.TryParse(versionString, out Version version);
        var versionCompat = new Version("2.1");
        if (version < versionCompat)
        {
            ParentViewModel.Console.AddConsoleLine("The connected target can not be licensed until it has been upgraded to SDK version 2.1 or greater.  Use Prep or Upgrade the Installer to install licenses");
            return;
        }

        var progress = new Progress<string>(info =>
        {
            ParentViewModel.Console.AddConsoleLine(info);
        });

        ParentViewModel.StartProcess();

        _ = Task.Run(() =>
        {
            var sparkInfo = new SparkConnectionInfo()
            {
                Url = this.ParentViewModel.SparkLogin.SparkUrl
            };

            // Provision the Display in the Cloud.
            return SparkCustomerTools.ProvisionDisplayInSpark(ParentViewModel.ActiveConnection.Connection,
                                sparkInfo, // Use Current Login
                                ParentViewModel.SelectedPackage, // Supports Provisioning Interface.
                                progress);

        }).ContinueWith(result =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.Status != TaskStatus.RanToCompletion)
                    ParentViewModel.Console.AddConsoleLine("Process was cancelled or an error occured");

                ParentViewModel.FinishProcess("", result?.Status == TaskStatus.RanToCompletion);
            });
        });
    }

    internal async void UploadToSpark()
    {
        if (ParentViewModel.SelectedPackage == null)
        {
            await ParentViewModel.Dialog.ShowDialog("Missing Package", "Please Select a Package to Upload", "OK");
            return;
        }

        if (this.SelectedTeam == null)
        {
            await ParentViewModel.Dialog.ShowDialog("Missing Project", "Please Select a Project in order to publish to Spark.  If no project are available, you can create one in the Projects Tool on the Main Toolbar.", "OK");
            return;
        }

        if (this.SelectedTeam.Model.MemberRole == MemberRole.Viewer)
        {
            await ParentViewModel.Dialog.ShowDialog("Invalid Access", "Members with the 'Viewer' role can not upload packages", "OK");
            return;
        }

        if (!this.SelectedTeam.Modules.ModuleList.Any(x=>x.ModuleName == "OTA Cloud Service"))
        {
            await ParentViewModel.Dialog.ShowDialog("Missing License", "The selected Project has not licensed the 'OTA Cloud Service' Module", "OK");
            return;
        }

        UploadInfo = new ReleaseDTO()
        {
            ContainerVersion = "1.0",
            ContainerType = SparkContainerType.Software,
            TrackType = SparkTrackType.Development,
            ReleaseName = ParentViewModel.SelectedPackage.PackageName,
            ChannelName = SparkCustomerTools.CreateChannelName(this.SelectedTeam.TeamName, ParentViewModel.SelectedPackage.PackageInfo.ApplicationId.ToString())
        };


        ParentViewModel.IsWorking = true;
        uploadInfo = await UpdateVersion(uploadInfo);
        ParentViewModel.IsWorking = false;


        // Handle OPV Packages
        string outputFilePath = string.Empty;
        if (ParentViewModel.SelectedPackage.PackageInfoPath.ToLower().EndsWith(PackageViewModel.opvpackageExtension.ToLower()))
        {
            // Extract Package Info to Temp File (delete when done
            using var zipFile = new FileStream(ParentViewModel.SelectedPackage.PackageInfoPath, FileMode.Open);
            using var archive = new ZipArchive(zipFile);
            var entry = archive.Entries.FirstOrDefault(x => x.Name.ToLower().EndsWith(".pv2"));
            if (entry == null)
            {
                await ParentViewModel.Dialog.ShowDialog("Missing Binary", "A PV2 file was not found in the opvPackage file.  Please verify that this file was produced by PowerVision 8.0 or greater ", "OK");
                return;
            }

            outputFilePath = Path.Combine(Path.GetTempPath(), entry.Name);
            entry.ExtractToFile(outputFilePath, true);
        }
        else
        {
            outputFilePath = ParentViewModel.SelectedPackage.PackageInfo.GetOutputFullPath();
        }

        string outputFileName = Path.GetFileName(outputFilePath);

        // Add Item to Upload
        UploadInfo.Contents = new()
        {
            new ReleaseFileDTO()
            {
                Path = outputFilePath
            }
        };

        if (!File.Exists(outputFilePath))
        {
            ParentViewModel.Console.AddConsoleLine("Please Generate the Package Before Uploading to Spark");
            return;
        }

        ParentViewModel.Popup.ShowPopup(new SparkUploadView() { DataContext = this });
    }

    internal void BeginUpload()
    {
        ParentViewModel.Console.ClearConsole();

        var progress = new Progress<string>(info =>
        {
            ParentViewModel.Console.AddConsoleLine(info);
        });

        ParentViewModel.Console.AddConsoleLine($"Preparing Upload Package for {UploadInfo.ReleaseName}-{UploadInfo.ContainerVersion}");

        ParentViewModel.StartProcess();
        ParentViewModel.Popup.DismissPopup();
        Task.Run(() =>
        {
            SparkCustomerTools.UploadToSpark(ParentViewModel.SelectedPackage.PackageInfo, UploadInfo, SelectedTeam.Model, progress);

            // Refresh Team
            SelectedTeam.LoadTeam();

        }).ContinueWith(result =>
        {
            // Cleanup if temp File.
            string uploadedFile = UploadInfo.Contents.First().Path;
            if (uploadedFile.ToLower().EndsWith("tmp"))
                File.Delete(uploadedFile);

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.Status != TaskStatus.RanToCompletion)
                    ParentViewModel.Console.AddConsoleLine("Process was cancelled or an error occured");

                ParentViewModel.FinishProcess("", result?.Status == TaskStatus.RanToCompletion);
            });
        });
    }

    internal async Task<ReleaseDTO> UpdateVersion(ReleaseDTO uploadInfo)
    {
        return await Task.Run<ReleaseDTO>(() =>
        {
            return SparkCustomerTools.UpdateVersion(SelectedTeam.Model, uploadInfo);
        });
    }

    public void UpdateOTA()
    {
        OnPropertyChanged(nameof(OTATeamsList));
        OnPropertyChanged(nameof(OTAEnabled));
    }
}


internal class PackageModuleViewModel : ChildViewModelBase<SparkViewModel>
{
    int totalQuantity;

    public string ModuleId { get; set; } 
    public string ModuleName { get; set; }

    public int TotalQuantity
    {
        get { return totalQuantity; }
        set
        {
            totalQuantity = value;
            OnPropertyChanged();
        }
    }

    public bool IsSelected
    {
        get { return ParentViewModel.ParentViewModel.SelectedPackage.Licenses.Any(x => x.LicenseId == this.ModuleId); }
        set
        {
            if (value)
                ParentViewModel.ParentViewModel.SelectedPackage.Licenses.Add(new ModuleLicense() { LicenseId = this.ModuleId, LicenseName = ModuleName });
            else
            {
                var item = ParentViewModel.ParentViewModel.SelectedPackage.Licenses.FirstOrDefault(x => x.LicenseId == this.ModuleId);
                if (item != null)
                    ParentViewModel.ParentViewModel.SelectedPackage.Licenses.Remove(item);
            }
            OnPropertyChanged();
        }
    }

    public int TotalUsed { get; set; }

    public PackageModuleViewModel(SparkViewModel teamVM, LicensedModuleViewModel moduleItem) : base(teamVM)
    {
        this.ModuleName =  moduleItem.ModuleName ?? "No Modules Listed";
        this.ModuleId = moduleItem.ModuleID;
        TotalQuantity = moduleItem.TotalQuantity;
    }



}

