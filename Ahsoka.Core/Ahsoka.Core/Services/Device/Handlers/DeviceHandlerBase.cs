using Ahsoka.ServiceFramework;
using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;

namespace Ahsoka.Services.Device.Handlers;

internal class DeviceHandlerBase
{
    internal bool SupportsFirmware { get; init; }

    internal virtual List<UsbDeviceId> Init(DeviceInfo deviceInfo, Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        return new List<UsbDeviceId>();
    }

    internal virtual void EnableDevice(DeviceInfo deviceInfo, Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        deviceInfo.State = DeviceState.Enabled;
        if (sendNotification != null)
            sendNotification(DeviceMessageTypes.Ids.DeviceEnabled, deviceInfo, null);
    }

    internal virtual void DisableDevice(DeviceInfo deviceInfo, Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        deviceInfo.State = DeviceState.Disabled;
        if (sendNotification != null)
            sendNotification(DeviceMessageTypes.Ids.DeviceDisabled, deviceInfo, null);
    }

    public static DeviceHandlerBase GetDeviceHandler(DeviceTypes deviceTypes)
    {
        switch (deviceTypes)
        {
            case DeviceTypes.OnboardWirelessComboModule:
                return new Esp32Handler();
            case DeviceTypes.OnboardWifiModule:
                return new WiFiModuleHandler();
            case DeviceTypes.OnboardBtModule:
                return new BTModuleHandler();
            case DeviceTypes.OnboardEthernet:
                return new EthernetHandler();
            case DeviceTypes.UsbCellularNetwork:
                return new CellularHandler();
            case DeviceTypes.UsbEthernet:
                return new UsbEthernetHandler();
            case DeviceTypes.IoControlModule:
            case DeviceTypes.DeviceTypeUnknown:
            default:
                return null;
        }
    }

    internal virtual byte[] GetFirmwareData(PlatformFamily platformFamily, string platformQualifier)
    {
        return null;
    }

    internal virtual void InstallLocal(string fwFile, LocalProgress<InstallLogInfo> log)
    {
        
    }

    internal virtual DeviceState GetDeviceState()
    {
        return DeviceState.NoState;
    }
}