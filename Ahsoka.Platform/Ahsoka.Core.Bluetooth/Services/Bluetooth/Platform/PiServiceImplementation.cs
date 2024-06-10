using Ahsoka.ServiceFramework;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.Bluetooth.Platform;

[ExcludeFromCodeCoverage]
internal class PiServiceImplementation : BluetoothImplementationBase
{
    protected override void OnInitServiceImplementation(BluetoothService service)
    {
        throw new global::System.NotImplementedException();
    }

    protected override void OnInitAdapter(BluetoothAdapterSetup adapterConfiguration)
    {
        throw new global::System.NotImplementedException();
    }

    protected override void OnExecuteDiscovery(AhsokaMessageHeader header, int adapterIndex)
    {
        throw new global::System.NotImplementedException();
    }

    protected override void OnDisableAdapter(int adapterIndex)
    {
        throw new global::System.NotImplementedException();
    }

    protected override void OnEnableAdapter(int adapterIndex)
    {
        throw new global::System.NotImplementedException();
    }

    protected override void OnUpdateAdapterState(int adapterIndex, bool isDiscoverable, bool isConnectable)
    {
        throw new global::System.NotImplementedException();
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
