using Ahsoka.Services.System;
using Ahsoka.Spark;
using Ahsoka.System.Hardware;
using Renci.SshNet;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.System;

/// <summary>
/// Interface for Tools that interact with a Remote Target to support various system operations.
/// </summary>
public interface IRemoteTargetTool
{
    /// <summary>
    /// Default value for RDP Ports Forwarding.
    /// </summary>
    const int DefaultRdpPort = 5999;

    /// <summary>
    /// Cancel the currently running process
    /// </summary>
    void Cancel();

    /// <summary>
    /// Clear the Journal / Logs for the connected target
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    string ClearJournal(IConnectionInfo info);

    /// <summary>
    ///  Get the Journal / Log for any Customer Application
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    string GetApplicationJournal(IConnectionInfo info);

    /// <summary>
    /// Get the Hardware ID from the Connected Target
    /// </summary>
    /// <param name="info"></param>
    /// <param name="sshClient"></param>
    /// <returns></returns>
    string GetHardwareId(HardwareInfo info, SshClient sshClient);

    /// <summary>
    /// Get the Journal / Log for the Installer
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    string GetInstallerJournal(ITargetConnectionInfo info);

    /// <summary>
    /// Execute the Ahsoka Prep Process for the Connected Target
    /// </summary>
    /// <param name="connectionInfo"></param>
    /// <param name="sparkInfo"></param>
    /// <param name="progress"></param>
    /// <param name="manufacturingInfo"></param>
    /// <returns></returns>
    bool Prep(ITargetConnectionInfo connectionInfo, SparkConnectionInfo sparkInfo, IProgress<string> progress, ManufacturingInfo manufacturingInfo = null);

    /// <summary>
    /// Reboot the Connected Target
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    string Reboot(IConnectionInfo info);

    /// <summary>
    /// Exit the application and return to the Installer on the connected target.
    /// </summary>
    /// <param name="connectionInfo"></param>
    /// <param name="client"></param>
    /// <param name="source"></param>
    /// <param name="progressString"></param>
    /// <returns></returns>
    GetHardwareInformationReply ReturnToInstaller(TargetConnectionInfo connectionInfo, SystemServiceClient client, CancellationTokenSource source, IProgress<string> progressString);

    /// <summary>
    /// Send a Stop Command for the connected Target.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="isInstaller"></param>
    /// <returns></returns>
    string StopApplication(IConnectionInfo info, bool isInstaller);

    /// <summary>
    /// Enable Remote Desktop Services
    /// </summary>
    /// <param name="hardwareInfo"></param>
    /// <param name="info"></param>
    /// <param name="rdpPort"></param>
    /// <param name="progressString"></param>
    /// <returns></returns>
    bool EnableRDP(HardwareInfo hardwareInfo, IConnectionInfo info, int rdpPort, IProgress<string> progressString);

    /// <summary>
    /// Disable Remote Desktop Services on this target.
    /// </summary>
    /// <param name="hardwareInfo"></param>
    /// <param name="info"></param>
    /// <param name="progressString"></param>
    /// <returns></returns>
    bool DisableRDP(HardwareInfo hardwareInfo, IConnectionInfo info, IProgress<string> progressString);

    /// <summary>
    /// Starts a Tunnel over SSH and Launches RDP.  If a task is passed in the process will wait for it to end and end the session when complete
    /// </summary>
    /// <param name="info"></param>
    /// <param name="rdpPort"></param>
    /// <param name="waitForTask"></param>
    void RunRDPSession(IConnectionInfo info, uint rdpPort, Task<bool> waitForTask);
}