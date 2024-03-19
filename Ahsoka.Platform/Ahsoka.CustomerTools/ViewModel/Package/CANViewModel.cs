using Ahsoka.Services.Can;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using SocketCANSharp.Network;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.CustomerTools;

internal class CanSetupViewModel : ViewModelBase
{
    readonly uint canIndex = 0;
    string originalCANConfigText = string.Empty;
    private readonly MainViewModel parentViewModel;
    private readonly PackageViewModel packageViewModel;
    private CanClientCalibration canClientCalibration = new();
    private ObservableCollection<NodeViewModel> nodes = new();
    private NodeViewModel selectedNode;
    private ObservableCollection<DiagnosticEventViewModel> diagnosticEvents = new();
    private DiagnosticEventViewModel selectedDiagnosticEvent;
    private ObservableCollection<MessageViewModel> messages = new();
    private MessageViewModel selectedMessage;
    private ObservableCollection<PortViewModel> ports = new();
    private PortViewModel selectedPort;
    private int selectedTabIndex = 0;
    private bool port0Enabled, port1Enabled = false;

    internal int SelectedTab { get { return selectedTabIndex; } set { selectedTabIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowAddRemove)); EvaluatePort(); } }

    public bool ShowAddRemove { get { return SelectedTab != 2; } set { } }

    public bool Port0Enabled
    {
        get { return port0Enabled; }
        set { port0Enabled = value; OnPropertyChanged(); }
    }

    public bool Port1Enabled
    {
        get { return port1Enabled; }
        set { port1Enabled = value; OnPropertyChanged(); }
    }

    public string CalibrationPath
    {
        get { return packageViewModel.PackageInfo.CanCalibration; }
        set
        {
            packageViewModel.PackageInfo.CanCalibration = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<MessageViewModel> Messages
    {
        get { return messages; }
        set
        {
            messages = value;
            OnPropertyChanged();
        }
    }

    public MessageViewModel SelectedMessage
    {
        get { return selectedMessage; }
        set
        {
            if (selectedMessage != null)
                selectedMessage.IsSelected = false;
            selectedMessage = value;
            if (selectedMessage != null)
                selectedMessage.IsSelected = true;

            OnPropertyChanged();
        }
    }

    public ObservableCollection<NodeViewModel> Nodes
    {
        get { return nodes; }
        set
        {
            nodes = value;
            OnPropertyChanged();
        }
    }

    public NodeViewModel SelectedNode
    {
        get { return selectedNode; }
        set
        {
            if (selectedNode != null)
                selectedNode.IsSelected = false;

            selectedNode = value;

            if (selectedNode != null)
                selectedNode.IsSelected = true;

            OnPropertyChanged();
        }
    }

    public ObservableCollection<PortViewModel> Ports
    {
        get { return ports; }
        set
        {
            ports = value;
            OnPropertyChanged();
        }
    }

    public PortViewModel SelectedPort
    {
        get { return selectedPort; }
        set
        {
            if (selectedPort != null)
                selectedPort.IsSelected = false;

            selectedPort = value;

            if (selectedPort != null)
                selectedPort.IsSelected = true;

            OnPropertyChanged();
        }
    }

    public ObservableCollection<DiagnosticEventViewModel> DiagnosticEvents
    {
        get { return diagnosticEvents; }
        set
        {
            diagnosticEvents = value;
            OnPropertyChanged();
        }
    }

    public DiagnosticEventViewModel SelectedDiagnosticEvent
    {
        get { return selectedDiagnosticEvent; }
        set
        {
            if (selectedDiagnosticEvent != null)
                selectedDiagnosticEvent.IsSelected = false;

            selectedDiagnosticEvent = value;

            if (selectedDiagnosticEvent != null)
                selectedDiagnosticEvent.IsSelected = true;

            OnPropertyChanged();
        }
    }

    public CanClientCalibration CanClientCalibration
    {
        get => canClientCalibration;
        set
        {
            canClientCalibration = value;
            originalCANConfigText = JsonUtility.Serialize(canClientCalibration);
        }
    }


    public CanSetupViewModel(MainViewModel parentViewModel, PackageViewModel packageViewModel, uint canIndex)
    {
        this.parentViewModel = parentViewModel;
        this.packageViewModel = packageViewModel;

        this.canIndex = canIndex;

        LoadCANConfiguration();

        EvaluatePort();
    }

    internal async void SetConfigurationDirectory()
    {
        string currentPath = "";
        if (CalibrationPath != null)
            currentPath = Path.Combine(Path.GetDirectoryName(packageViewModel.PackageInfoPath), CalibrationPath);

        var path = await parentViewModel.FileLocatorAsync("Pick CAN Calibration", "cancalibration.json", currentPath);
        if (path != null && File.Exists(path))
        {
            CalibrationPath = Path.GetRelativePath(Path.GetDirectoryName(packageViewModel.PackageInfoPath), path);
            LoadCANConfiguration();
        }
    }

    internal async void CreateFromDBC()
    {
        var pathToDBC = await parentViewModel.FileLocatorAsync("Pick DBC Calibration", "dbc", CalibrationPath);

        if (!File.Exists(pathToDBC))
            return;

        // Run Creation Process and Capture File.
        parentViewModel.StartProcess(null);

        // Clear and Begin Work
        parentViewModel.Console.ClearConsole();
        parentViewModel.Console.AddConsoleLine($"Creating CAN Configuration from DBC: {CalibrationPath}");

        var progress = new Progress<CommandLineProgressInfo>(info =>
        {
            if (info.IsWriteLine)
                parentViewModel.Console.AddConsoleLine(info.Message);
            else
                parentViewModel.Console.AddConsoleText(info.Message);
        });

        await Task.Run(() =>
        {
            string workingDirectory = Path.GetDirectoryName(packageViewModel.PackageInfoPath);
            string pathToOutputFile = Path.Combine(workingDirectory, CreateCalibationName());

            parentViewModel.RunCommandLineProcess($" --GenerateCalibrationFromDBC \"{pathToDBC}\" \"{pathToOutputFile}\"", workingDirectory, progress, CancellationToken.None);

            if (File.Exists(pathToOutputFile))
                return pathToOutputFile;
            else
                return string.Empty; // fileName;

        }).ContinueWith(result =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.Status == TaskStatus.Faulted)
                {
                    parentViewModel.Console.AddConsoleLine(result.Exception.Message);
                    parentViewModel.Dialog.ShowDialog("Creation Error", $"{result.Exception?.InnerException?.Message}", "OK");
                }
                else if (result.Status == TaskStatus.RanToCompletion && File.Exists(result.Result))
                {
                    CalibrationPath = Path.GetRelativePath(Path.GetDirectoryName(packageViewModel.PackageInfoPath), result.Result);
                    parentViewModel.Console.AddConsoleLine($"Create Completed at {CalibrationPath}");
                    LoadCANConfiguration();
                }

                parentViewModel.FinishProcess("", result.Status == TaskStatus.RanToCompletion && File.Exists(result.Result));
            });
        });
    }

    private string CreateCalibationName()
    {
        return $"{packageViewModel.PackageName}.{canIndex}.cancalibration.json";
    }

    internal void RemoveConfiguration()
    {
        CalibrationPath = null;
        CanClientCalibration = new CanClientCalibration();
        Nodes.Clear();
        Messages.Clear();

        OnPropertyChanged(nameof(ConfigurationPath));
        OnPropertyChanged(nameof(Nodes));
        OnPropertyChanged(nameof(Messages));
    }

    private void LoadCANConfiguration()
    {
        if (CalibrationPath != null)
        {
            string pathToConfig = Path.Combine(Path.GetDirectoryName(packageViewModel.PackageInfoPath), CalibrationPath);
            if (File.Exists(pathToConfig))
            {
                try
                {
                    CanClientCalibration = ConfigurationFileLoader.LoadFile<CanClientCalibration>(pathToConfig);                   

                    Ports.Clear();
                    foreach (PortDefinition port in CanClientCalibration.Ports.OrderBy(x => x.Port))
                        Ports.Add(new PortViewModel(parentViewModel, this, port) { IsEnabled = true }) ;

                    foreach (CanPort port in HardwareInfo.GetHardwareInfo(packageViewModel.PackageInfo.PlatformFamily, packageViewModel.PackageInfo.PlatformQualifier).CANInfo.CANPorts)
                        if (!Ports.Any(x => x.Port == port.Port))
                            Ports.Add(new PortViewModel(parentViewModel, this, new PortDefinition()
                            {
                                Port = port.Port,
                                CanInterfacePath = port.SocketCanInterfacePath,
                                BaudRate = CanBaudRate.Baud250kb,
                                CanInterface = CanInterface.SocketCan,
                                PromiscuousTransmit = false,
                                PromiscuousReceive = false,
                                UserDefined = true,                    
                            }) { IsEnabled = false });

                    Nodes.Clear();
                    foreach (NodeDefinition node in CanClientCalibration.Nodes.OrderBy(x => x.Id))
                        Nodes.Add(new NodeViewModel(parentViewModel, this, node));

                    Messages.Clear();
                    foreach (MessageDefinition msg in CanClientCalibration.Messages.OrderBy(x => x.Id))
                        Messages.Add(new MessageViewModel(parentViewModel, this, msg));

                    DiagnosticEvents.Clear();
                    foreach (DiagnosticEventDefinition eventDef in CanClientCalibration.DiagnosticEvents)
                        DiagnosticEvents.Add(new DiagnosticEventViewModel(parentViewModel, this, eventDef));

                    OnPropertyChanged(nameof(CalibrationPath));
                    OnPropertyChanged(nameof(CanInterface));
                }
                catch
                {
                    CanClientCalibration = new CanClientCalibration(); // Create Default 
                }
            }
            else
                CanClientCalibration = new CanClientCalibration(); // Create Default 
        }

        // Bump Calibration file to Current.
        CanClientCalibration.Version = VersionUtility.GetAppVersionString();

    }

    internal void AddItem()
    {
        if (SelectedTab == 1)
        {
            var nodeViewModel = new NodeViewModel(parentViewModel, this, null);
            this.Nodes.Add(nodeViewModel);
        }
        else if (SelectedTab == 0)
        {
            var nodeViewModel = new MessageViewModel(parentViewModel, this, null);
            nodeViewModel.MessageDefinition.UserDefined = true;
            this.Messages.Add(nodeViewModel);
           
            EvaluatePort();
        }
        else if (SelectedTab == 3)
        {
            var vm = new DiagnosticEventViewModel(parentViewModel, this, null);
            this.DiagnosticEvents.Add(vm);
        }
    }

    private void EvaluatePort()
    {
        Port0Enabled = this.Ports.Any(x => x.IsEnabled && x.Port == 0);
        Port1Enabled = this.Ports.Any(x => x.IsEnabled && x.Port == 1);
    }

    internal async void RemoveItem()
    {
        if (SelectedTab == 1)
        {
            var continueWork = await parentViewModel.Dialog.ShowDialog("Remove Node", "Are you sure you wish to remove the Selected Node?", "Yes", "Cancel");

            if (!continueWork)
                return;

            if (selectedNode != null)
            {
                this.CanClientCalibration.Nodes.Remove(selectedNode.NodeDefinition);
                this.Nodes.Remove(selectedNode);
                this.selectedNode = this.nodes.FirstOrDefault();
            }
        }
        else if (SelectedTab == 0)
        {
            var continueWork = await parentViewModel.Dialog.ShowDialog("Remove Message", "Are you sure you wish to remove the Selected Message?", "Yes", "Cancel");
            if (!continueWork)
                return;

            this.CanClientCalibration.Messages.Remove(SelectedMessage.MessageDefinition);
            this.Messages.Remove(SelectedMessage);
            this.SelectedMessage = this.messages.FirstOrDefault();
        }
        else if (SelectedTab == 3)
        {
            var continueWork = await parentViewModel.Dialog.ShowDialog("Remove Diagnostic Event", "Are you sure you wish to remove the Selected Event?", "Yes", "Cancel");
            if (!continueWork)
                return;

            this.CanClientCalibration.DiagnosticEvents.Remove(SelectedDiagnosticEvent.EventDefinition);
            this.DiagnosticEvents.Remove(SelectedDiagnosticEvent);
            this.selectedDiagnosticEvent = this.DiagnosticEvents.FirstOrDefault();
        }
    }

    internal void Save(string packageInfoPath)
    {
        /// Generate FileName if We Edited the Data but have no path.
        if (String.IsNullOrEmpty(CalibrationPath))
            if (nodes.Count > 0 || messages.Count > 0 || ports.Count > 0)
                CalibrationPath = CreateCalibationName();

        ValidateConfiguration();

        if (!String.IsNullOrEmpty(CalibrationPath))
        {
            string fileName = Path.Combine(packageInfoPath, CalibrationPath);
            string data = JsonUtility.Serialize(CanClientCalibration);
            File.WriteAllText(fileName, data);
        }
    }

    private void ValidateConfiguration()
    {
        canClientCalibration.Ports.Clear();
        foreach(var port in ports)
            if (port.IsEnabled)
                canClientCalibration.Ports.Add(port.PortDefinition);

        foreach (var message in Messages)
        {
            uint maxBits = message.MessageDefinition.Signals.Max(x => x.StartBit + x.BitLength);
            if (maxBits > 0)
            {
                uint dlc = (uint)Math.Ceiling(maxBits / 8.0);
                if (message.MessageDefinition.Dlc != dlc)
                    message.MessageDefinition.Dlc = dlc;
            }
        }
    }

    internal void SetStandardNode(bool include, string name)
    {
        var standardDefinitions = SystemInfo.StandardCanMessages;
        var standardNode = standardDefinitions.Nodes.First(x => x.Name == name);
        var nodeViewModel = new NodeViewModel(parentViewModel, this, standardNode);

        var node = Nodes.FirstOrDefault(x => x.Name == name);
        var inList = node != null;
        if (include && !inList)
        {
            Nodes.Add(nodeViewModel);
            CanClientCalibration.Nodes.Add(nodeViewModel.NodeDefinition);
        }
        else if (!include && inList)
        {
            Nodes.Remove(node);
            CanClientCalibration.Nodes.Remove(node.NodeDefinition);
        }
    }

    internal void UpdateAddressClaim()
    {
        var node = Nodes.FirstOrDefault(x => x.IsSelf);
        if (node != null)
        {
            node.NodeDefinition.J1939Info.UseAddressClaim = Nodes.Any(x => x.TransportProtocol == TransportProtocol.J1939 &&
                                                            x.NodeDefinition.J1939Info.AddressType != NodeAddressType.Static) ||
                                                            node.ACMax > node.ACMin;
        }
    }

    internal bool HasChanges()
    {
        string newConfig = JsonUtility.Serialize(CanClientCalibration);
        return newConfig != originalCANConfigText; 
    }
}
