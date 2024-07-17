using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using System.Collections.Generic;

namespace Ahsoka.Services.System;

/// <summary>
/// List of Key Values Available in this Service
/// </summary>
public class SystemServiceParameters : ServiceParametersBase
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


    /// <InheritDoc/>
    protected override void OnGetParameters(out string group, List<ParameterData> values, PackageInformation info)
    {
        group = SystemService.Name;

        values.Add(new() { Name = Available, DefaultValue = false, ValueType = ParameterValueTypes.Boolean });
        values.Add(new() { Name = CurrentApplicationName, ValueType = ParameterValueTypes.String });
        values.Add(new() { Name = ApplicationVersion, ValueType = ParameterValueTypes.String });
        values.Add(new() { Name = ApplicationDescription, ValueType = ParameterValueTypes.String });
        values.Add(new() { Name = SDKVersion, ValueType = ParameterValueTypes.String });
        values.Add(new() { Name = InstallerVersion, ValueType = ParameterValueTypes.String });
        values.Add(new() { Name = BSPVersion, ValueType = ParameterValueTypes.String });
        values.Add(new() { Name = TouchProductID, ValueType = ParameterValueTypes.String });
        values.Add(new() { Name = TouchVendorID, ValueType = ParameterValueTypes.String });
        values.Add(new() { Name = TouchConfigCRC, ValueType = ParameterValueTypes.String });
        values.Add(new() { Name = TouchHardwareVersion, ValueType = ParameterValueTypes.String });
        values.Add(new() { Name = TouchFirmwareVersion, ValueType = ParameterValueTypes.String });
    }
}
