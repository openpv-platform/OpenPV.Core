using Ahsoka.CustomerTools.Views;
using Ahsoka.Services.Can;
using Ahsoka.Utility;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Ahsoka.CustomerTools;

internal class MessageViewModel : ChildViewModelBase<MainViewModel>
{
    #region Fields
    const int extendedFrameBitPosition = 31;
    const int PDU2Threshold = 239;

    UserControl currentView;
    UserControl currentSignalView;

    CanSetupViewModel canViewModel;
    MessageSignalDefinition selectedSignal;
    bool isSelected = false;
    readonly J1939Helper.Id j1939Id = new();
    readonly ObservableCollection<MessageSignalDefinition> signals = new();
    readonly ObservableCollection<SignalModel> signalsModels = new();

    readonly List<NodeSelector> rxNodes = new();
    readonly List<NodeSelector> txNodes = new();
    #endregion

    #region ID Handling J1939 <-> RAW
    public bool IsJ1939 { get { return MessageType == MessageType.J1939ExtendedFrame; } }
    public bool IsPDU2 { get { return PDUF > PDU2Threshold; } }

    public MessageType MessageType
    {
        get { return MessageDefinition.MessageType; }
        set
        {
            if (value != MessageDefinition.MessageType)
            {
                MessageDefinition.Id = NegateExtendedBit(MessageDefinition.Id);

                if (value == MessageType.J1939ExtendedFrame)
                {
                    MessageDefinition.Id = NegateExtendedBit(MessageDefinition.Id);
                    j1939Id.ExtractValues(MessageDefinition.Id);
                }

                // Restore Extended Frame Bit
                if (value != MessageType.RawStandardFrame)
                    MessageDefinition.Id = AddExtendedBit(MessageDefinition.Id);
                else
                    MessageDefinition.Id = NegateExtendedBit(MessageDefinition.Id);
            }

            MessageDefinition.MessageType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IdMasked));
            OnPropertyChanged(nameof(IsJ1939));
        }
    }

    public uint Priority
    {
        get
        {
            return j1939Id.Priority;
        }
        set
        {
            j1939Id.Priority = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(J1939Id));
            OnPropertyChanged(nameof(IdMasked));
            
        }
    }

    public uint DataPage
    {
        get
        {
            return j1939Id.DataPage;
        }
        set
        {
            j1939Id.DataPage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(J1939Id));
            OnPropertyChanged(nameof(IdMasked));
        }
    }

    public uint PDUF
    {
        get
        {
            return j1939Id.PDUF;
        }
        set
        {
            j1939Id.PDUF = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPDU2));
            OnPropertyChanged(nameof(IdMasked));
        }
    }

    public uint Group
    {
        get
        {
            return j1939Id.PDUS;
        }
        set
        {
            j1939Id.PDUS = value;
            OnPropertyChanged();           
            OnPropertyChanged(nameof(IdMasked));
        }
    }

    public uint J1939Id
    {
        get
        {
            NodeViewModel Transmitter, Receiver = null;
            if (MessageDefinition.TransmitNodes[1] != -1 && MessageDefinition.ReceiveNodes[1] != -1)
            {
                Transmitter = canViewModel.Nodes.First(x => x.NodeDefinition.Id == (MessageDefinition.TransmitNodes[1]));
                Receiver = canViewModel.Nodes.First(x => x.NodeDefinition.Id == (MessageDefinition.ReceiveNodes[1]));
            }
            else if (MessageDefinition.TransmitNodes[0] != -1 && MessageDefinition.ReceiveNodes[0] != -1)
            {
                Transmitter = canViewModel.Nodes.First(x => x.NodeDefinition.Id == (MessageDefinition.TransmitNodes[0]));
                Receiver = canViewModel.Nodes.First(x => x.NodeDefinition.Id == (MessageDefinition.ReceiveNodes[0]));
            }
            else
                return j1939Id.WriteToUint();

            if (Transmitter.TransportProtocol != TransportProtocol.J1939)
                j1939Id.SourceAddress = 0;
            else if (Transmitter.IsSelf && Transmitter.ACMin >= Transmitter.ACMax)
                j1939Id.SourceAddress = Transmitter.ACMin;
            else if (Transmitter.IsSelf && Transmitter.ACMin < Transmitter.ACMax)
                j1939Id.SourceAddress = 0;
            else if (Transmitter.NodeDefinition.J1939Info.AddressType == NodeAddressType.Static)
                j1939Id.SourceAddress = (uint)Transmitter.NodeDefinition.J1939Info.AddressValueOne;
            else
                j1939Id.SourceAddress = 0;

            if (IsPDU2)
                j1939Id.PDUS = Group;
            else if (Receiver.TransportProtocol != TransportProtocol.J1939)
                j1939Id.PDUS = 0;
            else if (Receiver.IsSelf && Receiver.ACMin >= Receiver.ACMax)
                j1939Id.PDUS = Receiver.ACMin;
            else if (Receiver.IsSelf && Receiver.ACMin < Receiver.ACMax)
                j1939Id.PDUS = 0;
            else if (Receiver.NodeDefinition.J1939Info.AddressType == NodeAddressType.Static)
                j1939Id.PDUS = (uint)Receiver.NodeDefinition.J1939Info.AddressValueOne;
            else
                j1939Id.SourceAddress = 0;

            var id = j1939Id.WriteToUint();

            MessageDefinition.Id = AddExtendedBit(id);

            return id;
        }
    }

    public uint IdMasked
    {
        get
        {
            uint id;
            if (IsJ1939)
                id = J1939Id;
            else
                id = this.MessageDefinition.Id;

            // Remove Extended Frame 
            if (MessageType != MessageType.RawStandardFrame)
                id = NegateExtendedBit(id);

            return id;
        }
        set
        {
            if (MessageType == MessageType.J1939ExtendedFrame)
            {
                j1939Id.ExtractValues(value);
                MessageDefinition.Id = j1939Id.WriteToUint();
            }
            else
                MessageDefinition.Id = value;

            if (MessageType != MessageType.RawStandardFrame)
                MessageDefinition.Id = AddExtendedBit(MessageDefinition.Id);

            OnPropertyChanged();
        }
    }
    #endregion

    #region Roll Count and CRC
    public MessageSignalDefinition CrcSignal
    {
        get
        {
            return MessageDefinition.Signals.FirstOrDefault(x => x.StartBit == MessageDefinition.CrcBit);
        }
        set
        {
            if (value != null)
                MessageDefinition.CrcBit = value.StartBit;
        }
    }

    public MessageSignalDefinition RollCountSignal
    {
        get
        {
            return MessageDefinition.Signals.FirstOrDefault(x => x.StartBit == MessageDefinition.RollCountBit);
        }
        set
        {
            if (value != null)
            {
                MessageDefinition.RollCountBit = value.StartBit;
                MessageDefinition.RollCountLength = value.BitLength;
            }

        }
    }

    public bool HasCrc
    {
        get { return CrcType != CrcType.None; }
        set
        {
        }
    }

    public CrcType CrcType
    {
        get { return MessageDefinition.CrcType; }
        set
        {
            MessageDefinition.CrcType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasCrc));
        }
    }

    public bool HasRollCount
    {
        get { return MessageDefinition.HasRollCount; }
        set
        {
            MessageDefinition.HasRollCount = value;
            OnPropertyChanged();
        }
    }

    public uint RollCountBit
    {
        get { return MessageDefinition.RollCountBit; }
        set
        {
            MessageDefinition.RollCountBit = value;
            OnPropertyChanged();
        }
    }

    public uint RollCountLength
    {
        get { return MessageDefinition.RollCountLength; }
        set
        {
            MessageDefinition.RollCountLength = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Properties
    internal Ahsoka.Services.Can.ValueType[] ValueTypes { get; init; } = Enum.GetValues<Ahsoka.Services.Can.ValueType>();
    internal Ahsoka.System.ByteOrder[] ByteOrders { get; init; } = Enum.GetValues<Ahsoka.System.ByteOrder>();

    internal SignalModel SelectedSignalValue { get; set; }

    public ObservableCollection<MessageSignalDefinition> Signals { get { return signals; } }

    public ObservableCollection<SignalModel> SignalValues { get { return signalsModels; } }

    public MessageDefinition MessageDefinition { get; set; }

    public MessageSignalDefinition SelectedSignal
    {
        get { return selectedSignal; }
        set
        {
            selectedSignal = value; OnPropertyChanged();
        }
    }

    public CrcType[] CrcTypes { get; init; } = Enum.GetValues<CrcType>();

    public MuxRole[] MuxRoles { get; init; } = Enum.GetValues<MuxRole>();

    public MessageType[] MessageTypes { get; init; } = Enum.GetValues<MessageType>();

    public NodeSelector[] TransmitNodes
    {
        get { return txNodes.ToArray(); }
    }

    public NodeSelector SelectedTransmitNode
    {
        get;
        set;
    }

    public string Transmitter
    {
        get 
        {
            var transmit = "";
            foreach (var node in TransmitNodes)
            {
                if (node.Selected)
                    transmit += $"{node.Node}: {node.NodeViewModel.Name}";
            }
            return transmit;
        }
    }

public NodeSelector[] ReceiveNodes
    {
        get { return rxNodes.ToArray(); }
    }

    public NodeSelector SelectedReceiveNode
    {
        get;
        set;
    }



    [MaxLength(255)]
    public string Name
    {
        get { return MessageDefinition.Name; }
        set
        {
            MessageDefinition.Name = value;
            OnPropertyChanged();
        }
    }

    public int Rate
    {
        get { return MessageDefinition.Rate; }
        set
        {
            MessageDefinition.Rate = value;
            OnPropertyChanged();
        }
    }

    public bool FilterReceipt
    {
        get { return MessageDefinition.FilterReceipts; }
        set
        {
            MessageDefinition.FilterReceipts = value;
            OnPropertyChanged();
        }
    }

    [Range(0, 60000)]
    public Int32 Timeout
    {
        get { return MessageDefinition.TimeoutMs; }
        set
        {
            MessageDefinition.TimeoutMs = value;
            OnPropertyChanged();
        }
    }

    [MaxLength(255)]
    public string Comment
    {
        get { return MessageDefinition.Comment; }
        set
        {
            MessageDefinition.Comment = value;
            OnPropertyChanged();
        }
    }

    public bool IsEditable
    {
        get { return MessageDefinition.UserDefined; }
    }

    public bool IsSelected
    {
        get { return isSelected; }
        set
        {
            isSelected = value; OnPropertyChanged();
            OnPropertyChanged(nameof(IconColor));
            OnPropertyChanged(nameof(BackColor));
        }
    }

    public IBrush IconColor
    {
        get { return IsSelected ? Brushes.SteelBlue : Brushes.Gainsboro; }
        set { }
    }

    public IBrush BackColor
    {
        get { return IsSelected ? Brushes.WhiteSmoke : Brushes.White; }
        set { }
    }
    #endregion

    #region Methods
    internal CanSetupViewModel CanViewModel
    {
        get => canViewModel;
        set => canViewModel = value;
    }

    public MessageViewModel(MainViewModel model, CanSetupViewModel canViewModel, MessageDefinition definition)
        : base(model)
    {
        if (definition == null)
        {
            definition = new() { Name = "New Message", Id = 100, TransmitNodes = new int[] {-1, -1}, ReceiveNodes = new int[] { -1, -1 } };

            // FindNode Index
            for (uint i = 0; i < UInt32.MaxValue; i++)
                if (!canViewModel.Messages.Any(x => x.MessageDefinition.Id == i))
                {
                    definition.Id = i;
                    break;
                }
            canViewModel.CanClientCalibration.Messages.Add(definition);
        }

        MessageDefinition = definition;

        foreach (var node in canViewModel.Nodes)
        {
            txNodes.Add(new NodeSelector(this, node, true));
            rxNodes.Add(new NodeSelector(this, node, false));
        }
        SelectedTransmitNode = txNodes.FirstOrDefault(x => x.Selected);
        SelectedReceiveNode = rxNodes.FirstOrDefault(x => x.Selected);

        MessageDefinition.Signals.Sort(delegate (MessageSignalDefinition x, MessageSignalDefinition y)
        {
            return x.StartBit.CompareTo(y.StartBit);
        });

        j1939Id = new(MessageDefinition.Id);
        MessageDefinition.SetAddressOnSend = true;
        SelectedSignal = MessageDefinition.Signals.FirstOrDefault();

        this.CanViewModel = canViewModel;

        this.signals = new ObservableCollection<MessageSignalDefinition>(MessageDefinition.Signals);
    }

    public void ShowEditor()
    {
        currentView = new CANMessageEditView() { DataContext = this };
        ParentViewModel.Popup.ShowPopup(currentView);
    }

    public void EditSignalTransforms()
    {
        currentSignalView = new CANMessageSignalView() { DataContext = this };
        ParentViewModel.Popup.ShowPopup(currentSignalView);
    }

    public void EditValueTable()
    {
        LoadSignalValues();
        currentSignalView = new CANMessageValueView() { DataContext = this };
        ParentViewModel.Popup.ShowPopup(currentSignalView);
    }

    public void EditMuxInfo()
    {
        currentSignalView = new CANMessageMuxView() { DataContext = this };
        ParentViewModel.Popup.ShowPopup(currentSignalView);
    }

    public void CloseEditor()
    {
        if (currentSignalView != null)
        {
            ParentViewModel.Popup.DismissPopup(currentSignalView);

            if (currentSignalView is CANMessageValueView)
                UpdateSignalValues();

            currentSignalView = null;
        }
        else
        {
            ParentViewModel.Popup.DismissPopup(currentView);
            currentView = null;
        }
    }

    internal void AddItem()
    {
        var signalDef = new MessageSignalDefinition
        {
            Name = "New Signal",
            BitLength = 4,
            Id = 0,
            ValueType = Services.Can.ValueType.Unsigned,
            Scale = 1.0f
        };

        // FindNode Index
        if (signals.Count > 0)
        {
            for (uint i = 0; i < 255; i++)
                if (!this.Signals.Any(x => x.Id == i))
                {
                    signalDef.Id = i;
                    signalDef.Name += $" {i}";
                    break;
                }

            signalDef.StartBit = this.signals.Max(x => x.StartBit + x.BitLength);

        }

        this.MessageDefinition.Signals.Add(signalDef);
        this.Signals.Add(signalDef);
    }

    internal async void RemoveItem()
    {
        var continueWork = await ParentViewModel.Dialog.ShowDialog("Remove Signal", "Are you sure you wish to remove the Selected Signal?", "Yes", "Cancel");

        if (!continueWork)
            return;

        this.MessageDefinition.Signals.Remove(selectedSignal);
        this.Signals.Remove(selectedSignal);
    }

    internal void AddValue()
    {
        if (this.selectedSignal != null)
            for (int i = 0; i < 255; i++)
                if (!this.SelectedSignal.Values.Any(x => x.Key == i))
                {
                    var signal = new SignalModel() { Key = i, Value = "New Value" };
                    this.SignalValues.Add(signal);
                    break;
                }
    }

    internal void RemoveValue()
    {
        if (SelectedSignalValue != null)
        {
            this.SignalValues.Remove(SelectedSignalValue);
        }
    }

    internal uint AddExtendedBit(uint value)
    {
        return value | (1u << extendedFrameBitPosition);
    }

    internal uint NegateExtendedBit(uint id)
    {
        return id & ~(1 << extendedFrameBitPosition);
    }

    private void LoadSignalValues()
    {
        this.SignalValues.Clear();
        if (this.selectedSignal != null)
            foreach (var item in this.SelectedSignal.Values)
                this.SignalValues.Add(new() { Key = item.Key, Value = item.Value });
    }

    private void UpdateSignalValues()
    {
        if (selectedSignal == null)
            return;

        this.SelectedSignal?.Values.Clear();
        foreach (var item in this.SignalValues)
            SelectedSignal.Values[item.Key] = item.Value;
    }

    public void AddNode(NodeViewModel node)
    {
        txNodes.Add(new NodeSelector(this, node, true));
        rxNodes.Add(new NodeSelector(this, node, false));
    }

    public void RemoveNode(NodeViewModel node)
    {
        var delete = txNodes.First(x => x.Node == node.NodeDefinition.Id);
        txNodes.Remove(delete);
        delete = rxNodes.First(x => x.Node == node.NodeDefinition.Id);
        rxNodes.Remove(delete);
        UpdateNodes();
    }

    public void UpdateNodes()
    {
        MessageDefinition.TransmitNodes = CreateNodeArray(txNodes);
        MessageDefinition.ReceiveNodes = CreateNodeArray(rxNodes);
        OnPropertyChanged(nameof(IdMasked));
        OnPropertyChanged(nameof(Transmitter));
    }

    public int[] CreateNodeArray(List<NodeSelector> nodes)
    {
        var nodeArray = new int[2] { -1, -1 };
        foreach (var node in nodes)
        {
            if (node.Selected)
            {
                foreach (var port in node.NodeViewModel.NodeDefinition.Ports)
                    nodeArray[port] = node.Node;
            }
        }
        return nodeArray;
    }
    #endregion
}

internal class NodeSelector
{
    readonly MessageViewModel model;
    bool transmit = false;
    bool selected = false;

    public bool Selected
    {
        get
        {
            return selected;
        }
        set
        {
            selected = value;
            model.UpdateNodes();
        }
    }
    public string Name
    {
        get
        {
            return $"Node: {NodeViewModel.NodeDefinition.Id} {NodeViewModel.NodeDefinition.Name}";
        }
    }
    public int Node
    {
        get
        {
            return NodeViewModel.NodeDefinition.Id;
        }
    }
    public NodeViewModel NodeViewModel { get;  set; }

    public NodeSelector(MessageViewModel model, NodeViewModel node, bool transmit)
    {
        this.model = model;
        NodeViewModel = node;
        this.transmit = transmit;

        if (transmit)
            selected = model.MessageDefinition.TransmitNodes.Any(x => x == node.NodeDefinition.Id);
        else
            selected = model.MessageDefinition.ReceiveNodes.Any(x => x == node.NodeDefinition.Id);
    }
}

internal class SignalModel
{
    [Range(0, int.MaxValue)]
    public int Key { get; set; }
    [MaxLength(255)]
    public string Value { get; set; }
}

