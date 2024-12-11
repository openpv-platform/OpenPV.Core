using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using Usb.Events;

namespace Ahsoka.Services.Device;

/// <summary>
/// Device Service manages USB and Native Device Power Modes
/// </summary>
[AhsokaService(Name)]
public class DeviceService : AhsokaServiceBase<DeviceMessageTypes.Ids>
{
    #region Fields

    IUsbEventWatcher usbEventWatcher;
    internal const string Name = "DeviceService";
    readonly IDeviceServiceImplementation coreInterop;
    Dictionary<uint,DeviceState> devices = new();
    #endregion

    #region Methods
    internal static IDeviceServiceImplementation GetInterop()
    {
        return SystemInfo.CurrentPlatform switch
        {
            PlatformFamily.Windows64 => new DesktopServiceImplementation(),
            PlatformFamily.Ubuntu64 => new DesktopServiceImplementation(),
            PlatformFamily.OpenViewLinux => new STServiceImplementation(),
            PlatformFamily.MacosArm64 => new DesktopServiceImplementation(),
            PlatformFamily.OpenViewLinuxPro => new TIServiceImplementation(),
            _ => throw new PlatformNotSupportedException(),
        };
    }

    /// <summary>
    /// Default CTOR
    /// </summary>
    public DeviceService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {
   
    }

 

    /// <summary>
    /// CTOR Accepting Custom Configuration
    /// </summary>
    /// <param name="config"></param>
    public DeviceService(ServiceConfiguration config) :
        base(config, new DeviceServiceMessages())
    {
        using var stopwatch = new AhsokaStopwatch("Create DeviceService");
        coreInterop = GetInterop();
        coreInterop.Init(SendNotification);
        stopwatch.ResetTiming("Init Completed");

        usbEventWatcher = new UsbEventWatcher(false, true, false, false);
        usbEventWatcher.UsbDeviceRemoved += (_, device) => UsbRemoved(device); 
        usbEventWatcher.UsbDeviceAdded += (_, device) => UsbAdded(device); 

    }

    /// <summary>
    /// Default Handler Method for Service Requests
    /// </summary>
    /// <param name="request"></param>
    protected override void OnHandleServiceRequest(AhsokaServiceRequest request)
    {
        switch (request.TransportId)
        {
            case DeviceMessageTypes.Ids.GetUsbInfo:
                HandleGetUsbInfo(request);
                break;

            case DeviceMessageTypes.Ids.SetUsbInfo:
                HandleSetUsbInfo(request);
                break;

            case DeviceMessageTypes.Ids.SetSerialConfig:
                HandleSetRS485Config(request);
                break;

            case DeviceMessageTypes.Ids.UpdateTouchTune:
                HandleUpdateTouchscreen(request);
                break;

            case DeviceMessageTypes.Ids.GetDevices:
                HandleGetDevices(request);
                break;

            case DeviceMessageTypes.Ids.EnableDevice:
                HandleDeviceEnabled(request);
                break;

            case DeviceMessageTypes.Ids.DisableDevice:
                HandleDeviceDisabled(request);
                break;
        }
    }

    private void HandleGetDevices(AhsokaServiceRequest request)
    {
        var result = coreInterop.HandleGetDevices(request.Message as DeviceInfoRequest);
        foreach (var device in result.Devices)
            if (devices.TryGetValue(device.Id, out var state))
                device.State = state;

        SendResponse(request, result);
    }

    private void HandleDeviceEnabled(AhsokaServiceRequest request)
    {
       
        var deviceInfo = request.Message as DeviceInfo;
        var handler =  coreInterop.GetDeviceHandler(deviceInfo);
        handler.EnableDevice(deviceInfo, this.SendNotification);
        devices[deviceInfo.Id] = DeviceState.Enabled;
        SendResponse(request);
    }

    private void HandleDeviceDisabled(AhsokaServiceRequest request)
    {
        var deviceInfo = request.Message as DeviceInfo;
        var handler = coreInterop.GetDeviceHandler(deviceInfo);
        handler.DisableDevice(deviceInfo, this.SendNotification);
        devices[deviceInfo.Id] = DeviceState.Disabled;
        SendResponse(request);
    }

    private void HandleGetUsbInfo(AhsokaServiceRequest message)
    {
        SendResponse(message, coreInterop.GetUsbInfo());
    }

    private void HandleSetUsbInfo(AhsokaServiceRequest message)
    {
        coreInterop.SetUsbInfo(message.Message as UsbInfo);
        SendResponse(message);
    }

    private void HandleSetRS485Config(AhsokaServiceRequest message)
    {
        coreInterop.SetRS485Config(message.Message as RS485ConfigNotification);
        SendResponse(message);
    }
    
    private void HandleUpdateTouchscreen(AhsokaServiceRequest message)
    {
        var response = coreInterop.UpdateTouchTune(message.Message as TouchTuneInfo);
        SendResponse(message, response);
    }


    private void UsbAdded(UsbDevice device)
    {
        SendNotification(DeviceMessageTypes.Ids.UsbDeviceAdded, new DeviceInfo());
    }

    private void UsbRemoved(UsbDevice device)
    {
        SendNotification(DeviceMessageTypes.Ids.UsbDeviceRemoved, new DeviceInfo());
    }
    #endregion
}
