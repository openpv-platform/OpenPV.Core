using System;
using Ahsoka.Services.Device.Handlers;

namespace Ahsoka.Services.Device;

internal interface IDeviceServiceImplementation
{
    UsbInfo GetUsbInfo();

    void SetUsbInfo(UsbInfo usbInfo);

    void SetRS485Config(RS485ConfigNotification serial_rs485_config);

    TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo updateTouchscreen);

    DeviceList HandleGetDevices(DeviceInfoRequest deviceInfoRequest);

    DeviceHandlerBase GetDeviceHandler(DeviceInfo deviceInfo)
    {
        return DeviceHandlerBase.GetDeviceHandler(deviceInfo.DeviceType);
    }

    void Init(Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification);
}
