using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.Installer;
using Ahsoka.Spark;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using EnovationCloud.Core.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class ConnectionsViewModel : ChildViewModelBase<MainViewModel>, ICanCancel, IToolViewModel
{
    #region Fields
    CancellationTokenSource tokenSource;
    ConnectionInfo editInfo;
    SparkConnectionInfo factoryPrepInfo;
    ManufacturingInfo factoryInfo;
    bool supportsOpenPVTools = false;
    bool showFactoryPrep = false;
    bool canFactoryPrep = false;
    DependenciesCheckViewModel deps;
    #endregion

    #region Properties

    public DependenciesCheckViewModel Deps
    {
        get { return deps; }
        set
        {
            deps = value;
            OnPropertyChanged();
        }
    }

    public bool SupportsOpenPVTools
    {
        get { return supportsOpenPVTools; }
        set
        {
            supportsOpenPVTools = value;
            OnPropertyChanged();
        }
    }

    public bool ShowFactoryPrep
    {
        get { return showFactoryPrep; }
        set
        {
            showFactoryPrep = value;
            OnPropertyChanged();
        }
    }

    public ManufacturingInfo FactoryInfo
    {
        get { return factoryInfo; }
        set
        {
            factoryInfo = value;
            OnPropertyChanged();
        }
    }

    public SparkConnectionInfo FactoryPrepConnectionInfo
    {
        get { return factoryPrepInfo; }
        set
        {
            factoryPrepInfo = value;
            OnPropertyChanged();
        }
    }

    public bool CanFactoryPrep
    {
        get { return canFactoryPrep; }
        set
        {
            canFactoryPrep = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ConnectionInfo> ConnectionList
    {
        get;
        set;
    } = new ObservableCollection<ConnectionInfo>();

    public List<string> PlatformDescriptions
    {
        get
        {
            return HardwareInfo.GetHardwareInfoDescriptions();
        }
    }

    public ConnectionInfo EditConfigurationInfo
    {
        get { return editInfo; }
        set
        {
            editInfo = value;
            OnPropertyChanged();

            if (value != null)
            {
                Deps.CheckDependencies(SystemInfo.CurrentPlatform, value.PlatformFamily);
                SupportsOpenPVTools = value.PlatformFamily != PlatformFamily.PowerVision;
            }
        }
    }

    public int DownloadPercent { get; set; }

    public MainViewModel MainViewModel => ParentViewModel;

    public string PercentStatus { get; set; }

    #endregion

    #region Methods
    public ConnectionsViewModel(MainViewModel customerToolsViewModel) : base(customerToolsViewModel)
    {
        Deps = new DependenciesCheckViewModel(customerToolsViewModel);
        ConnectionList.CollectionChanged += Connections_CollectionChanged;
        LoadConnectionInfo();
    }

    private void Connections_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
            foreach (ConnectionInfo connectionInfo in e.NewItems)
                connectionInfo.PropertyChanged += ConnectionInfo_PropertyChanged;
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (ConnectionInfo connectionInfo in e.OldItems)
                connectionInfo.PropertyChanged -= ConnectionInfo_PropertyChanged;

            SaveConnectionInfo();
        }
    }

    private void ConnectionInfo_PropertyChanged(object sender, global::System.ComponentModel.PropertyChangedEventArgs e)
    {
        SaveConnectionInfo();
        OnPropertyChanged(nameof(ConnectionList)); // Refresh Drop Down Item

        if (e.PropertyName == "IsFavorite")
        {
            var item = this.EditConfigurationInfo;
            ConnectionList = new ObservableCollection<ConnectionInfo>(ConnectionList.OrderByDescending(x => x.IsFavorite));
            OnPropertyChanged(nameof(ConnectionList));
            this.EditConfigurationInfo = item;
        }
    }

    private void LoadConnectionInfo()
    {
        try
        {
            if (File.Exists(ConnectionFilePath()))
            {
                var connectionInfos = JsonUtility.Deserialize<List<ConnectionInfo>>(File.ReadAllText(ConnectionFilePath()));
                foreach (var item in connectionInfos.OrderByDescending(x => x.IsFavorite))
                {
                    var newItem = new ConnectionInfo(this)
                    {
                        Description = item.Description,
                        PlatformQualifier = item.PlatformQualifier,
                        PlatformFamily = item.PlatformFamily,
                        HostName = item.HostName,
                        SystemServicePort = item.SystemServicePort,
                        PrivateKey = item.PrivateKey,
                        PrivateAppKey = item.PrivateAppKey,
                        UserName = item.UserName,
                        IsFavorite = item.IsFavorite,
                        SecurityServicePort = item.SecurityServicePort,
                        APIKey = item.APIKey,
                    };

                    ConnectionList.Add(newItem);
                }
            }
        }
        catch (Exception) { }

        if (ConnectionList.Count == 0)
        {
            ConnectionList.Add(new ConnectionInfo(this)
            {
                Description = "OpenView",
                HostName = OperatingSystem.IsMacOS() ? "192.168.9.1" : "192.168.7.1",
                PlatformFamily = PlatformFamily.OpenViewLinux,
                PlatformQualifier = "S50",
                SystemServicePort = 5001,
                SecurityServicePort = 5007,
                UserName = "root",
            });

            ConnectionList.Add(new ConnectionInfo(this)
            {
                Description = "OpenViewPro",
                HostName = "am67-sk",
                PlatformFamily = PlatformFamily.OpenViewLinuxPro,
                PlatformQualifier = "GenericArm64",
                SystemServicePort = 5001,
                SecurityServicePort = 5007,
                UserName = "root",
            });

            ConnectionList.Add(new ConnectionInfo(this)
            {
                Description = "This Machine",
                HostName = "localhost",
                SystemServicePort = 5001,
                SecurityServicePort = 5007,
                UserName = "root",
            });


            ConnectionList.Add(new ConnectionInfo(this)
            {
                Description = "PowerVision",
                HostName = "192.168.0.2",
                PlatformFamily = PlatformFamily.PowerVision,
                PlatformQualifier = "Any",
                SystemServicePort = 5001,
                SecurityServicePort = 5007,
                UserName = "root",
            });


            SaveConnectionInfo();
        }

        ParentViewModel.ActiveConnection.Connection = ConnectionList.FirstOrDefault();
    }

    private void SaveConnectionInfo()
    {
        var list = ConnectionList.ToArray();
        string data = JsonUtility.Serialize(list);
        File.WriteAllText(ConnectionFilePath(), data);
    }

    private string ConnectionFilePath()
    {
        return CustomerToolPaths.GetSettingsPath("ConnectionList.json");
    }

    internal void OpenSettings()
    {
        if (ParentViewModel.Popup.RootPopup is Views.ConnectionsView)
            ParentViewModel.Popup.DismissPopup();
        else
        {
            ParentViewModel.Popup.DismissPopup();
            ParentViewModel.Popup.ShowPopup(new Views.ConnectionsView() { DataContext = this });
        }


        CanFactoryPrep = SparkAccountClient.SharedSparkClient.IsAuthorizedForFeature("FactoryPrep");

        EditConfigurationInfo = ConnectionList.FirstOrDefault();

    }

    internal async void SecureDevice()
    {
        var continueWork = await ParentViewModel.Dialog.ShowDialog("Lock Target", "Locking the device will Disable Password Based Logins.  In addition, it will install device and application keys (if selected).  Are you sure you wish to Lock this device?", "Yes", "Cancel");

        if (!continueWork)
            return;

        ParentViewModel.Console.ClearConsole();
        ParentViewModel.Console.AddConsoleLine("Beginning Device Lock Process");

        var progress = new Progress<string>(info =>
        {
            ParentViewModel.Console.AddConsoleLine(info);
        });


        var prepItem = RemoteTargetToolFactory.GetToolsForPlatform(this.EditConfigurationInfo.PlatformFamily);

        ParentViewModel.StartProcess();

        await Task.Run(() =>
        {
            if (ParentViewModel.ActiveConnection.Connection != EditConfigurationInfo)
            {
                ParentViewModel.Console.AddConsoleLine("Selected Connection does not match Active Connection");
                throw new Exception("Incorrect Device Connection");
            }
            if (!ParentViewModel.ActiveConnection.IsConnected)
            {
                ParentViewModel.Console.AddConsoleLine("No Active Connection Found");
                throw new Exception("Incorrect Device Connection");
            }


            string keyName = string.Empty;
            KeyFileInfo keyFileInfo = null;

            if (File.Exists(this.EditConfigurationInfo.PrivateKeyFile))
            {
                // Use File Name if Local Key
                keyName = Path.GetFileName(EditConfigurationInfo.PrivateKeyFile);
                keyFileInfo = SshUtility.CreateKeyFile(keyName);
            }
            else
            {
                // Use Description for Key Name for Local Keys
                keyName = this.EditConfigurationInfo.Description;

                // Load from Machine or Create and Add Configuration
                keyFileInfo = SshUtility.CreateKeyFile(this.EditConfigurationInfo.Description);

                // Update Key File Path and Save Path.
                this.EditConfigurationInfo.PrivateKey = Path.GetFileName(keyFileInfo.PrivateKeyPath);
            }

            SaveConnectionInfo();

            // Add To the SSH Configuration
            SshUtility.AddToSshConfiguration(keyFileInfo, this.EditConfigurationInfo.Description, this.EditConfigurationInfo.HostName, this.editInfo.UserName);

            // Add Key To Display
            var result = SshUtility.AddSshPublicKey(this.EditConfigurationInfo, keyFileInfo);

            // Add Key To Display
            if (File.Exists(editInfo.PrivateAppKeyFile))
            {
                var hardwareID = Guid.Parse(ParentViewModel.ActiveConnection.ConnectedHardwareInfo.DeviceId);

                var info = HardwareInfo.GetHardwareInfo(editInfo.PlatformFamily, editInfo.PlatformQualifier);
                var targetSigningKey = info.TargetPathInfo.GetInstallerPath(InstallerPaths.ApplicationSigningKeyPath);

                result += SshUtility.AddApplicationKey(this.EditConfigurationInfo, this.editInfo.PrivateAppKeyFile, targetSigningKey, hardwareID);
            }


            return $"Keys Loaded. The target is now available to connect to using ssh with the address \"{this.EditConfigurationInfo.UserName}@{this.EditConfigurationInfo.Description}\"";

        }).ContinueWith(result =>
        {
            string status = string.Empty;
            if (result.Status != TaskStatus.RanToCompletion)
            {
                status = "Process was cancelled or an error occured";
                if (result.Exception != null)
                    status += "\r\n" + result.Exception?.Message;
            }
            else
            {
                status = result.Result;

                ParentViewModel.Dialog.ShowDialog("Lock Display Complete", status, "Ok");
            }

            ParentViewModel.FinishProcess(status, result.Status == TaskStatus.RanToCompletion);
        });
    }

    internal async void UnSecureDevice()
    {
        var continueWork = await ParentViewModel.Dialog.ShowDialog("Unlock Display", "This process will leave any keys in place but allow standard logins.  Are you sure you wish to unlock this device?", "Yes", "Cancel");

        if (!continueWork)
            return;

        ParentViewModel.Console.ClearConsole();
        ParentViewModel.Console.AddConsoleLine("Beginning Device Unlock Process");

        var progress = new Progress<string>(info =>
        {
            ParentViewModel.Console.AddConsoleLine(info);
        });

        ParentViewModel.StartProcess();

        await Task.Run(() =>
        {
            var info = HardwareInfo.GetHardwareInfo(editInfo.PlatformFamily, editInfo.PlatformQualifier);
            var targetSigningKey = info.TargetPathInfo.GetInstallerPath(InstallerPaths.ApplicationSigningKeyPath);

            var result = SshUtility.UnlockDevice(EditConfigurationInfo, targetSigningKey);

            return $"Device Unlocked.";

        }).ContinueWith(result =>
        {
            string status = string.Empty;
            if (result.Status != TaskStatus.RanToCompletion)
            {
                status = "Process was cancelled or an error occured";
                if (result.Exception != null)
                    status += "\r\n" + result.Exception?.Message;
            }
            else
                status = result.Result;

            ParentViewModel.FinishProcess(status, result.Status == TaskStatus.RanToCompletion);
        });
    }

    internal async void Prep()
    {
        bool continuePrep = false;

        // If we are connected to this profile...compare
        if (editInfo != null && ParentViewModel.ActiveConnection.Connection == editInfo
            && ParentViewModel.ActiveConnection.IsConnected)
        {
            // Check for Match to save some Time...compare connected hardware info
            bool isMatch = editInfo.PlatformFamily == ParentViewModel.ActiveConnection.ConnectedHardwareInfo.PlatformFamily &&
                            editInfo.PlatformQualifier == ParentViewModel.ActiveConnection.ConnectedHardwareInfo.PlatformQualifier;

            if (!isMatch)
            {
                await ParentViewModel.Dialog.ShowDialog("Prep Target", $"Please select the correct hardware for the connected display.  The currently connected display appears to be {ParentViewModel.ActiveConnection.ConnectedHardwareInfo.PlatformFamily} ({ParentViewModel.ActiveConnection.ConnectedHardwareInfo.PlatformQualifier})", "Cancel");
                return;
            }

        }

        // We didn't Prompt before so prompt now.
        if (!continuePrep)
        {
            continuePrep = await ParentViewModel.Dialog.ShowDialog("Prep Target", $"Are you sure you want to Prep the Selected Target as \"{editInfo.PlatformDescription}\"?", "Yes", "Cancel");

            if (!continuePrep)
                return;
        }

        ParentViewModel.Console.ClearConsole();
        ParentViewModel.Console.AddConsoleLine("Beginning Developer Prep Process");

        var progress = new Progress<CommandLineProgressInfo>(info =>
        {
            if (info.IsWriteLine)
                ParentViewModel.Console.AddConsoleLine(info.Message);
            else
                ParentViewModel.Console.AddConsoleText(info.Message);

        });

        // Reset Token Source
        tokenSource = new();

        ParentViewModel.StartProcess(this);
        await Task.Run(() =>
        {
            // Create Spark File with Current Auth.
            SparkConnectionInfo info = new()
            {
                CurrentAuthorization = SparkAccountClient.SharedSparkClient.CurrentAuthorization,
                Url = ParentViewModel.SparkLogin.SparkUrl
            };

            // Serialize to JSON Strings
            var connectionInfoJson = JsonUtility.Serialize(editInfo);
            string connectionTempFile = Path.GetTempFileName();
            File.WriteAllText(connectionTempFile, connectionInfoJson);

            var sparkInfoJson = JsonUtility.Serialize(info);
            string sparkTempFileName = Path.GetTempFileName();
            File.WriteAllText(sparkTempFileName, sparkInfoJson);

            ParentViewModel.RunCommandLineProcess($"--Prep \"{connectionTempFile}\" \"{sparkTempFileName}\"", null, progress, tokenSource.Token);

            // Clean Up after Completeing Files.
            File.Delete(sparkTempFileName);
            File.Delete(connectionTempFile);

        }).ContinueWith(result =>
        {
            bool prepFailed = ParentViewModel.Console.ConsoleOutput.Contains("Prep failed");

            string status = string.Empty;
            if (result.Status != TaskStatus.RanToCompletion)
                status = "Process was cancelled or an error occured";

            ParentViewModel.FinishProcess(status, result.Status == TaskStatus.RanToCompletion && !prepFailed && !tokenSource.IsCancellationRequested);
        });
    }

    internal async void ShowFactoryPrepData()
    {
        await Task.Run(() =>
        {
            var hardwareInfo = HardwareInfo.GetHardwareInfo(editInfo.PlatformFamily, editInfo.PlatformQualifier);

            var connection = SshUtility.CreateSshClient(editInfo);
            var result = connection.RunCommand("cat /sys/firmware/devicetree/base/serial-number"); // This Needs to change but don't want to be reliant on the command being on the unit.
            var hardwareID = result.Result.Replace("\0", "");

            return SparkAccountClient.SharedSparkClient.GetFactoryInfo(hardwareInfo.PlatformFamily.ToString(), hardwareID);

        }).ContinueWith(result =>
        {
            try
            {
                FactoryInfo = new ManufacturingInfo()
                {
                    MacAddress = result.Result.MacAddress,
                    PartNumber = result.Result.PartNumber,
                    SerialNumber = result.Result.SerialNumber,
                };

                FactoryPrepConnectionInfo = new SparkConnectionInfo()
                {
                    Url = ParentViewModel.SparkLogin.SparkUrl,
                    Login = ParentViewModel.SparkLogin.LoginName
                };

            }
            catch { }
            ShowFactoryPrep = true;
        });
    }

    internal void CancelFactoryPrep()
    {
        ShowFactoryPrep = false;
    }

    internal async void FactoryPrep()
    {
        ShowFactoryPrep = false;

        var continuePrep = await ParentViewModel.Dialog.ShowDialog("Prep Target", "Are you sure you want to Prep the Selected Target?", "Yes", "Cancel");

        if (!continuePrep)
            return;

        ParentViewModel.Console.ClearConsole();
        ParentViewModel.Console.AddConsoleLine("Beginning Developer Prep Process");

        var progress = new Progress<CommandLineProgressInfo>(info =>
        {
            if (info.IsWriteLine)
                ParentViewModel.Console.AddConsoleLine(info.Message);
            else
                ParentViewModel.Console.AddConsoleText(info.Message);
        });

        // Reset Token Source
        tokenSource = new();

        ParentViewModel.StartProcess(this);
        await Task.Run(() =>
        {
            // Serialize to JSON Strings
            var connectionInfoJson = JsonUtility.Serialize(editInfo);
            string connectionTempFile = Path.GetTempFileName();
            File.WriteAllText(connectionTempFile, connectionInfoJson);

            var sparkInfoJson = JsonUtility.Serialize(factoryPrepInfo);
            string sparkTempFileName = Path.GetTempFileName();
            File.WriteAllText(sparkTempFileName, sparkInfoJson);

            var factoryInfoJson = JsonUtility.Serialize(factoryInfo);
            string factoryTempFile = Path.GetTempFileName();
            File.WriteAllText(factoryTempFile, factoryInfoJson);

            ParentViewModel.RunCommandLineProcess($"--FactoryPrep \"{connectionTempFile}\" \"{sparkTempFileName}\" \"{factoryTempFile}\"", null, progress, tokenSource.Token);

            // Clean Up after Completeing Files.
            File.Delete(sparkTempFileName);
            File.Delete(connectionTempFile);
            File.Delete(factoryTempFile);

        }).ContinueWith(result =>
        {
            string status = string.Empty;
            if (result.Status != TaskStatus.RanToCompletion)
                status = "Process was cancelled or an error occured";

            ParentViewModel.FinishProcess(status, result.Status == TaskStatus.RanToCompletion && !tokenSource.IsCancellationRequested);
        });
    }

    internal async void InstallApplication()
    {
        ParentViewModel.Console.ClearConsole();

        if (!ParentViewModel.ActiveConnection.IsInstaller)
        {
            _ = ParentViewModel.Dialog.ShowDialog("Connection Error", "No connection available or target is running an application currently", "Ok");
            ParentViewModel.Console.AddConsoleLine("The target is currently running an application.");
            ParentViewModel.Console.AddConsoleLine("You must exit the running application to return to the installer.");
            ParentViewModel.Console.AddConsoleLine(" Hint: Hit the 'Stop Application' Button on the bottom right.");
            return;
        }

        var ApplicationFile = await ParentViewModel.FileLocatorAsync("Pick Application File", PackageType.opvEncrypted.GetFileExtension(false), PackageType.opvEncrypted.GetFileExtension(false));

        if (ApplicationFile == null)
        {
            _ = ParentViewModel.Dialog.ShowDialog("Install Error", "No application file selected", "Ok");
            return;
        }

        ParentViewModel.Console.AddConsoleLine("Beginning Application Load to Target");

        var progressString = new Progress<FileTransmitProgressInfo>(info =>
        {
            ParentViewModel.Console.AddConsoleText(info.Message, true);
        });


        ParentViewModel.StartProcess(null);

        await Task.Run(() =>
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            var hardwareInfo = ParentViewModel.ActiveConnection.GetHardwareInformation();
            var connectedInfo = hardwareInfo.PlatformFamily;
            var packageInfo = EditConfigurationInfo.PlatformFamily;

            if (connectedInfo != packageInfo)
            {
                return $"The selected package type '{connectedInfo}' does not match the connected hardware '{packageInfo}'";
            }

            // Load Package
            using (var stream = new FileStream(ApplicationFile, FileMode.Open, FileAccess.Read))
            {
                var result = ParentViewModel.ActiveConnection.LoadApplicationAsync(stream, true, progressString);

                // Wait and Check Result
                result.GetAwaiter().GetResult();
            }

            stopwatch.Stop();

            return "Package Load Completed";

        }).ContinueWith(result =>
        {
            ParentViewModel.FinishProcess(result.Result, result.Status == TaskStatus.RanToCompletion);
        });
    }

    internal void AddItem()
    {
        var connInfo = new ConnectionInfo(this)
        {
            PlatformFamily = PlatformFamily.Windows64,
            PlatformQualifier = "Desktop",
            Description = "New Item",
            HostName = "localhost",
            SystemServicePort = 5001,
            SecurityServicePort = 5007,
            UserName = "root",
        };

        ConnectionList.Add(connInfo);
        EditConfigurationInfo = connInfo;
    }

    internal async void RemoveItem()
    {
        var continueRemove = await ParentViewModel.Dialog.ShowDialog("Remove?", "Are you sure you want remove the selected Display Configuration?", "Yes", "Cancel");
        if (!continueRemove)
            return;

        ConnectionList.Remove(EditConfigurationInfo);
        EditConfigurationInfo = ConnectionList.First();
    }

    internal void ConfigurationSelected()
    {
        OnPropertyChanged(nameof(EditConfigurationInfo));
    }

    internal void ClosePopup()
    {
        ParentViewModel.Popup.DismissPopup();
    }

    public void Cancel()
    {
        tokenSource.Cancel();
    }

    internal async void FlashBSP()
    {
        tokenSource = new CancellationTokenSource();

        ParentViewModel.Console.ClearConsole();
        ParentViewModel.Console.AddConsoleLine("Beginning DFU Flash Process");

        var progress = new Progress<CommandLineProgressInfo>(info =>
        {
            if (info.IsWriteLine)
                ParentViewModel.Console.AddConsoleLine(info.Message);
            else
                ParentViewModel.Console.AddConsoleText(info.Message);
        });

        // Reset Token Source
        tokenSource = new();

        ParentViewModel.StartProcess(this);
        await Task.Run(() =>
        {

            if (!ParentViewModel.Plugins.ToolListDownloaded)
            {
                ParentViewModel.Console.AddConsoleLine("Downloading Tool List.");
                ParentViewModel.Plugins.LoadItems(true);

            }

            // Check Pre-Requisites
            var family = ParentViewModel.Connections.EditConfigurationInfo.PlatformFamily;
            PlatformSupportTool firmwareTool = (PlatformSupportTool)ParentViewModel.Plugins.SupportInfo?.Downloads.FirstOrDefault(x => x is PlatformSupportTool tool && tool.PlatformFamily == family);

            if (firmwareTool == null || firmwareTool.GetPlatformFirmwareTool() == null)
            {
                ParentViewModel.Console.AddConsoleLine($"A Distribution Installer is not available for Target Platform {family}");
                return;
            }

            if (firmwareTool != null)
            {
                if (!firmwareTool.IsDownloaded)
                {
                    ParentViewModel.Console.AddConsoleLine($"Downloading Firmware Package - {firmwareTool.Name}");
                    var downloadTask = firmwareTool.DownloadTool(this, tokenSource);
                    downloadTask.Wait();
                }

                var installTask = firmwareTool.GetPlatformFirmwareTool().InstallTool(this);
                installTask.Wait();
            }

        }).ContinueWith(result =>
        {
            string status = string.Empty;
            if (result.Status != TaskStatus.RanToCompletion)
                status = "Process was cancelled or an error occured";

            ParentViewModel.FinishProcess(status, result.Status == TaskStatus.RanToCompletion);
        });
    }

    internal async void EnableRDP()
    {
        var continueEnableRDP = await ParentViewModel.Dialog.ShowDialog("Enable Remote Desktop", "This process will enable the RDP Service on the Target.   Once enabled you can connect with a standard Remote Desktop Client. Are you sure you want to Enable Remote Desktop on this Target?", "Yes", "Cancel");

        if (!continueEnableRDP)
            return;

        ParentViewModel.Console.ClearConsole();
        ParentViewModel.Console.AddConsoleLine("Enabling Remote Desktop...");

        var progress = new Progress<string>(info =>
        {
            ParentViewModel.Console.AddConsoleLine(info);
        });

        // Reset Token Source
        tokenSource = new();

        ParentViewModel.StartProcess(this);
        await Task.Run(() =>
        {
            var prepItem = RemoteTargetToolFactory.GetToolsForPlatform(this.EditConfigurationInfo.PlatformFamily);
            var info = HardwareInfo.GetHardwareInfo(this.EditConfigurationInfo.PlatformFamily, this.EditConfigurationInfo.PlatformQualifier);
            prepItem.EnableRDP(info, this.EditConfigurationInfo, IRemoteTargetTool.DefaultRdpPort, progress);

        }).ContinueWith(result =>
        {
            string status = string.Empty;
            if (result.Status != TaskStatus.RanToCompletion)
                status = "Process was cancelled or an error occured";

            ParentViewModel.FinishProcess(status, result.Status == TaskStatus.RanToCompletion && !tokenSource.IsCancellationRequested);
        });
    }

    internal async void StartSession()
    {
        var result = await ParentViewModel.Dialog.ShowDialog("Start RDP Session", "This process will start a secure tunnel to the device and open a remote desktop session. Note that the first time you use this feature, you may be prompted to allow this action.   OK?", "Yes", "No");
        if (!result)
            return;

        var task = ParentViewModel.Dialog.ShowDialog("Session Started", "Session Should begin in a moment.  Click Finish Session or Close Remote Desktop to end the session?", "Finish Session");

        await Task.Run(() =>
        {
            var prepItem = RemoteTargetToolFactory.GetToolsForPlatform(this.EditConfigurationInfo.PlatformFamily);
            prepItem.RunRDPSession(this.EditConfigurationInfo, IRemoteTargetTool.DefaultRdpPort, task);

        });
    }



    internal async void DisableRDP()
    {
        var continueEnableRDP = await ParentViewModel.Dialog.ShowDialog("Disable Remote Desktop", "Are you sure you want to Disable Remote Desktop on this Target?", "Yes", "Cancel");

        if (!continueEnableRDP)
            return;

        ParentViewModel.Console.ClearConsole();
        ParentViewModel.Console.AddConsoleLine("Disable Remote Desktop...");

        var progress = new Progress<string>(info =>
        {
            ParentViewModel.Console.AddConsoleLine(info);
        });

        // Reset Token Source
        tokenSource = new();

        ParentViewModel.StartProcess(this);
        await Task.Run(() =>
        {
            var prepItem = RemoteTargetToolFactory.GetToolsForPlatform(this.EditConfigurationInfo.PlatformFamily);
            var info = HardwareInfo.GetHardwareInfo(this.EditConfigurationInfo.PlatformFamily, this.EditConfigurationInfo.PlatformQualifier);
            prepItem.DisableRDP(info, this.EditConfigurationInfo, progress);

        }).ContinueWith(result =>
        {
            string status = string.Empty;
            if (result.Status != TaskStatus.RanToCompletion)
                status = "Process was cancelled or an error occured";

            ParentViewModel.FinishProcess(status, result.Status == TaskStatus.RanToCompletion && !tokenSource.IsCancellationRequested);
        });
    }


    #endregion
}

