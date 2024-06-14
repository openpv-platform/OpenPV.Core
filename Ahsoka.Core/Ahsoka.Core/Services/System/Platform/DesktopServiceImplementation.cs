using Ahsoka.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.System.Platform;

[ExcludeFromCodeCoverage]
internal class DesktopServiceImplementation : IServiceImplementation
{
    public string GetHardwareSerialNumber()
    {
        return GetWMICValue("bios get serialnumber");
    }

    public string GetHardwareModel()
    {
        return GetWMICValue("csproduct get name");
    }

    public void SetBrightness(int percent)
    {

    }

    public int GetBrightness()
    {
        return 100;
    }

    public UsbInfo GetUsbInfo()
    {
        // Not Implemented
        UsbInfo usbInfo = new()
        {
            Mode = UsbMode.Unknown,
            MountPoint = ""
        };
        return usbInfo;
    }

    public void SetUsbInfo(UsbInfo usbInfo)
    {
        // Not Implemented
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

    void IServiceImplementation.SetRS485Config(Platform.SerialRS485 serial_rs485_config) { }

    void IServiceImplementation.Reboot()
    {
        // Not Impemented on Windows
    }

    void IServiceImplementation.ShutdownApplication(int shutDownCode)
    {
        Environment.Exit(shutDownCode);
    }

    public TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo updateTouchscreen)
    {
        var response = new TouchTuneInfoReply()
        {
            ReplyStatus = MessageReplyStatus.Undetermined,
            ReplyReason = "Not Implemented on Windows"
        };
        return response;
    }

    public double GetCPUTemp()
    {
        // NOTE: This means that this is not implemented
        return 0.0;
    }

    public Dictionary<string, string> GetPlatformVersions()
    {
        return new Dictionary<string, string>();
    }

    void IServiceImplementation.SetBootLogo(string logoPath)
    {
        // Not Impemented on Windows
    }
}
