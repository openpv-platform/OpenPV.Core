using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device.Handlers;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Usb.Events;
using static NetMQ.NetMQSelector;
using static Org.BouncyCastle.Math.EC.ECCurve;
using Globalization = System.Globalization.NumberStyles;

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
    readonly DeviceServiceImplementation coreInterop;
    Dictionary<uint,DeviceState> devices = new();
    bool notificationsEnabled = false;
    Dictionary<string,UsbDeviceId> activeDevices = new ();
    #endregion

    #region Methods
    internal static DeviceServiceImplementation GetInterop()
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
        coreInterop.Init(SendNotificationIfEnabled);
        stopwatch.ResetTiming("Init Completed");
    }

    private void SendNotificationIfEnabled(DeviceMessageTypes.Ids transportId, Object message, byte[] specificClient = null)
    {
        if (notificationsEnabled)
            SendNotification(transportId, message, specificClient);
    }

    /// <summary>
    /// Default Handler Method for Service Requests
    /// </summary>
    /// <param name="request"></param>
    protected override void OnHandleServiceRequest(AhsokaServiceRequest request)
    {
        lock (devices)
        {
            switch (request.TransportId)
            {
                case DeviceMessageTypes.Ids.EnableDeviceNotifications:
                    HandleEnableNotifications(request);
                    break;

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
    }

    private void HandleEnableNotifications(AhsokaServiceRequest request)
    {
        if (!notificationsEnabled && !SystemInfo.IsDesktop())
        {
            usbEventWatcher = new UsbEventWatcher(true, true, false, false);
            usbEventWatcher.UsbDeviceRemoved += (_, device) => UsbRemoved(device);
            usbEventWatcher.UsbDeviceAdded += (_, device) => UsbAdded(device);
        }

        notificationsEnabled = true;

        // Send Notifications to Clients for Existing Onboard Devices
        foreach (var item in coreInterop.GetDevices().Devices)
            if (item.State != DeviceState.NoState)
            {
                if (item.DeviceType.ToString().ToLower().Contains("onboard")) // USB Devices will come from the USB Watcher
                    SendNotification(DeviceMessageTypes.Ids.OnboardDeviceReady, item, GetClientNotificationId(request.Header.ClientId));

                // Execute Auto Enable
                if (item.State != DeviceState.Enabled && item.AutoEnable)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Auto-Enabling  {item.Name}");
                    coreInterop.GetDeviceHandler(item).EnableDevice(item, SendNotificationIfEnabled);
                }
                else if (item.AutoEnable) // Already Enabled
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Device {item.Name} Already Enabled...sending Enabled Message");
                    SendNotificationIfEnabled(DeviceMessageTypes.Ids.DeviceEnabled, item, null);
                }
            }

        SendResponse(request, new EmptyNotification());
    }

    private void HandleGetDevices(AhsokaServiceRequest request)
    {
        var requestInfo = request.Message as DeviceInfoRequest;
        var result = coreInterop.GetDevices();

        DeviceList list = new DeviceList();
        foreach (var item in result.Devices)
        {
            var services = DeviceInfo.GetSupportedServices(item.DeviceType);
            if (requestInfo.FeatureSearch == SupportedFeatures.FeaturesUnknown || services.Contains(requestInfo.FeatureSearch))
                list.Devices.Add(item);
        }

        foreach (var device in result.Devices)
            if (devices.TryGetValue(device.Id, out var state))
                device.State = state;

        SendResponse(request, list);
    }

    private void HandleDeviceEnabled(AhsokaServiceRequest request)
    {
        var deviceInfo = request.Message as DeviceInfo;
        var handler =  coreInterop.GetDeviceHandler(deviceInfo);
        handler.EnableDevice(deviceInfo, this.SendNotificationIfEnabled);
        devices[deviceInfo.Id] = DeviceState.Enabled;
        SendResponse(request);
    }

    private void HandleDeviceDisabled(AhsokaServiceRequest request)
    {
        var deviceInfo = request.Message as DeviceInfo;
        var handler = coreInterop.GetDeviceHandler(deviceInfo);
        handler.DisableDevice(deviceInfo, this.SendNotificationIfEnabled);
        devices[deviceInfo.Id] = DeviceState.Disabled;
        SendResponse(request);
    }

    private void HandleGetUsbInfo(AhsokaServiceRequest message)
    {
        var usbInfo = coreInterop.GetUsbInfo();
        SendResponse(message, usbInfo);
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
        ushort.TryParse(device.ProductID, Globalization.HexNumber, null, out ushort pid);
        ushort.TryParse(device.VendorID, Globalization.HexNumber, null, out ushort vid);
       
        var usbDevice = new UsbDeviceId() { ProductId = pid, VendorId = vid };
        var deviceInfo = coreInterop.GetUsbDevice(usbDevice);
        if (deviceInfo != null)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Device Added VendorId: {vid} Product: {pid} - {device.DeviceName}");

            activeDevices[device.DeviceName] = usbDevice;
            SendNotificationIfEnabled(DeviceMessageTypes.Ids.UsbDeviceAdded, new UsbDeviceInfo() { ProductId = (ushort)pid, VendorId = (ushort)vid, DevicePath = device.DeviceName, DeviceInfo = deviceInfo });

            if (deviceInfo.AutoEnable)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Auto-Enabling  {deviceInfo.Name}");

                var handler = coreInterop.GetDeviceHandler(deviceInfo);
                handler.EnableDevice(deviceInfo, this.SendNotificationIfEnabled);
                lock(devices)
                    devices[deviceInfo.Id] = DeviceState.Enabled;
            }

        }
    }

    private void UsbRemoved(UsbDevice device)
    {
        if (activeDevices.TryGetValue(device.DeviceName, out var usbDevice))
        {
            activeDevices.Remove(device.DeviceName);

            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Device Removed VendorId: {usbDevice.VendorId} Product: {usbDevice.VendorId} - {device.DeviceName}");

            var deviceInfo = coreInterop.GetUsbDevice(usbDevice);
            if (deviceInfo != null)
                SendNotificationIfEnabled(DeviceMessageTypes.Ids.UsbDeviceRemoved, new UsbDeviceInfo() { ProductId = (ushort)usbDevice.ProductId, VendorId = (ushort)usbDevice.VendorId, DevicePath = device.DeviceName, DeviceInfo = deviceInfo });
        }
    }
    #endregion
}


struct UsbDeviceId
{
    public ushort VendorId;
    public ushort ProductId;
}