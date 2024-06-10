using Ahsoka.ServiceFramework;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System;
using System.Threading.Tasks;

namespace Ahsoka.Services.Bluetooth.Platform;

internal class WindowsServiceImplementation : BluetoothImplementationBase
{
    protected override void OnInitServiceImplementation(BluetoothService service)
    {

    }

    protected override void OnInitAdapter(BluetoothAdapterSetup adapterConfiguration)
    {
        // Check if a radio is found. If not, mark that the adapter
        //   as not present. This will prevent other BT methods
        //   from trying to access nonexistent adapters/radios.
        BluetoothRadio defaultRadio = null;
        try
        {
            defaultRadio = BluetoothRadio.Default;
        }
        catch { }

        if (defaultRadio != null)
        {
            adapterConfiguration.DeviceDiscoveryName = defaultRadio.Name;
            adapterConfiguration.DeviceMacAddress = defaultRadio.LocalAddress.ToString();
            State.IsPresent = true;
            State.IsEnabled = true;
            State.IsConnectable = defaultRadio.Mode.HasFlag(RadioMode.Connectable);
            State.IsDiscoverable = !defaultRadio.Mode.HasFlag(RadioMode.Discoverable);
        }
        else
        {
            State.IsPresent = false;
            State.IsEnabled = false;
            State.IsConnectable = false;
            State.IsDiscoverable = false;
            State.IsConnected = false;
            State.IsPolling = false;
        }
    }

    protected override void OnExecuteDiscovery(AhsokaMessageHeader header, int adapterIndex)
    {
        var result = Task.Run(() =>
        {
            var cli = new BluetoothClient
            {
                InquiryLength = TimeSpan.FromMilliseconds(AdapterConfiguration.DiscoveryTimeout)
            };
            foreach (var item in cli.DiscoverDevices(100))
            {
                // Notify Device Found
                BluetoothDeviceDiscoveryInfo info = new()
                {
                    DeviceMacAddress = item.DeviceAddress.ToString(),
                    DeviceName = item.DeviceName,
                    AdapterIndex = this.State.AdapterIndex
                };

                Service.SendDiscoveredDeviceNotification(header, info);
            }
        });
        result.Wait();
    }

    protected override void OnDisableAdapter(int adapterIndex)
    {
        State.IsEnabled = false;
    }

    protected override void OnEnableAdapter(int adapterIndex)
    {
        State.IsEnabled = true;
    }

    protected override void OnUpdateAdapterState(int adapterIndex, bool isDiscoverable, bool isConnectable)
    {
        uint mode = 0;

        if (isDiscoverable)
            mode |= Convert.ToUInt32(RadioMode.Discoverable);
        else if (isConnectable)
            mode |= Convert.ToUInt32(RadioMode.Connectable);

        BluetoothRadio.Default.Mode = (RadioMode)mode;

        State.IsDiscoverable = BluetoothRadio.Default.Mode.HasFlag(RadioMode.Discoverable);
        State.IsConnectable = !BluetoothRadio.Default.Mode.HasFlag(RadioMode.Connectable);
    }

    protected override ProfileConnectionState OnConnectDevice(AhsokaMessageHeader header, BluetoothConnectionRequest request)
    {
        throw new global::System.NotImplementedException();
    }

    protected override void OnDisconnectDevice(AhsokaMessageHeader header, string deviceAddress, BluetoothProfiles profile)
    {
        throw new global::System.NotImplementedException();
    }

    protected override BluetoothDevicePairedInfo OnPairDevice(AhsokaMessageHeader header, BluetoothDeviceDiscoveryInfo info)
    {
        throw new global::System.NotImplementedException();
    }

    protected override void OnUnpairDevice(AhsokaMessageHeader header, BluetoothDevicePairedInfo info)
    {
        throw new global::System.NotImplementedException();
    }

    protected override BluetoothPairedDevices OnGetPairedDevices(AhsokaMessageHeader header, int adapterIndex)
    {
        throw new global::System.NotImplementedException();
    }
}
