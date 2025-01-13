using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ahsoka.Installer.Components;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device.Handlers;
using Ahsoka.System;
using Ahsoka.Utility;

namespace Ahsoka.Services.Device;

internal abstract class DeviceServiceImplementation
{
    Dictionary<UsbDeviceId, KeyValuePair<DeviceInfo, DeviceHandlerBase>> usbDevices = new();
    DeviceList deviceList = new();

    internal void Init(Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        HardwareConfiguration config = null;

        string fileName = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(Ahsoka.System.Hardware.RootPaths.ApplicationRoot), ApplicationOutputConstants.AppInfoFolder, "DeviceConfiguration.pbuff");
        if (File.Exists(fileName) && AppDomain.CurrentDomain.FriendlyName != "Ahsoka.Installer.UI")
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Device Setup Path Found");
            try
            {
                FileStream fileStream = File.OpenRead(fileName);
                config = ProtoBuf.Serializer.Deserialize<HardwareConfiguration>(fileStream);
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Device Service Loaded with {config.HardwareItems.Count} Devices.");
            }
            catch (Exception ex)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, ex.Message);
            }
        }

        if (config == null)
            config = new();

        // Load Devices
        foreach(var item in config.HardwareItems)
            deviceList.Devices.Add(new DeviceInfo()
            {
                Id = (uint)HashCode32.GetHashCode32(item.Name),
                CustomStartup = item.CustomStartup,
                Name = item.Name,
                State = DeviceState.NoState,
                DeviceType = item.DeviceType,
                AutoEnable = item.AutoEnable,
            });

        OnInit(sendNotification, deviceList);

        foreach (var item in deviceList.Devices)
        {
            var handler = GetDeviceHandler(item);
            var usbDeviceList = handler.Init(item, sendNotification);
            if (usbDeviceList != null)
                foreach (var usbItem in usbDeviceList)
                    usbDevices[usbItem] = new KeyValuePair<DeviceInfo, DeviceHandlerBase>(item, handler);
        }

        if (config.UsbStartStatePrimary != UsbMode.Default || config.UsbStartStateSecondary != UsbMode.Default)
            Task.Run(() =>
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Setting Primary USB to {config.UsbStartStatePrimary}");
                SetUsbRoleMode(config.UsbStartStatePrimary);
                // Secondary coming Later.
            });
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
