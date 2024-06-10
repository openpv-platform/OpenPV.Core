using Ahsoka.ServiceFramework;

namespace Ahsoka.Services.System;

/// <summary>
/// List of Key Values Available in this Service
/// </summary>
public class SystemServiceDataKeys : KnownDataKeys
{
    /// <summary>
    /// The name of the current Application thats running
    /// </summary>
    public const string CurrentApplicationName = "ApplicationName";

    /// <summary>
    /// Is the Service Available
    /// </summary>
    public const string Available = "Available";

    /// <summary>
    /// Application Version
    /// </summary>
    public const string ApplicationVersion = "ApplicationVersion";

    /// <summary>
    /// Application Version
    /// </summary>
    public const string ApplicationDescription = "ApplicationDescription";

    /// <summary>
    /// SDK Version
    /// </summary>
    public const string SDKVersion = "SDKVersion";

    /// <summary>
    /// Installer Version
    /// </summary>
    public const string InstallerVersion = "InstallerVersion";

    /// <summary>
    /// BSP Version
    /// </summary>
    public const string BSPVersion = "BSPVersion";

    /// <summary>
    /// Touch Produce ID For Touch Screen Displays
    /// </summary>
    public const string TouchProductID = "TouchProductID";

    /// <summary>
    /// Touch Vender ID For Touch Screen Displays
    /// </summary>
    public const string TouchVendorID = "TouchVendorID";

    /// <summary>
    /// Touch CRC For Touch Screen Displays
    /// </summary>
    public const string TouchConfigCRC = "TouchConfigCRC";

    /// <summary>
    /// Touch Harware Version For Touch Screen Displays
    /// </summary>
    public const string TouchHardwareVersion = "TouchHardwareVersion";

    /// <summary>
    /// Touch Firmware Version For Touch Screen Displays
    /// </summary>
    public const string TouchFirmwareVersion = "TouchFirmwareVersion";

    /// <summary>
    /// Touch Factory Installer Version
    /// </summary>
    public const string InstallerVersionFactory = "InstallerVersionFactory";

    /// <summary>
    /// Touch Factory Installer Version
    /// </summary>
    public const string CurrentIgnitionState = "IgnitionState";
}