#region ConnectionInfo
internal class ConnectionInfo : ChildViewModelBase<ConnectionsViewModel>, ITargetConnectionInfo
{
    string description;
    string qualifier;
    string hostName;
    int port;
    int securityPort;
    bool isFavorite;
    string userName;
    string privateKey;
    string privateAppKey;
    string apiKey;
    PlatformFamily family;

    public bool IsFavorite
    {
        get { return isFavorite; }
        set
        {
            isFavorite = value;
            OnPropertyChanged();
        }
    }

    public string PlatformDescription
    {
        get
        {
            var hardwareInfo = HardwareInfo.GetHardwareInfo(PlatformFamily, PlatformQualifier);

            return hardwareInfo?.PlatformDescription;
        }
        set
        {
            if (value == null)
                return;

            var hardwareInfo = HardwareInfo.GetHardwareInfoByDescription(value);
            if (hardwareInfo != null)
            {
                PlatformFamily = hardwareInfo.PlatformFamily;
                PlatformQualifier = hardwareInfo.PlatformQualifier;
            }
            OnPropertyChanged();
        }
    }

    public List<String> SshKeys
    {
        get
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
            List<string> returnList = new List<string>() { "(None)" };

            if (Path.Exists(folder))
                returnList.AddRange(Directory.GetFiles(folder, "*.pub", SearchOption.AllDirectories).Select(Path.GetFileNameWithoutExtension));
            return returnList;
        }
        set { }
    }

    public List<String> SigningKeys
    {
        get
        {
            string path = Path.GetDirectoryName(PackageInformation.GetPackageCertPath("Test", KeyTypes.PackageSigningKey));
            List<string> returnList = new List<string>() { "(None)" };

            if (Path.Exists(path))
                returnList.AddRange(Directory.GetFiles(path, "*.appcert").Select(Path.GetFileNameWithoutExtension));

            return returnList;
        }
        set { }
    }


    public PlatformFamily PlatformFamily
    {
        get { return family; }
        set
        {
            if (value != PlatformFamily.Any)
            {
                family = value;
            }
        }
    }

    public string Description
    {
        get { return description; }
        set
        {
            description = value;
            OnPropertyChanged();
        }
    }

    public string PlatformQualifier
    {
        get { return qualifier; }
        set
        {
            qualifier = value;
        }
    }

    public string HostName
    {
        get { return hostName; }
        set
        {
            hostName = value;
            OnPropertyChanged();
        }
    }

    public int Port
    {
        get;
        set;
    }

    public int SystemServicePort
    {
        get { return port; }
        set
        {
            port = value;
            OnPropertyChanged();
        }
    }


    public int SecurityServicePort
    {
        get { return securityPort; }
        set
        {
            securityPort = value;
            OnPropertyChanged();
        }
    }

    public string UserName
    {
        get { return userName; }
        set
        {
            userName = value;
            OnPropertyChanged();
        }
    }

    public string PrivateKeyFile
    {
        get
        {
            string pathToKeys = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
            string pathToKey = Path.Combine(pathToKeys, PrivateKey);
            if (File.Exists(pathToKey))
                return pathToKey;
            else
            {
                pathToKey = Path.Combine(pathToKeys, "Ahsoka", PrivateKey);
                if (File.Exists(pathToKey))
                    return pathToKey;
            }
            return string.Empty;
        }

    }

    public string PrivateKey
    {
        get
        {
            if (SshKeys.Contains(privateKey))
                return privateKey;
            else
                return "(None)"; ;
        }
        set
        {
            privateKey = value;
            OnPropertyChanged();
        }
    }

    public string PrivateAppKeyFile
    {
        get
        {
            string path = PackageInformation.GetPackageCertPath(PrivateAppKey, KeyTypes.PackageSigningKey);
            if (File.Exists(path))
                return path;

            return string.Empty;
        }

    }

    public string PrivateAppKey
    {
        get
        {
            if (SigningKeys.Contains(privateAppKey))
                return privateAppKey;
            else
                return "(None)";
        }
        set
        {
            privateAppKey = value;
            OnPropertyChanged();
        }
    }


    public string APIKey
    {
        get
        {
            return apiKey;
        }
        set
        {
            apiKey = value;
            OnPropertyChanged();
        }
    }

    public string ConnectionAddress
    {
        get
        {
            return $"tcp://{HostName}:{SystemServicePort}";
        }
    }

    public override string ToString()
    {
        return Description.ToString();
    }

    internal string GetReleaseName()
    {
        return family.ToString();
    }

    public void FavoriteChanged()
    {
        this.IsFavorite = !IsFavorite;

    }

    public ConnectionInfo(ConnectionsViewModel parentViewModel) : base(parentViewModel)
    {

    }
}
#endregion
