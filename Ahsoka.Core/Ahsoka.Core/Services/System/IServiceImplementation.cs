using System.Collections.Generic;

namespace Ahsoka.Services.System;

internal interface IServiceImplementation
{
    string GetHardwareSerialNumber();

    string GetHardwareModel();

    int GetBrightness();

    void SetBrightness(int percent);

    UsbInfo GetUsbInfo();

    void SetUsbInfo(UsbInfo usbInfo);

    void SetRS485Config(Platform.SerialRS485 serial_rs485_config);

    void Reboot();

    TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo updateTouchscreen);

    double GetCPUTemp();

    void SetBootLogo(string logoPath);

    void ShutdownApplication(int shutDownCode);

    Dictionary<string, string> GetPlatformVersions();
}
