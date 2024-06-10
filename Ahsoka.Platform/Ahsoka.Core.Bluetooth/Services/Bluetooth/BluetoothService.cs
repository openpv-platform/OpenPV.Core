#pragma warning disable CS1591
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Bluetooth.Platform;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;

namespace Ahsoka.Services.Bluetooth;

[AhsokaService(Name)]
public class BluetoothService : AhsokaServiceBase<BluetoothMessageTypes.Ids>
{
    public const string Name = "BluetoothService";
    BluetoothConfiguration configuration;
    readonly Dictionary<int, BluetoothImplementationBase> adapterImpl = new();

    internal static BluetoothImplementationBase GetInterop()
    {
        return SystemInfo.CurrentPlatform switch
        {
            PlatformFamily.Windows64 => new WindowsServiceImplementation(),
            PlatformFamily.OpenViewLinux => new STServiceImplementation(),
            _ => throw new PlatformNotSupportedException(),
        };
    }

    public BluetoothService()
    : this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    public BluetoothService(ServiceConfiguration config) :
        base(config, new BluetoothServiceMessages())
    {
        configuration = LoadDefaultConfiguration();
    }

    private BluetoothConfiguration LoadDefaultConfiguration()
    {
        BluetoothConfiguration config = new();

        foreach (var item in SystemInfo.HardwareInfo.BluetoothInfo.BluetoothAdapters)
        {
            // Load Adapter for Platform Adapter
            var impl = GetInterop();
            impl.Init(this, item);
            adapterImpl.Add(item.Index, impl);

            config.AdapterConfigurations.Add(impl.AdapterConfiguration);
        }

        return config;
    }

    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<BluetoothMessageTypes.Ids>())
        {
            case BluetoothMessageTypes.Ids.GetBluetoothConfiguration:
                GetBluetoothConfiguration(messageHeader, message as BluetoothAdapterRequest);
                break;

            case BluetoothMessageTypes.Ids.SetBluetoothConfiguration:
                SetBluetoothConfiguration(messageHeader, message as BluetoothConfiguration);
                break;

            case BluetoothMessageTypes.Ids.EnableAdapter:
                if (message is BluetoothAdapterRequest enableAdapterTarget)
                    EnableAdapter(messageHeader, enableAdapterTarget.AdapterIndex);
                break;

            case BluetoothMessageTypes.Ids.DisableAdapter:
                if (message is BluetoothAdapterRequest disableAdapterTarget)
                    DisableAdapter(messageHeader, disableAdapterTarget.AdapterIndex);
                break;

            case BluetoothMessageTypes.Ids.RequestAdapterState:
                if (message is BluetoothAdapterRequest adapterStateTarget)
                    RequestAdapterState(messageHeader, adapterStateTarget.AdapterIndex);
                break;

            case BluetoothMessageTypes.Ids.UpdateAdapterState:
                if (message is BluetoothAdapterState adapterState)
                    UpdateAdapterState(messageHeader, adapterState);
                break;

            case BluetoothMessageTypes.Ids.StartDiscovery:
                if (message is BluetoothAdapterRequest adapterDiscoveryTarget)
                    StartDiscovery(messageHeader, adapterDiscoveryTarget.AdapterIndex);
                break;

            case BluetoothMessageTypes.Ids.RequestToPair:
                if (message is BluetoothDeviceDiscoveryInfo pairingTarget)
                    RequestToPair(messageHeader, pairingTarget);
                break;

            case BluetoothMessageTypes.Ids.RequestToUnpair:
                if (message is BluetoothDevicePairedInfo unpairTarget)
                    RequestToUnpair(messageHeader, unpairTarget);
                break;

            case BluetoothMessageTypes.Ids.GetPairedDevices:
                if (message is BluetoothAdapterRequest adapterTarget)
                    GetPairedDevices(messageHeader, adapterTarget.AdapterIndex);
                break;

            case BluetoothMessageTypes.Ids.RequestConnect:
                if (message is BluetoothConnectionRequest connectionTarget)
                    RequestConnect(messageHeader, connectionTarget);
                break;

            case BluetoothMessageTypes.Ids.RequestDisconnect:
                if (message is BluetoothConnectionRequest disconnectionTarget)
                    RequestDisconnect(messageHeader, disconnectionTarget);
                break;

        } // end of switch (messageHeader.TransportId.IntToEnum<BluetoothMessageTypes.Ids>())
    } // end of override method OnHandleReceive(AhsokaMessageHeader messageHeader, object message)

    private void GetBluetoothConfiguration(AhsokaMessageHeader messageHeader, BluetoothAdapterRequest request)
    {
        SendMessageInternal(messageHeader, configuration);
    }

    private void SetBluetoothConfiguration(AhsokaMessageHeader messageHeader, BluetoothConfiguration config)
    {
        configuration = config;
        SendMessageInternal(messageHeader);
    }

    private void EnableAdapter(AhsokaMessageHeader messageHeader, int index)
    {
        adapterImpl[index]?.Enable();
        SendMessageInternal(messageHeader);
    }

    private void DisableAdapter(AhsokaMessageHeader messageHeader, int index)
    {
        adapterImpl[index]?.Disable();
        SendMessageInternal(messageHeader);
    }

    internal void RequestAdapterState(AhsokaMessageHeader messageHeader, int adapterIndex)
    {
        var replyHeader = CreateReplyMessageHeader(messageHeader, BluetoothMessageTypes.Ids.RequestAdapterState);
        SendMessageInternal(replyHeader, adapterImpl[adapterIndex].State);
    }

    private void UpdateAdapterState(AhsokaMessageHeader messageHeader, BluetoothAdapterState state)
    {
        adapterImpl[state.AdapterIndex]?.UpdateState(state.IsDiscoverable, state.IsConnectable);
        SendMessageInternal(messageHeader);
    }

    private void StartDiscovery(AhsokaMessageHeader messageHeader, int index)
    {
        adapterImpl[index]?.StartDiscovery(messageHeader, index);
        SendMessageInternal(messageHeader);
    }

    internal void SendDiscoveredDeviceNotification(AhsokaMessageHeader messageHeader, BluetoothDeviceDiscoveryInfo info)
    {
        var replyHeader = CreateReplyMessageHeader(messageHeader, BluetoothMessageTypes.Ids.DiscoveryDeviceNotification);
        SendMessageInternal(replyHeader, info);
    }

    internal void SendDiscoveredDeviceNotification(BluetoothDeviceDiscoveryInfo info)
    {
        BluetoothDeviceDiscoveryInfo response = new()
        {
            AdapterIndex = info.AdapterIndex,
            DeviceName = info.DeviceName,
            DeviceRssi = info.DeviceRssi,
            DeviceMacAddress = info.DeviceMacAddress
        };

        AhsokaMessageHeader notificationHeader = new()
        {
            TransportId = BluetoothMessageTypes.Ids.DiscoveryDeviceNotification.EnumToInt(),
        };

        SendMessageInternal(notificationHeader, response, true);
    }

    private void RequestToPair(AhsokaMessageHeader messageHeader, BluetoothDeviceDiscoveryInfo request)
    {
        BluetoothDevicePairedInfo retval;

        retval = adapterImpl[request.AdapterIndex]?.PairDevice(messageHeader, request);
        SendMessageInternal(messageHeader, retval);
    }

    private void RequestToUnpair(AhsokaMessageHeader messageHeader, BluetoothDevicePairedInfo request)
    {
        adapterImpl[request.AdapterIndex]?.UnpairDevice(messageHeader, request);
        SendMessageInternal(messageHeader);
    }

    private void GetPairedDevices(AhsokaMessageHeader messageHeader, int adapterIndex)
    {
        BluetoothPairedDevices retval;

        retval = adapterImpl[adapterIndex]?.GetPairedDevices(messageHeader, adapterIndex);
        SendMessageInternal(messageHeader, retval);
    }

    internal void SendPairedDeviceNotification(AhsokaMessageHeader messageHeader, BluetoothDevicePairedInfo info)
    {
        var replyHeader = CreateReplyMessageHeader(messageHeader, BluetoothMessageTypes.Ids.PairedDeviceNotification);
        SendMessageInternal(replyHeader, info);
    }

    internal void SendPairedDeviceNotification(BluetoothDevicePairedInfo info)
    {
        AhsokaMessageHeader notificationHeader = new()
        {
            TransportId = BluetoothMessageTypes.Ids.PairedDeviceNotification.EnumToInt(),
        };

        SendMessageInternal(notificationHeader, info, true);
    }

    private void RequestConnect(AhsokaMessageHeader messageHeader, BluetoothConnectionRequest request)
    {
        ProfileConnectionState retval;

        retval = adapterImpl[request.AdapterIndex]?.ConnectDevice(messageHeader, request);

        var replyHeader = CreateReplyMessageHeader(messageHeader, BluetoothMessageTypes.Ids.RequestConnect);
        SendMessageInternal(replyHeader, retval);
    }

    private void RequestDisconnect(AhsokaMessageHeader messageHeader, BluetoothConnectionRequest request)
    {
        adapterImpl[request.AdapterIndex]?.DisconnectDevice(messageHeader, request.DeviceMacAddress, request.RequestedProfiles);
        SendMessageInternal(messageHeader);
    }

    internal void SendProfileConnectionStateChangedNotification(AhsokaMessageHeader messageHeader, ProfileConnectionState connectionState)
    {
        var replyHeader = CreateReplyMessageHeader(messageHeader, BluetoothMessageTypes.Ids.ProfileConnectionStateChanged);
        SendMessageInternal(replyHeader, connectionState);
    }

    internal void SendProfileConnectionStateChangedNotification(ProfileConnectionState connectionState)
    {
        ProfileConnectionState response = new()
        {
            AdapterIndex = connectionState.AdapterIndex,
            DeviceMacAddress = connectionState.DeviceMacAddress,
            Profile = connectionState.Profile,
            IsConnected = connectionState.IsConnected,
        };

        AhsokaMessageHeader notificationHeader = new()
        {
            TransportId = BluetoothMessageTypes.Ids.ProfileConnectionStateChanged.EnumToInt(),
        };

        SendMessageInternal(notificationHeader, response, true);
    }

}
