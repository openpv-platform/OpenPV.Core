using Ahsoka.System;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Ahsoka.Services.Device;

[ExcludeFromCodeCoverage]
internal class TIServiceImplementation : DeviceServiceImplementation
{
    #region Fields
    readonly string[] logoFileTypes = { ".png", ".jpg" };

    private const string firmwareModelNumberPath = "/sys/firmware/devicetree/base/model";
    private const string firmwareSerialNumberPath = "/sys/block/mmcblk0/device/cid";
   
    private const string SerialPortName = "/dev/ttySTM1\0";
    #endregion

    #region Interface
    internal override void OnInitConfiguration(Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification, HardwareConfiguration deviceList, bool isInstaller)
    {

    }

    internal override void SetUsbRoleMode(UsbPort port, UsbMode mode)
    {
        
    }

    internal override  UsbInfo GetUsbInfo()
    {
        UsbInfo usbInfo = new()
        {
            MountPoint = ""
        };

        return usbInfo;
    }

    internal override void SetUsbInfo(UsbInfo usbInfo)
    {
        if (usbInfo.Mode == UsbMode.Host)
        {
           
        }
        else if (usbInfo.Mode == UsbMode.Device)
        {
           
        }
    }

    internal override void SetRS485Config(RS485ConfigNotification serial_rs485_config)
    {
        
    }

    internal override TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo updateTouchscreen)
    {
        var response = new TouchTuneInfoReply()
        {
            ReplyStatus = MessageReplyStatus.Undetermined,
            ReplyReason = "Not Implemented"
        };
      
        return response;
    }


    public double GetCPUTemp()
    {
        string path = "/sys/devices/virtual/thermal/thermal_zone0/temp";
        if (File.Exists(path))
        {
            var value = int.Parse(File.ReadAllText(path));
            return (double)value / 1000;
        }

        return 0;
    }


    #endregion
}
