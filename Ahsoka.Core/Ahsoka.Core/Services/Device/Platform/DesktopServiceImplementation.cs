using Ahsoka.Services.Device.Handlers;
using Ahsoka.System;
using Ahsoka.Utility;
using Microsoft.Extensions.Azure;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ahsoka.Services.Device;

[ExcludeFromCodeCoverage]
internal class DesktopServiceImplementation : IDeviceServiceImplementation
{
    UsbInfo usbInfo = new UsbInfo() { Mode = UsbMode.Host };
    private Action<DeviceMessageTypes.Ids, object, byte[]> sendNotificationHandler;
    private DeviceList deviceList;

    public void Init(Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        sendNotificationHandler = sendNotification;
        deviceList = new DeviceList()
        {
            Devices = { new DeviceInfo() { DeviceType = DeviceTypes.OnboardWifiModule, Id = 1234 } }
        };
     }

    public DeviceList HandleGetDevices(DeviceInfoRequest deviceInfoRequest)
    {
        DeviceList list = new DeviceList();
        foreach (var item in deviceList.Devices)
        {
            var services = DeviceInfo.GetSupportedServices(item.DeviceType);
            if (deviceInfoRequest.FeatureSearch == SupportedFeatures.FeaturesUnknown  || services.Contains(deviceInfoRequest.FeatureSearch))
                list.Devices.Add(item);
        }
        return deviceList;
    }

    public UsbInfo GetUsbInfo()
    {
        // Not Implemented
        return usbInfo;
    }

    public void SetUsbInfo(UsbInfo usbInfo)
    {
        this.usbInfo = usbInfo;
    }

    private static string GetWMICValue(string args)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = @"wmic",
            Arguments = args,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        var process = Process.Start(processStartInfo);
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output.Substring(output.IndexOf("\r\n")).Replace("\r", "").Replace("\n", "").Trim();
    }

    void IDeviceServiceImplementation.SetRS485Config(RS485ConfigNotification serial_rs485_config) { }

    public TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo updateTouchscreen)
    {
        var response = new TouchTuneInfoReply()
        {
            ReplyStatus = MessageReplyStatus.Undetermined,
            ReplyReason = "Not Implemented on Windows"
        };
        return response;
    }

    public DeviceHandlerBase GetDeviceHandler(DeviceInfo deviceInfo)
    {
        return DeviceHandlerBase.GetDeviceHandler(deviceInfo.DeviceType);
    }
}
