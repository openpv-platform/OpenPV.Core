using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ahsoka.Services.Device;

/// <summary>
///  System Service Metadata Class
/// </summary>
public class DeviceServiceMessages : AhsokaMessagesBase
{
   
    /// <InheritDoc/>
    public DeviceServiceMessages()
    {
        this.RegisterServiceRequest(DeviceMessageTypes.Ids.GetUsbInfo, typeof(EmptyNotification), typeof(UsbInfo));
        this.RegisterServiceRequest(DeviceMessageTypes.Ids.SetUsbInfo, typeof(UsbInfo), typeof(EmptyNotification));

        this.RegisterServiceRequest(DeviceMessageTypes.Ids.SetSerialConfig, typeof(RS485ConfigNotification), typeof(EmptyNotification));
        this.RegisterServiceRequest(DeviceMessageTypes.Ids.UpdateTouchTune, typeof(TouchTuneInfo), typeof(TouchTuneInfoReply));
    }

    /// <InheritDoc/>
    protected override void OnGetParameters(out string group, List<ParameterData> values, PackageInformation info)
    {
        group = DeviceService.Name;

    }

    /// <InheritDoc/>
    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\DeviceService.proto", Properties.Resources.SystemService);

        return result;
    }

}
