using System.Collections.Generic;

namespace Ahsoka.Services.System;

internal interface IServiceImplementation
{
    string GetHardwareSerialNumber();

    string GetHardwareModel();

    int GetBrightness();

    void SetBrightness(int percent);

    void Reboot();

    double GetCPUTemp();

    void SetBootLogo(string logoPath);

    void ShutdownApplication(int shutDownCode);

    Dictionary<string, string> GetPlatformVersions();
}
