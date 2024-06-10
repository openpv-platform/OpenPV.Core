using Ahsoka.ServiceFramework;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.Bluetooth.Platform;

[ExcludeFromCodeCoverage]
internal class STServiceImplementation : BluetoothImplementationBase
{
    private const int SUCCESS = 0;

    private DBusInterop _interop;

    protected override void OnInitServiceImplementation(BluetoothService service)
    {
        _interop = new DBusInterop(service);
    }

    protected override ProfileConnectionState OnConnectDevice(AhsokaMessageHeader header, BluetoothConnectionRequest request)
    {
        ProfileConnectionState connectionState = new();

        int adapterIndex = connectionState.AdapterIndex = request.AdapterIndex;
        BluetoothProfiles profile = connectionState.Profile = request.RequestedProfiles;
        string deviceAddress = connectionState.DeviceMacAddress = request.DeviceMacAddress;

        connectionState.IsConnected = false;

        // Throw an exception (does not return) if adapter is disabled/unpowered.
        StopIfAdapterIsDisabled();

        int result = _interop.ConnectDevice(header, adapterIndex, deviceAddress, profile);

        if (result == SUCCESS)
        {
            State.IsConnected = true;
            connectionState.IsConnected = true;
        }
        else
        {
            State.IsConnected = false;
            connectionState.IsConnected = false;
        }

        return connectionState;

    } // end of method OnConnectDevice()

    protected override void OnDisconnectDevice(AhsokaMessageHeader header, string deviceAddress, BluetoothProfiles profile)
    {
        int adapterIndex = AdapterConfiguration.AdapterIndex;
        bool isConnected = State.IsConnected;

        // Throw an exception (does not return) if adapter is disabled/unpowered.
        StopIfAdapterIsDisabled();

        if (_interop.DisconnectDevice(adapterIndex, deviceAddress, profile) == SUCCESS)
            isConnected = false;

        if (isConnected != State.IsConnected)
        {
            State.IsConnected = isConnected;
        } // end of if ( isConnected != State.IsConnected )

    } // end of method OnDisconnectDevice()

    protected override void OnDisableAdapter(int adapterIndex)
    {
        _interop.SetAdapterPowered(adapterIndex, false);
        State.IsEnabled = false;
    } // end of method OnDisableAdapter()

    protected override void OnEnableAdapter(int adapterIndex)
    {
        _interop.SetAdapterPowered(adapterIndex, true);
        State.IsEnabled = true;
    } // end of method OnEnableAdapter()

    protected override void OnExecuteDiscovery(AhsokaMessageHeader header, int adapterIndex)
    {
        bool resetDiscoverable = false;

        // Throw an exception (does not return) if adapter is disabled/unpowered.
        StopIfAdapterIsDisabled();

        // Make adapter discoverable
        if (_interop.GetAdapterDiscoverable(adapterIndex) == false)
        {
            resetDiscoverable = true;
            _interop.SetAdapterDiscoverable(adapterIndex, true);
        }

        foreach (BluetoothDeviceDiscoveryInfo deviceInfo in _interop.GetDiscoveredDevicesInfo(adapterIndex, AdapterConfiguration.DiscoveryTimeout))
        {
            // Notify Device Found
            Service.SendDiscoveredDeviceNotification(header, deviceInfo);
        }

        // Reset adapter discoverable state
        if (resetDiscoverable)
            _interop.SetAdapterDiscoverable(adapterIndex, false);

    } // end of method OnExecuteDiscovery()

    protected override void OnInitAdapter(BluetoothAdapterSetup adapterConfiguration)
    {
        if (adapterConfiguration.Stack == SupportBluetoothStack.Bluez)
        {
            // Get the requested adapter
            int adapterIndex = adapterConfiguration.AdapterIndex;

            // Ensure adapter list has been initialized
            _interop.InitAdapterList();

            // Check if any adapters were found. If not, mark that the
            //   adapter is not present. This will prevent other BT methods
            //   from trying to access nonexistent adapters.
            if (_interop.GetAdapterListCount() == 0)
            {
                State.IsPresent = false;
            }
            else
            {
                State.IsPresent = true;

                // Put adapter in a known state
                _interop.SetAdapterConfig(adapterIndex, adapterConfiguration);
            }

            State.IsEnabled = false;
            State.IsConnectable = false;
            State.IsDiscoverable = false;
            State.IsConnected = false;
            State.IsPolling = false;

        } // end of if (adapterConfiguration.Stack == SupportBluetoothStack.Bluez)

    } // end of method OnInitAdapter()

    // TODO: Need to add logic to support modern device pairing (Secure Simple Pairing) where we return a code
    //       from the device before accepting the pairing.
    protected override BluetoothDevicePairedInfo OnPairDevice(AhsokaMessageHeader header, BluetoothDeviceDiscoveryInfo info)
    {
        int adapterIndex = AdapterConfiguration.AdapterIndex;

        BluetoothDevicePairedInfo pairedInfo = new()
        {
            AdapterIndex = adapterIndex,
            DeviceMacAddress = info.DeviceMacAddress,
            DeviceRssi = 0,
            DeviceName = "",
            Paired = false
        };

        // Throw an exception (does not return) if adapter is disabled/unpowered.
        StopIfAdapterIsDisabled();

        BluetoothDevicePairedInfo retval = _interop.PairDevice(adapterIndex, info.DeviceMacAddress);

        pairedInfo.DeviceRssi = retval.DeviceRssi;
        pairedInfo.DeviceName = retval.DeviceName;
        pairedInfo.Paired = retval.Paired;

        Service.SendPairedDeviceNotification(pairedInfo);

        return pairedInfo;

    } // end of method OnPairDevice()

    protected override void OnUnpairDevice(AhsokaMessageHeader header, BluetoothDevicePairedInfo info)
    {
        int adapterIndex = info.AdapterIndex;

        // Throw an exception (does not return) if adapter is disabled/unpowered.
        StopIfAdapterIsDisabled();

        if (_interop.RemoveDevice(adapterIndex, info.DeviceMacAddress) == SUCCESS)
        {
            info.Paired = false;
            Service.SendPairedDeviceNotification(header, info);
        }
    } // end of method OnUnpairDevice()

    protected override BluetoothPairedDevices OnGetPairedDevices(AhsokaMessageHeader header, int adapterIndex)
    {
        BluetoothPairedDevices pairedDevices = new()
        {
            AdapterIndex = adapterIndex
        };
        pairedDevices.PairedDeviceLists.Clear();

        // Throw an exception (does not return) if adapter is disabled/unpowered.
        StopIfAdapterIsDisabled();

        pairedDevices = _interop.GetPairedDevices(adapterIndex);

        return pairedDevices;

    } // end of method OnGetPairedDevices()

    protected override void OnUpdateAdapterState(int adapterIndex, bool isDiscoverable, bool isConnectable)
    {
        _interop.SetAdapterDiscoverable(adapterIndex, isDiscoverable);
        _interop.SetAdapterPairable(adapterIndex, isConnectable);

        State.IsDiscoverable = isDiscoverable;
        State.IsConnectable = isConnectable;

    } // end of method OnUpdateAdapterState()

    private void StopIfAdapterIsDisabled()
    {
        if (State.IsEnabled == false)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Adapter [{State.AdapterIndex}] is DISABLED!");
            throw new ApplicationException($"Adapter [{State.AdapterIndex}] is DISABLED!");
        }
    } // end of method StopIfAdapterIsDisabled()

} // end of internal class STServiceImplementation : BluetoothImplmentationBase
