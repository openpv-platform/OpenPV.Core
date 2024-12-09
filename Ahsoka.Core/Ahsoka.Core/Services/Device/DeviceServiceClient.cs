using Ahsoka.ServiceFramework;
using Ahsoka.System;

namespace Ahsoka.Services.Device;

/// <summary>
/// Client for talking to System Service
/// </summary>
public class DeviceServiceClient : AhsokaClientBase<DeviceMessageTypes.Ids>
{
    /// <summary>
    /// Constructor using Default Service Configuration
    /// </summary>
    public DeviceServiceClient() :
        this(ConfigurationLoader.GetServiceConfig(DeviceService.Name))
    {

    }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndPoint OnCreateDefaultService()
    {
        return new DeviceService(this.ServiceConfig);
    }

    /// <summary>
    /// Constructor for passing a custom Service Configuration
    /// </summary>
    /// <param name="config">Service Configuration used for Connecting to the Service</param>
    public DeviceServiceClient(ServiceConfiguration config) :
        base(config, new DeviceServiceMessages())
    {

    }


    /// <summary>
    /// Returns the state of the Default USB Controller 
    /// </summary>
    /// <returns>Information about the current Mode (Device / Host) and the Mount Point</returns>
    public UsbInfo GetUsbInfo()
    {
        return SendMessageWithResponse<UsbInfo>(DeviceMessageTypes.Ids.GetUsbInfo);
    }

    /// <summary>
    /// Sets the USB State to the Specified Info. Used for executing a Role swap or changing the default mount point for USB Mass Storage Devices.
    /// </summary>
    /// <param name="usbInfo">Information about the current Mode (Device / Host) and the Mount Point</param>
    public void SetUsbInfo(UsbInfo usbInfo)
    {
        SendMessageWithResponse<EmptyNotification>(DeviceMessageTypes.Ids.SetUsbInfo, usbInfo);
    }

    /// <summary>
    /// Sets the Extended Configuration for the Onboard RS485 Interface
    /// Standard Serial Configurations are completed using the Language Native Serial API's
    /// </summary>
    /// <param name="serialRS485Config">Extended Configration Values to set such as RTS Before / After and any Custom Flags for the Port</param>
    public void SetRS485Config(RS485ConfigNotification serialRS485Config)
    {
        SendMessageWithResponse<EmptyNotification>(DeviceMessageTypes.Ids.SetSerialConfig, serialRS485Config);
    }

    /// <summary>
    /// Update Touch Tune on Primary Screen
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo info)
    {
        return SendMessageWithResponse<TouchTuneInfoReply>(DeviceMessageTypes.Ids.UpdateTouchTune, info);
    }

    /// <summary>
    /// Get List of System Devices which can be Enabled or Disabled
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public DeviceList GetDevices(DeviceInfoRequest info)
    {
        return SendMessageWithResponse<DeviceList>(DeviceMessageTypes.Ids.GetDevices, info);
    }

    /// <summary>
    /// Enable the Specified Device - Uses ID for Identification
    /// </summary>
    /// <param name="info"></param>
    public void EnableDevice(DeviceInfo info)
    {
        SendMessageWithResponse(DeviceMessageTypes.Ids.EnableDevice, info);
    }

    /// <summary>
    /// Enable the Specified Device - Uses ID for Identification
    /// </summary>
    /// <param name="info"></param>
    public void DisableDevice(DeviceInfo info)
    {
        SendMessageWithResponse(DeviceMessageTypes.Ids.DisableDevice, info);
    }
}
