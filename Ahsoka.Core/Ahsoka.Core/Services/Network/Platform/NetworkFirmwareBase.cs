using Ahsoka.System;
using Ahsoka.System.Hardware;
using Renci.SshNet;
using System;

namespace Ahsoka.Services.Network;

/// <summary>
///  Interface for Firmware Updaters
/// </summary>
public interface INetworkFirmwareBase
{
    /// <summary>
    /// Initialize
    /// </summary>
    void Init();

    /// <summary>
    /// Install Local
    /// </summary>
    /// <param name="pathToFirmware"></param>
    void InstallLocal(string pathToFirmware);

    /// <summary>
    /// Install Remove
    /// </summary>
    /// <param name="info"></param>
    /// <param name="sshClient"></param>
    /// <param name="scpClient"></param>
    /// <param name="progress"></param>
    void InstallRemote(HardwareInfo info,
        SshClient sshClient,
        ScpClient scpClient,
        IProgress<string> progress);

    /// <summary>
    /// Get File Data
    /// </summary>
    /// <param name="family"></param>
    /// <returns></returns>
    byte[] GetData(PlatformFamily family);


    /// <summary>
    /// Boot the Network hardware
    /// </summary>
    void Boot();

    /// <summary>
    /// Shutdown the Network Hardware
    /// </summary>
    void Shutdown();
}