using Ahsoka.Core;
using Ahsoka.Installer;
using System.Collections.Generic;

namespace Ahsoka.Services.System;

/// <summary>
///  System Service Metadata Class
/// </summary>
public class SystemServiceMessages : AhsokaMessagesBase
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
    public SystemServiceMessages() : base(SystemService.Name)
    {
        this.RegisterServiceRequest(SystemMessageTypes.Ids.Echo, typeof(EchoValue), typeof(EchoValue));
        this.RegisterServiceRequest(SystemMessageTypes.Ids.GetServiceInfo, typeof(EmptyNotification), typeof(ServiceInfo));
        this.RegisterServiceRequest(SystemMessageTypes.Ids.GetHardwareInformation, typeof(EmptyNotification), typeof(GetHardwareInformationReply));

        this.RegisterServiceRequest(SystemMessageTypes.Ids.LoadApplication, typeof(LoadApplication), typeof(LoadApplicationReply), false, true);
        this.RegisterServiceRequest(SystemMessageTypes.Ids.LoadApplicationData, typeof(LoadApplicationData), typeof(LoadApplicationDataReply), false, true);
        this.RegisterServiceRequest(SystemMessageTypes.Ids.LoadApplicationCancelled, typeof(LoadApplicationCancelled), typeof(LoadApplicationCancelled), false, true);

        this.RegisterServiceRequest(SystemMessageTypes.Ids.GetBrightness, typeof(EmptyNotification), typeof(BrightnessInfo));
        this.RegisterServiceRequest(SystemMessageTypes.Ids.SetBrightness, typeof(BrightnessInfo), typeof(BrightnessInfo));
        this.RegisterServiceRequest(SystemMessageTypes.Ids.SetBootLogo, typeof(BootLogoInfo), typeof(EmptyNotification));

        this.RegisterServiceRequest(SystemMessageTypes.Ids.RequestShutdown, typeof(ShutdownInformation), typeof(EmptyNotification));

        // Test Calls
        this.RegisterServiceRequest(SystemMessageTypes.Ids.RequestNotif, typeof(EchoValue), typeof(EmptyNotification), false, true);
        this.RegisterServiceNotification(SystemMessageTypes.Ids.TestNotif, typeof(EchoValue), false, true);
    }

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

    /// <InheritDoc/>
    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("inc\\services\\SystemServiceClientExtensions.h", Properties.Resources.SystemServiceClientExtensionsH);

        result.Add("Ahsoka.Proto\\SystemService.proto", GetProtoMessageFile());

        if (includeImplementation)
            result.Add("inc\\services\\SystemServiceClientExtensions.cxx", Properties.Resources.SystemServiceClientExtensionsCXX);

        return result;
    }

    /// <InheritDoc/>
    public override byte[] GetProtoMessageFile()
    {
        return this.GenerateProtoFile("AhsokaSystem", typeof(SystemMessageTypes.Ids));
    }

}
