using Ahsoka.Installer.Components;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Ahsoka.Services.Device;

[ExcludeFromCodeCoverage]
internal class TIServiceImplementation : IDeviceServiceImplementation
{
    #region Fields
    DeviceList deviceList;
    readonly string[] logoFileTypes = { ".png", ".jpg" };

    private const string firmwareModelNumberPath = "/sys/firmware/devicetree/base/model";
    private const string firmwareSerialNumberPath = "/sys/block/mmcblk0/device/cid";
   
    private const string SerialPortName = "/dev/ttySTM1\0";

  
    #endregion

    #region Interface
    private void BuzzerInit()
    {
       
    }


    public UsbInfo GetUsbInfo()
    {
        UsbInfo usbInfo = new()
        {
            MountPoint = ""
        };


        return usbInfo;
    }

    public void SetUsbInfo(UsbInfo usbInfo)
    {
        if (usbInfo.Mode == UsbMode.Host)
        {
           
        }
        else if (usbInfo.Mode == UsbMode.Device)
        {
           
        }
    }

    void IDeviceServiceImplementation.SetRS485Config(RS485ConfigNotification serial_rs485_config)
    {
        
    }

    public TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo updateTouchscreen)
    {
        var response = new TouchTuneInfoReply()
        {
            ReplyStatus = MessageReplyStatus.Undetermined,
            ReplyReason = "Not Implemented on ST"
        };

        foreach (KeyValuePair<string, string> pair in GetScreenInfo())
            response.VersionInfoes.Add(pair.Key, pair.Value);

        string configurationPath = "/lib/firmware/maxtouch.cfg";
        string firmwarePath = "/lib/firmware/maxtouch.fw";
        string configurationDevicePath = "/sys/bus/i2c/drivers/atmel_mxt_ts/0-004a/update_cfg";
        string firmwareDevicePath = "/sys/bus/i2c/drivers/atmel_mxt_ts/0-004a/update_fw";
        if (File.Exists(updateTouchscreen.ConfigurationFileName) && File.Exists(updateTouchscreen.FirmwareFileName))
        {
            File.Copy(updateTouchscreen.FirmwareFileName, firmwarePath, true);
            File.Copy(updateTouchscreen.ConfigurationFileName, configurationPath, true);

            ProcessUtility.RunProcessScript($"echo {firmwarePath} > {firmwareDevicePath}", null, out string _, out string _);

            File.Delete(firmwarePath);
            File.Delete(configurationPath);

            response.ReplyStatus = MessageReplyStatus.Success;
            response.ReplyReason = "Updated touchscreen firmware and configuration.";
        }
        else if (File.Exists(updateTouchscreen.ConfigurationFileName))
        {
            File.Copy(updateTouchscreen.ConfigurationFileName, configurationPath, true);
            ProcessUtility.RunProcessScript($"echo {configurationPath} > {configurationDevicePath}", null, out string _, out string _);
            File.Delete(configurationPath);
            response.ReplyStatus = MessageReplyStatus.Success;
            response.ReplyReason = "Updated touchscreen configuration.";
        }
        else
        {
            response.ReplyStatus = MessageReplyStatus.Success;
            response.ReplyReason = "Returned touchscreen VendorId and ProductId.  No updates applied.";
        }

        return response;
    }

    private static Dictionary<string, string> GetScreenInfo()
    {
        Dictionary<string, string> results = new(); 

        return results;
    }

    public double GetCPUTemp()
    {
        string path = "/sys/devices/virtual/thermal/thermal_zone0/temp";
        if (File.Exists(path))
        {
            var value = int.Parse(File.ReadAllText(path));
            return (double)value / 1000;
        }

        return 0;
    }

    public DeviceList HandleGetDevices(DeviceInfoRequest deviceInfoRequest)
    {
        DeviceList list = new DeviceList();
        foreach (var item in deviceList.Devices)
        {
            var services = DeviceInfo.GetSupportedServices(item.DeviceType);
            if (deviceInfoRequest.FeatureSearch == SupportedFeatures.FeaturesUnknown || services.Contains(deviceInfoRequest.FeatureSearch))
                list.Devices.Add(item);
        }
        return deviceList; 
    }

    public void Init(Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        string fileName = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(Ahsoka.System.Hardware.RootPaths.ApplicationRoot), ApplicationOutputConstants.AppInfoFolder, ApplicationOutputComponent.AppVersionFile);
        if (File.Exists(fileName))
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Device Setup Path Found");
            FileStream fileStream = File.OpenRead(fileName);
            deviceList = ProtoBuf.Serializer.Deserialize<DeviceList>(fileStream);
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Device Service Loaded with {deviceList.Devices.Count} Devices.");
        }
        else
        {
            // Fall back to Old Combo Module
            deviceList = new DeviceList()
            {
                Devices = { new DeviceInfo()
                {
                     Id= (uint)HashCode32.GetHashCode32("Wifi Module"),
                     Name = "Wifi Module",
                     DeviceType = DeviceTypes.OnboardWifiModule,
                }}
            };
        }
    }

    #endregion
}
