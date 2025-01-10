using Ahsoka.Services.Device.Handlers;
using Ahsoka.System;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ahsoka.Services.Device;

[ExcludeFromCodeCoverage]
internal class DesktopServiceImplementation : DeviceServiceImplementation
{
    UsbInfo usbInfo = new UsbInfo() { Mode = UsbMode.Host };
    private Action<DeviceMessageTypes.Ids, object, byte[]> sendNotificationHandler;

    internal override void OnInit(Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification, DeviceList list)
    {
        sendNotificationHandler = sendNotification;
        if (list.Devices.Count == 0)
            list.Devices.Add(new DeviceInfo() { DeviceType = DeviceTypes.OnboardWifiModule, Id = 1234, State = DeviceState.Ready });
    }


    internal override UsbInfo GetUsbInfo()
    {
        // Not Implemented
        return usbInfo;
    }

    internal override void SetUsbInfo(UsbInfo usbInfo)
    {
        this.usbInfo = usbInfo;
    }

    internal override void SetUsbRoleMode(UsbMode mode)
    {

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

    internal override void SetRS485Config(RS485ConfigNotification serial_rs485_config) { }

    internal override TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo updateTouchscreen)
    {
        var response = new TouchTuneInfoReply()
        {
            ReplyStatus = MessageReplyStatus.Undetermined,
            ReplyReason = "Not Implemented on Windows"
        };
        return response;
    }

}
