using Ahsoka.Core;
using Ahsoka.Installer;
using System.Collections.Generic;

namespace Ahsoka.Services.Device;

/// <summary>
///  System Service Metadata Class
/// </summary>
public class DeviceServiceMessages : AhsokaMessagesBase
{
    /// <InheritDoc/>
    public DeviceServiceMessages() : base(DeviceService.Name)
    {
        this.RegisterServiceRequest(DeviceMessageTypes.Ids.GetUsbInfo, typeof(EmptyNotification), typeof(UsbInfo));
        this.RegisterServiceRequest(DeviceMessageTypes.Ids.SetUsbInfo, typeof(UsbInfo), typeof(EmptyNotification));

        this.RegisterServiceRequest(DeviceMessageTypes.Ids.SetSerialConfig, typeof(RS485ConfigNotification), typeof(EmptyNotification));
        this.RegisterServiceRequest(DeviceMessageTypes.Ids.UpdateTouchTune, typeof(TouchTuneInfo), typeof(TouchTuneInfoReply));

        this.RegisterServiceRequest(DeviceMessageTypes.Ids.GetDevices, typeof(DeviceInfoRequest), typeof(DeviceList));

        this.RegisterServiceRequest(DeviceMessageTypes.Ids.EnableDevice, typeof(DeviceInfo), typeof(EmptyNotification));
        this.RegisterServiceRequest(DeviceMessageTypes.Ids.DisableDevice, typeof(DeviceInfo), typeof(EmptyNotification));

        // This Enables Device Notifications - Otherwise the Clients must use Polling
        // Causes all devices to notify state as well.
        this.RegisterServiceRequest(DeviceMessageTypes.Ids.EnableDeviceNotifications, typeof(EmptyNotification), typeof(EmptyNotification));

        this.RegisterServiceNotification(DeviceMessageTypes.Ids.OnboardDeviceReady, typeof(DeviceInfo));
        this.RegisterServiceNotification(DeviceMessageTypes.Ids.UsbDeviceAdded, typeof(UsbDeviceInfo));
        this.RegisterServiceNotification(DeviceMessageTypes.Ids.UsbDeviceRemoved, typeof(UsbDeviceInfo));

        this.RegisterServiceNotification(DeviceMessageTypes.Ids.DeviceEnabled, typeof(DeviceInfo));
        this.RegisterServiceNotification(DeviceMessageTypes.Ids.DeviceDisabled, typeof(DeviceInfo));
    }

    /// <InheritDoc/>
    protected override void OnGetParameters(out string group, List<ParameterData> values, PackageInformation info)
    {
        group = DeviceService.Name;
    }

    /// <InheritDoc/>
    public override byte[] GetProtoMessageFile()
    {
        return this.GenerateProtoFile("AhsokaDevice", typeof(DeviceMessageTypes.Ids));
    }


}
