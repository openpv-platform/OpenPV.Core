namespace Ahsoka.Core;

/// <summary>
///  Returns Minimum Values Needed for Compatible Firmware
/// </summary>
public class FirmwarePreRequisites
{
    /// <summary>
    /// Minimum Version needed for Compatiblity with OpenPV 
    /// </summary>
    public string MinVersion { get; private set; }

    /// <summary>
    /// Filter Prefix when Accessing Spark Distribution Packages
    /// </summary>
    public string FirmwareFilter { get; private set; }

    /// <summary>
    /// BSP Codename (for Yocto) 
    /// </summary>
    public string RequiredCodeName { get; private set; }

    /// <summary>
    /// Get PreReqs for BSP
    /// </summary>
    /// <param name="family"></param>
    /// <returns></returns>
    public static FirmwarePreRequisites GetBSPInfo(PlatformFamily family)
    {
        return family switch
        {
            PlatformFamily.OpenViewLinux => new FirmwarePreRequisites()
            {
                FirmwareFilter = "2.",
                MinVersion = "2.2023-12-05T125138.kirkstone.43cacf3",
                RequiredCodeName = "kirkstone"
            },
            PlatformFamily.OpenViewLinuxPro => new FirmwarePreRequisites()
            {
                FirmwareFilter = "2.",
                MinVersion = "2099-0.0",
                RequiredCodeName = "unknown"
            },
            _ => new FirmwarePreRequisites()
            {
                FirmwareFilter = "2.",
                MinVersion = "2099-12-31",
                RequiredCodeName = "OpenViewSTest"
            },
        };
    }
}


