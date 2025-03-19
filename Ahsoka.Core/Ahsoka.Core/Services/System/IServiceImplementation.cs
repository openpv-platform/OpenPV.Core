using System.Collections.Generic;

namespace Ahsoka.Services.System;

internal interface IServiceImplementation
{
    string GetHardwareSerialNumber();

    string GetHardwareModel();

    int GetBrightness();
    
    int GetBrightnessMax();

    void SetBrightness(int value);

    void Reboot();

    double GetCPUTemp();

    void SetBootLogo(string logoPath);

    void ShutdownApplication(int shutDownCode);

    Dictionary<string, string> GetPlatformVersions();

}
