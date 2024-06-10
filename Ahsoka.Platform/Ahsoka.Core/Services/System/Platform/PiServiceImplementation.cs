using Ahsoka.System;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.System.Platform;

[ExcludeFromCodeCoverage]
internal class PiServiceImplementation : IServiceImplementation
{
    public string GetHardwareModel()
    {
        return string.Empty;
    }

    public string GetHardwareSerialNumber()
    {
        return string.Empty;
    }

    public void SetBrightness(int percent)
    {

    }

    public int GetBrightness()
    {
        return 100;
    }

    public BuzzerConfig GetBuzzerConfig()
    {
        // Not Implemented
        return new BuzzerConfig()
        {
            IsEnabled = false,
            FrequencyInHz = 1000,
            VolumePct = 50
        };
    }

    public void SetBuzzerConfig(BuzzerConfig buzzerConfig)
    {

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

    void IServiceImplementation.SetRS485Config(Platform.SerialRS485 serialRS485Config) { }

    void IServiceImplementation.Reboot()
    {
        LinuxSystemUtilities.Reboot();
    }

    void IServiceImplementation.ShutdownApplication(int shutDownCode)
    {
        Environment.Exit(shutDownCode);
    }


    public VoltageValue GetVBat()
    {
        // *****Not Implemented*****
        VoltageValue response = new()
        {
            MilliVolts = 0
        };
        return response;
    }

    public IgnitionState GetIGNPin()
    {
        // *****Not Implemented*****
        IgnitionState response = new()
        {
            MilliVolts = 0,
            State = IgnitionStates.On
        };
        return response;
    }

    public TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo updateTouchscreen)
    {
        var response = new TouchTuneInfoReply()
        {
            ReplyStatus = MessageReplyStatus.Undetermined,
            ReplyReason = "Not Implemented on Pi"
        };
        return response;
    }

    public double GetCPUTemp()
    {
        // NOTE: This means that this is not implemented
        return double.MinValue;
    }

    public Dictionary<string, string> GetPlatformVersions()
    {
        return new Dictionary<string, string>();
    }

    public void SetBootLogo(string logoPath)
    {
        // Not Impemented on Windows
    }
}
