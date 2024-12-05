using System.Collections.Generic;

namespace Ahsoka.Services.Device;

internal interface IServiceImplementation
{
    UsbInfo GetUsbInfo();

    void SetUsbInfo(UsbInfo usbInfo);

    void SetRS485Config(SerialRS485 serial_rs485_config);

    TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo updateTouchscreen);
}
