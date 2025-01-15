using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Device;

[ExcludeFromCodeCoverage]
internal class STServiceImplementation : DeviceServiceImplementation
{
    Action<DeviceMessageTypes.Ids, object, byte[]> sendNotificationHandler;
    Task task = null;
    CancellationTokenSource tokenSource = null;
    string currentMountPoint = "";
    bool hasUsbEth;
    readonly string[] logoFileTypes = { ".png", ".jpg" };

    internal override void OnInitConfiguration(Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification, HardwareConfiguration config, bool isInstaller)
    {
        sendNotificationHandler = sendNotification;

        // Apply Defaults (Support WiFi and Cell Modems if No Device File Found
        if (isInstaller)
        {
            config.HardwareItems.Clear();
            config.UsbStartStatePrimary = UsbMode.Device;

            config.HardwareItems.Add(new()
            {
                Name = "ESP32 Onboard Combo Module",
                DeviceType = DeviceTypes.OnboardWirelessComboModule,
                AutoEnable = true,
            });

            config.HardwareItems.Add(new()
            {
                Name = "USB Cell Modem Module",
                DeviceType = DeviceTypes.UsbCellularNetwork,
                AutoEnable = false,
            });
        }

        if (!config.HardwareItems.Any(x=>x.DeviceType == DeviceTypes.UsbEthernet))
            config.HardwareItems.Add(new()
            {
                Name = "USB Ethernet",
                DeviceType = DeviceTypes.UsbEthernet,
                AutoEnable = false, // Ignored.
            });
    }

  
    internal override UsbInfo GetUsbInfo()
    {
        UsbInfo usbInfo = new()
        {
            MountPoint = currentMountPoint,
        };

        string usbRole = File.ReadAllText(USBRoleSwap.usbRoleDriverPath).Trim();
        if ("host" == usbRole)
            usbInfo.Mode = UsbMode.Host;
        else if ("none" == usbRole)
            usbInfo.Mode = UsbMode.Device;
        else
            usbInfo.Mode = UsbMode.Default;

        return usbInfo;
    }

    internal override void SetUsbInfo(UsbInfo usbInfo)
    {
        if (usbInfo.Mode == UsbMode.Host)
        {
            SetUsbRoleMode(usbInfo.Port, UsbMode.Host);

            tokenSource = new CancellationTokenSource();
            task = Task.Run(() =>
            {
               
                while (!tokenSource.IsCancellationRequested)
                {
                    CheckForMountPoint(usbInfo);

                    CheckForUsbEthernet(usbInfo);

                    Thread.Sleep(1000);
                }
            }, tokenSource.Token);
        }
        else if (usbInfo.Mode == UsbMode.Device)
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
                task.Wait();
                tokenSource = null;
                task = null;

                if (currentMountPoint != "")
                {
                    var output = Process.Start("umount", $"{currentMountPoint}");
                    output.WaitForExit();
                    Console.WriteLine($"Unmounted USB Stick at {currentMountPoint}");
                    currentMountPoint = "";
                }
            }

            SetUsbRoleMode(usbInfo.Port, usbInfo.Mode);
        }
    }

    private void CheckForUsbEthernet(UsbInfo usbInfo)
    {
        string command = "networkctl list eth0";

        // Try fat and exfat
        ProcessUtility.RunProcessScript(command, null, out string stdout, out string stderr);
        bool hasEth = stdout.Contains("routable") && stdout.Contains("configured");

        if (hasEth != hasUsbEth)
        {
            var info = GetDevices().Devices.FirstOrDefault(x => x.DeviceType == DeviceTypes.UsbEthernet);
          
            hasUsbEth = hasEth;
            if (hasEth)
                GetDeviceHandler(info).EnableDevice(info,sendNotificationHandler);
            else
                GetDeviceHandler(info).DisableDevice(info, sendNotificationHandler);
            
            Console.WriteLine($"USB Ethernet State Changed {hasUsbEth}");
        }
    }

    private void CheckForMountPoint(UsbInfo usbInfo)
    {
        string blockCommand = "/sbin/blkid -l -t TYPE=\"{0}\" -o device";
        // Try fat and exfat
        ProcessUtility.RunProcessScript(string.Format(blockCommand, "vfat"), null, out string stdout, out string stderr);
        if (stdout == "")
            ProcessUtility.RunProcessScript(string.Format(blockCommand, "exfat"), null, out stdout, out stderr);

        if (stdout != "")
        {
            string usbDevice = stdout.Trim();
            Console.WriteLine($"Found USB Stick at {usbDevice}");

            var output = Process.Start("mount", $"{usbDevice} {usbInfo.MountPoint}");
            output.WaitForExit();
            Console.WriteLine($"Mounted USB Stick at {usbDevice} on {usbInfo.MountPoint}");
            currentMountPoint = usbInfo.MountPoint;

            return;
        }
    }

    internal override void SetRS485Config(RS485ConfigNotification config)
    {
        // We are converting the protobuf structure back to it's base type to set the config locally.
        var serial_rs485_config = new SerialRS485()
        {
            flags = config.Flags,
            delay_rts_after_send = config.DelayRtsAfterSend,
            delay_rts_before_send = config.DelayRtsBeforeSend
        };

        RS485ConfigUtility.Configure(serial_rs485_config);
    }

    internal override TouchTuneInfoReply UpdateTouchTune(TouchTuneInfo updateTouchscreen)
    {
        var response = new TouchTuneInfoReply()
        {
            ReplyStatus = MessageReplyStatus.Undetermined,
            ReplyReason = "Not Implemented on ST"
        };

        foreach (KeyValuePair<string, string> pair in System.Platform.STServiceImplementation.GetScreenInfo())
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

    internal override void SetUsbRoleMode(UsbPort port, UsbMode mode)
    {
        int returnCode = 0;
        returnCode = USBRoleSwap.ChangeModes(port, mode);

        if (returnCode != 0)
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Ahsoka.Services (ST): SetPlatformUsbRoleMode failed: returnCode{returnCode}");
        else
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Ahsoka.Services (ST): SetPlatformUsbRoleMode succeeded");
    }
}


