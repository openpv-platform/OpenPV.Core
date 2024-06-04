using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ahsoka.Services.System;

internal class SystemServiceMessages : AhsokaMessagesBase
{
    public SystemServiceMessages()
    {
        this.RegisterServiceRequest(SystemMessageTypes.Ids.Echo, typeof(EchoValue), typeof(EchoValue));
        this.RegisterServiceRequest(SystemMessageTypes.Ids.GetServiceInfo, typeof(EmptyNotification), typeof(ServiceInfo));
        this.RegisterServiceRequest(SystemMessageTypes.Ids.GetHardwareInformation, typeof(EmptyNotification), typeof(GetHardwareInformationReply));

        this.RegisterServiceRequest(SystemMessageTypes.Ids.LoadApplication, typeof(LoadApplication), typeof(LoadApplicationReply), false, true);
        this.RegisterServiceRequest(SystemMessageTypes.Ids.LoadApplicationData, typeof(LoadApplicationData), typeof(LoadApplicationDataReply), false, true);
        this.RegisterServiceRequest(SystemMessageTypes.Ids.LoadApplicationCancelled, typeof(LoadApplicationCancelled), typeof(LoadApplicationCancelled), false, true);

        this.RegisterServiceRequest(SystemMessageTypes.Ids.GetBrightness, typeof(EmptyNotification), typeof(BrightnessInfo));
        this.RegisterServiceRequest(SystemMessageTypes.Ids.SetBrightness, typeof(BrightnessInfo), typeof(BrightnessInfo));
        this.RegisterServiceRequest(SystemMessageTypes.Ids.GetBuzzerConfig, typeof(EmptyNotification), typeof(BuzzerConfig));
        this.RegisterServiceRequest(SystemMessageTypes.Ids.SetBuzzerConfig, typeof(BuzzerConfig), typeof(EmptyNotification));
        this.RegisterServiceRequest(SystemMessageTypes.Ids.GetUsbInfo, typeof(EmptyNotification), typeof(UsbInfo));
        this.RegisterServiceRequest(SystemMessageTypes.Ids.SetUsbInfo, typeof(UsbInfo), typeof(EmptyNotification));

        this.RegisterServiceRequest(SystemMessageTypes.Ids.SetSerialConfig, typeof(RS485ConfigNotification), typeof(EmptyNotification));

        this.RegisterServiceRequest(SystemMessageTypes.Ids.GetBatteryVoltage, typeof(EmptyNotification), typeof(VoltageValue));
        this.RegisterServiceRequest(SystemMessageTypes.Ids.GetIgnitionPin, typeof(EmptyNotification), typeof(IgnitionState));

        this.RegisterServiceRequest(SystemMessageTypes.Ids.SetBootLogo, typeof(BootLogoInfo), typeof(EmptyNotification));
        this.RegisterServiceRequest(SystemMessageTypes.Ids.UpdateTouchTune, typeof(TouchTuneInfo), typeof(TouchTuneInfoReply));

        this.RegisterServiceRequest(SystemMessageTypes.Ids.RequestShutdown, typeof(ShutdownInformation), typeof(EmptyNotification));

        this.RegisterServiceNotification(SystemMessageTypes.Ids.IgnitionOffNotification, typeof(EmptyNotification));
        this.RegisterServiceNotification(SystemMessageTypes.Ids.IgnitionOnNotification, typeof(EmptyNotification));
    }

    public override Dictionary<string, byte[]> GetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.GetAdditionalClientResources(type);
        result.Add("SystemServiceClientExtensions.h", Properties.Resources.SystemServiceClientExtensionsH);

        if (includeImplementation)
            result.Add("SystemServiceClientExtensions.cxx", Properties.Resources.SystemServiceClientExtensionsCXX);

        return result;
    }

    public override KnownDataKeys GetKnownValues()
    {
        return new SystemServiceDataKeys();
    }
}

/// <summary>
/// Message used for loading an application.
/// </summary>
public partial class LoadApplication : IFileTransmit
{

}

/// <summary>
/// Message Response for Load Applications
/// </summary>
public partial class LoadApplicationReply : IFileTransmitReply
{

}

/// <summary>
/// Message for Carrying Data for an Application Load.
/// </summary>
public partial class LoadApplicationData : IFileTransmitData
{

}

/// <summary>
/// Message Reply for Application Data Messages
/// </summary>
public partial class LoadApplicationDataReply : IFileTransmitDataReply
{

}

/// <summary>
///  Message sent to Cancel an Application Load
/// </summary>
public partial class LoadApplicationCancelled : IFileTransmitCancelled
{

}

/// <summary>
/// Event Args raised when an Application Load is Cancelled.
/// </summary>
public partial class LoadApplicationCompleteEventArgs : EventArgs
{

}