#pragma warning disable CS1591
using Ahsoka.Installer;
using Ahsoka.Installer.Components;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Ahsoka.Services.Device;

[AhsokaService(Name)]
public class DeviceService : AhsokaServiceBase<DeviceMessageTypes.Ids>
{
    #region Fields
    public const string Name = "DeviceService";
    readonly IServiceImplementation coreInterop;
    #endregion

    #region Methods
    internal static IServiceImplementation GetInterop()
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

    public DeviceService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {
  
    }

    public DeviceService(ServiceConfiguration config) :
        base(config, new DeviceServiceMessages())
    {
        using var stopwatch = new AhsokaStopwatch("Create SystemService");

        stopwatch.ResetTiming("Interop Found");

        coreInterop = GetInterop();
        stopwatch.ResetTiming("Settings Loaded");
        stopwatch.ResetTiming("Hardware Info Loaded");

        InitValues();

    }

    private void InitValues()
    {
       
    }


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

        }
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
        var config = message.Message as RS485ConfigNotification;

        // We are converting the protobuf structure back to it's base type to set the config locally.
        var serial_rs485_config = new SerialRS485()
        {
            flags = config.Flags,
            delay_rts_after_send = config.DelayRtsAfterSend,
            delay_rts_before_send = config.DelayRtsBeforeSend
        };
        coreInterop.SetRS485Config(serial_rs485_config);

        SendResponse(message);
    }
    
    private void HandleUpdateTouchscreen(AhsokaServiceRequest message)
    {
        var response = coreInterop.UpdateTouchTune(message.Message as TouchTuneInfo);
        SendResponse(message, response);
    }

    #endregion
}
