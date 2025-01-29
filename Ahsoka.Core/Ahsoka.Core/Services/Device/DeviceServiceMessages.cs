using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using System.Collections.Generic;

namespace Ahsoka.Services.Device;

/// <summary>
///  System Service Metadata Class
/// </summary>
public class DeviceServiceMessages : AhsokaMessagesBase
{
    /// <InheritDoc/>
    public DeviceServiceMessages()
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
    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\DeviceService.proto", Properties.Resources.SystemService);

        return result;
    }

}
