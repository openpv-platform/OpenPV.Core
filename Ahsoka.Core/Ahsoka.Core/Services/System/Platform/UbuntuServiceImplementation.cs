using Ahsoka.System;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.System.Platform;

[ExcludeFromCodeCoverage]
internal class UbuntuServiceImplementation : IServiceImplementation
{
    #region Interface
    string IServiceImplementation.GetHardwareModel()
    {
        return "UbuntuTestDevice";
    }

    string IServiceImplementation.GetHardwareSerialNumber()
    {
        return "123456789";
    }

    void IServiceImplementation.SetBrightness(int percent)
    {

    }

    int IServiceImplementation.GetBrightness()
    {
        return 100;
    }

    public UsbInfo GetUsbInfo()
    {
        UsbInfo usbInfo = new()
        {
            MountPoint = ""
        };
        return usbInfo;
    }

    public void SetUsbInfo(UsbInfo usbInfo)
    {

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

    void IServiceImplementation.SetRS485Config(Platform.SerialRS485 serial_rs485_config) { }

    void IServiceImplementation.Reboot()
    {
        LinuxSystemUtilities.Reboot();
    }

    void IServiceImplementation.ShutdownApplication(int shutDownCode)
    {
        Environment.Exit(shutDownCode);
    }

    public void SetRS485Config(SerialRS485 serial_rs485_config)
    {

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
            ReplyReason = "Not Implemented on Ubuntu"
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

    void IServiceImplementation.SetBootLogo(string logoPath)
    {
        // Not Impemented on Windows
    }
    #endregion
}
