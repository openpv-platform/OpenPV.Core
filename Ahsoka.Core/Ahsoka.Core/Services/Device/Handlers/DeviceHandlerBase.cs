using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.Utility;
using GLib;
using System;

namespace Ahsoka.Services.Device.Handlers;

internal class DeviceHandlerBase
{
    internal bool SupportsFirmware { get; init; }

    internal virtual void EnableDevice(DeviceInfo deviceInfo, global::System.Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        deviceInfo.State = DeviceState.Enabled;
        if (sendNotification != null)
            sendNotification(DeviceMessageTypes.Ids.DeviceEnabled, deviceInfo, null);
    }

    internal virtual void DisableDevice(DeviceInfo deviceInfo, global::System.Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
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
}