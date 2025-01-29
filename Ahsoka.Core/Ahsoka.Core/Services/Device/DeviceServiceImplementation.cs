using Ahsoka.Installer.Components;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device.Handlers;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Ahsoka.Services.Device;

internal abstract class DeviceServiceImplementation
{
    Dictionary<UsbDeviceId, KeyValuePair<DeviceInfo, DeviceHandlerBase>> usbDevices = new();
    DeviceList deviceList = new();

    internal void Init(Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        HardwareConfiguration config = null;

        bool isInstaller = AppDomain.CurrentDomain.FriendlyName == "Ahsoka.Installer.UI";

        string fileName = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(Ahsoka.System.Hardware.RootPaths.ApplicationRoot), ApplicationOutputConstants.AppInfoFolder, "DeviceConfiguration.pbuff");
        if (File.Exists(fileName) && !isInstaller)
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


        OnInitConfiguration(sendNotification, config, isInstaller);

        // Load Devices
        foreach (var item in config.HardwareItems)
        {
            var newDevice = new DeviceInfo()
            {
                Id = (uint)HashCode32.GetHashCode32(item.Name),
                CustomStartup = item.CustomStartup,
                Name = item.Name,
                State = item.DefaultState,
                DeviceType = item.DeviceType,
                AutoEnable = item.AutoEnable,
            };

            deviceList.Devices.Add(newDevice);

            var handler = GetDeviceHandler(newDevice);
            var usbDeviceList = handler.Init(newDevice, sendNotification);
            if (usbDeviceList != null)
                foreach (var usbItem in usbDeviceList)
                    usbDevices[usbItem] = new KeyValuePair<DeviceInfo, DeviceHandlerBase>(newDevice, handler);

        }

        if (config.UsbStartStatePrimary != UsbMode.Default || config.UsbStartStateSecondary != UsbMode.Default)
            Task.Run(() =>
            {
                if (config.UsbStartStatePrimary != UsbMode.Default)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Setting Primary USB to {config.UsbStartStatePrimary}");
                    SetUsbRoleMode(UsbPort.Primary, config.UsbStartStatePrimary);
                }

                if (config.UsbStartStateSecondary != UsbMode.Default)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Setting Secondary USB to {config.UsbStartStatePrimary}");
                    SetUsbRoleMode(UsbPort.Secondary, config.UsbStartStateSecondary);
                }
            });
    }

    public DeviceInfo GetUsbDevice(UsbDeviceId id)
    {
        if (usbDevices.TryGetValue(id, out var deviceInfo))
            return deviceInfo.Key;

        return null;
    }

    public DeviceList GetDevices()
    {
        return deviceList;
    }

    internal DeviceHandlerBase GetDeviceHandler(DeviceInfo deviceInfo)
    {
        return DeviceHandlerBase.GetDeviceHandler(deviceInfo.DeviceType);
    }

    internal abstract void OnInitConfiguration(Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification, HardwareConfiguration config, bool isInstaller);

    internal abstract UsbInfo GetUsbInfo();

    internal abstract void SetUsbRoleMode(UsbPort port, UsbMode mode);

    internal abstract void SetUsbInfo(UsbInfo usbInfo);

    internal abstract void SetRS485Config(RS485ConfigNotification serial_rs485_config);

    internal abstract TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo updateTouchscreen);

}
