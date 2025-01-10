using System;
using System.Collections.Generic;
using System.IO;
using Ahsoka.Installer.Components;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device.Handlers;
using Ahsoka.System;

namespace Ahsoka.Services.Device;

internal abstract class DeviceServiceImplementation
{
    Dictionary<UsbDeviceId, KeyValuePair<DeviceInfo, DeviceHandlerBase>> usbDevices = new();
    DeviceList deviceList;

    internal void Init(Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        string fileName = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(Ahsoka.System.Hardware.RootPaths.ApplicationRoot), ApplicationOutputConstants.AppInfoFolder, "DeviceConfiguration.pbuff");
        if (File.Exists(fileName) && AppDomain.CurrentDomain.FriendlyName != "Ahsoka.Installer.UI")
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Device Setup Path Found");
            try
            {
                FileStream fileStream = File.OpenRead(fileName);
                deviceList = ProtoBuf.Serializer.Deserialize<DeviceList>(fileStream);
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Device Service Loaded with {deviceList.Devices.Count} Devices.");
            }
            catch (Exception ex)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, ex.Message);
            }
        }

        if (deviceList == null)
            deviceList = new();

        OnInit(sendNotification, deviceList);

        foreach (var item in deviceList.Devices)
        {
            var handler = GetDeviceHandler(item);
            var usbDeviceList = handler.Init(item, sendNotification);
            if (usbDeviceList != null)
                foreach (var usbItem in usbDeviceList)
                    usbDevices[usbItem] = new KeyValuePair<DeviceInfo, DeviceHandlerBase>(item, handler);
        }
    }
   
    public DeviceInfo GetUsbDevice(UsbDeviceId id)
    {
        if (usbDevices.TryGetValue(id, out var deviceInfo))
            return deviceInfo.Key;

        return null;
    }

    public DeviceList HandleGetDevices()
    {
        return deviceList;
    }

    internal DeviceHandlerBase GetDeviceHandler(DeviceInfo deviceInfo)
    {
        return DeviceHandlerBase.GetDeviceHandler(deviceInfo.DeviceType);
    }

    internal abstract void OnInit(Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification, DeviceList list);

    internal abstract UsbInfo GetUsbInfo();

    internal abstract void SetUsbRoleMode(UsbMode mode);

    internal abstract  void SetUsbInfo(UsbInfo usbInfo);

    internal abstract void SetRS485Config(RS485ConfigNotification serial_rs485_config);

    internal abstract TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo updateTouchscreen);
    
}
