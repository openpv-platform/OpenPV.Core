namespace Ahsoka.Services.Install;

/// <summary>
/// Interface used to allow implementations of an Installer UI to communicate with the 
/// installer application during execution.  This application runs in its own process
/// and connects to the UX via our NetMQ based Communication system allowing installers
/// to be located in a different process or even monitored from a different target or development
/// PC.
/// </summary>
public interface IInstallerUI
{
    /// <summary>
    /// Communicates the status of the install including messages and progress percentage
    /// </summary>
    /// <param name="info">Percent Complete, Status Message and Overall Status included in the info object.</param>
    void InstallProgress(InstallProgressInfo info);

    /// <summary>
    /// Logged information coming from the install process.  Not intended to be used by the UX but 
    /// may be used for diagnostics.
    /// </summary>
    /// <param name="info">Information to be logged that occurs during the install process</param>
    void Log(InstallLogInfo info);

    /// <summary>
    /// Notification that the install is completed and the final status.
    /// </summary>
    /// <param name="info">Information about the final result of the install</param>
    void InstallComplete(InstallCompleteInfo info);
}
