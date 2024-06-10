using Ahsoka.System;
using Ahsoka.System.Hardware;
using Renci.SshNet;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ahsoka.Core.Bluetooth.LE")]
[assembly: InternalsVisibleTo("Ahsoka.Core.Bluetooth")]

namespace Ahsoka.Services.Network;


internal interface INetworkFirmwareBase
{
    void Init();

    void InstallLocal(string pathToFirmware);

    void InstallRemote(HardwareInfo info,
        SshClient sshClient,
        ScpClient scpClient,
        IProgress<string> progress);

    byte[] GetData(PlatformFamily family);

    void Boot();

    void Shutdown();
}