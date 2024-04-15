#pragma warning disable CS1591
using Ahsoka.ServiceFramework;
using Ahsoka.System;

namespace Ahsoka.Services.Bluetooth;

public class BluetoothServiceClient : AhsokaClientBase<BluetoothMessageTypes.Ids>
{
    #region Fields
    #endregion

    #region Methods

    public BluetoothServiceClient()
        : this(ConfigurationLoader.GetServiceConfig(BluetoothService.Name))
    {

    }


    public BluetoothServiceClient(ServiceConfiguration config) :
        base(config, new BluetoothServiceMessages())
    {

    }


    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndpoint OnCreateDefaultService()
    {
        return new BluetoothService(this.ServiceConfig);
    }

    public BluetoothConfiguration GetAdapterConfiguration()
    {
        return SendNotificationWithResponse<BluetoothConfiguration>(BluetoothMessageTypes.Ids.GetBluetoothConfiguration, new EmptyNotification { });
    }

    public void EnableAdapter(int adapterIndex)
    {
        SendNotificationWithResponse<EmptyNotification>(BluetoothMessageTypes.Ids.EnableAdapter, new BluetoothAdapterRequest { AdapterIndex = adapterIndex });
    }

    public void DisableAdapter(int adapterIndex)
    {
        SendNotificationWithResponse<EmptyNotification>(BluetoothMessageTypes.Ids.DisableAdapter, new BluetoothAdapterRequest { AdapterIndex = adapterIndex });
    }

    public void SetAdapterState(int adapterIndex, bool discoverable, bool connectable)
    {
        SendNotificationWithResponse<EmptyNotification>(BluetoothMessageTypes.Ids.UpdateAdapterState, new BluetoothAdapterState
        {
            AdapterIndex = adapterIndex,
            IsDiscoverable = discoverable,
            IsConnectable = connectable
        });
    }

    public void StartDiscovery(int adapterIndex)
    {
        SendNotificationWithResponse<EmptyNotification>(BluetoothMessageTypes.Ids.StartDiscovery, new BluetoothAdapterRequest { AdapterIndex = adapterIndex });
    }

    public BluetoothAdapterState RequestAdapterState(int adapterIndex)
    {
        return SendNotificationWithResponse<BluetoothAdapterState>(BluetoothMessageTypes.Ids.RequestAdapterState, new BluetoothAdapterRequest { AdapterIndex = adapterIndex });
    }

    public ProfileConnectionState RequestConnect(int adapterIndex, string deviceMACAddress, BluetoothProfiles requestedProfiles)
    {
        return SendNotificationWithResponse<ProfileConnectionState>(BluetoothMessageTypes.Ids.RequestConnect, new BluetoothConnectionRequest { AdapterIndex = adapterIndex, DeviceMacAddress = deviceMACAddress, RequestedProfiles = requestedProfiles });
    }

    public void RequestDisconnect(int adapterIndex, string deviceMACAddress, BluetoothProfiles requestedProfiles)
    {
        SendNotificationWithResponse<EmptyNotification>(BluetoothMessageTypes.Ids.RequestDisconnect, new BluetoothConnectionRequest { AdapterIndex = adapterIndex, DeviceMacAddress = deviceMACAddress, RequestedProfiles = requestedProfiles });
    }

    public BluetoothDevicePairedInfo RequestToPair(int adapterIndex, string deviceMACAddress)
    {
        return SendNotificationWithResponse<BluetoothDevicePairedInfo>(BluetoothMessageTypes.Ids.RequestToPair, new BluetoothDeviceDiscoveryInfo { AdapterIndex = adapterIndex, DeviceMacAddress = deviceMACAddress });
    }

    public void RequestToUnpair(int adapterIndex, string deviceMACAddress)
    {
        SendNotificationWithResponse<EmptyNotification>(BluetoothMessageTypes.Ids.RequestToUnpair, new BluetoothDevicePairedInfo { AdapterIndex = adapterIndex, DeviceMacAddress = deviceMACAddress });
    }

    public BluetoothPairedDevices RequestPairedDevices(int adapterIndex)
    {
        return SendNotificationWithResponse<BluetoothPairedDevices>(BluetoothMessageTypes.Ids.GetPairedDevices, new BluetoothAdapterRequest { AdapterIndex = adapterIndex });
    }

    #endregion
}

