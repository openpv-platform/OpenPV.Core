using Ahsoka.ServiceFramework;
using Ahsoka.System.Hardware;
using System;

namespace Ahsoka.Services.Bluetooth;

internal abstract class BluetoothImplementationBase
{
    public BluetoothAdapterState State { get; private set; } = new BluetoothAdapterState();

    public BluetoothService Service { get; private set; }

    public BluetoothAdapterSetup AdapterConfiguration { get; private set; }

    public void Disable()
    {
        State.IsEnabled = false;
        OnDisableAdapter(State.AdapterIndex);
    }

    protected abstract void OnDisableAdapter(int adapterIndex);

    public void Enable()
    {
        State.IsEnabled = true;
        OnEnableAdapter(State.AdapterIndex);
    }

    protected abstract void OnEnableAdapter(int adapterIndex);

    public void Init(BluetoothService service, BluetoothHardwareAdapter item)
    {
        this.Service = service ?? throw new ArgumentNullException(nameof(service));

        State.AdapterIndex = item.Index;

        AdapterConfiguration = new BluetoothAdapterSetup
        {
            AdapterIndex = item.Index,
            PairingTimeout = item.PairingTimeout,
            DiscoveryTimeout = item.DiscoveryTimeout,
            ConnectionTimeout = item.ConnectionTimeout,
            Stack = (SupportBluetoothStack)item.Stack,
            DeviceInterfacePath = item.DeviceInterfacePath
        };

        OnInitServiceImplementation(service);

        OnInitAdapter(AdapterConfiguration);
    }

    protected abstract void OnInitServiceImplementation(BluetoothService service);

    protected abstract void OnInitAdapter(BluetoothAdapterSetup adapterConfiguration);

    public ProfileConnectionState ConnectDevice(AhsokaMessageHeader header, BluetoothConnectionRequest request)
    {
        if (State.IsPresent)
        {
            return OnConnectDevice(header, request);
        }
        else
        {
            ProfileConnectionState result = new()
            {
                AdapterIndex = request.AdapterIndex,
                DeviceMacAddress = request.DeviceMacAddress,
                Profile = request.RequestedProfiles,
                IsConnected = false
            };
            return result;
        }
    }

    protected abstract ProfileConnectionState OnConnectDevice(AhsokaMessageHeader header, BluetoothConnectionRequest request);

    public void DisconnectDevice(AhsokaMessageHeader header, string deviceMACAddress, BluetoothProfiles profile)
    {
        if (State.IsPresent)
            OnDisconnectDevice(header, deviceMACAddress, profile);
    }

    protected abstract void OnDisconnectDevice(AhsokaMessageHeader header, string deviceMACAddress, BluetoothProfiles profile);

    public BluetoothDevicePairedInfo PairDevice(AhsokaMessageHeader header, BluetoothDeviceDiscoveryInfo info)
    {
        if (State.IsPresent)
        {
            return OnPairDevice(header, info);
        }
        else
        {
            BluetoothDevicePairedInfo result = new()
            {
                AdapterIndex = info.AdapterIndex,
                DeviceMacAddress = info.DeviceMacAddress,
                DeviceRssi = info.DeviceRssi,
                ConnectedProfiles = 0
            };
            return result;
        }
    }

    protected abstract BluetoothDevicePairedInfo OnPairDevice(AhsokaMessageHeader header, BluetoothDeviceDiscoveryInfo info);

    public BluetoothPairedDevices GetPairedDevices(AhsokaMessageHeader header, int adapterIndex)
    {
        if (State.IsPresent)
        {
            return OnGetPairedDevices(header, adapterIndex);
        }
        else
        {
            BluetoothPairedDevices result = new()
            {
                AdapterIndex = adapterIndex
            };
            result.PairedDeviceLists.Clear();
            return result;
        }
    }

    protected abstract BluetoothPairedDevices OnGetPairedDevices(AhsokaMessageHeader header, int adapterIndex);

    public void StartDiscovery(AhsokaMessageHeader header, int adapterIndex)
    {
        if (State.IsPresent)
        {
            State.IsPolling = true;
            OnExecuteDiscovery(header, adapterIndex);
            State.IsPolling = false;
        }
    }

    protected abstract void OnExecuteDiscovery(AhsokaMessageHeader header, int adapterIndex);

    public void UnpairDevice(AhsokaMessageHeader header, BluetoothDevicePairedInfo info)
    {
        if (State.IsPresent)
            OnUnpairDevice(header, info);
    }

    protected abstract void OnUnpairDevice(AhsokaMessageHeader header, BluetoothDevicePairedInfo info);

    public void UpdateState(bool isDiscoverable, bool isConnectable)
    {
        if (State.IsPresent)
        {
            State.IsDiscoverable = isDiscoverable;
            State.IsConnectable = isConnectable;
            OnUpdateAdapterState(State.AdapterIndex, isDiscoverable, isConnectable);
        }
    }

    protected abstract void OnUpdateAdapterState(int adapterIndex, bool isDiscoverable, bool isConnectable);

}