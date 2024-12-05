using Ahsoka.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Ahsoka.Services.Device;

[ExcludeFromCodeCoverage]
internal class DesktopServiceImplementation : IServiceImplementation
{
    UsbInfo usbInfo = new UsbInfo() { Mode = UsbMode.Host };

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

    void IServiceImplementation.SetRS485Config(SerialRS485 serial_rs485_config) { }

    public TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo updateTouchscreen)
    {
        var response = new TouchTuneInfoReply()
        {
            ReplyStatus = MessageReplyStatus.Undetermined,
            ReplyReason = "Not Implemented on Windows"
        };
        return response;
    }    
}
