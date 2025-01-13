using Ahsoka.Installer.Components;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Device.Handlers;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
    readonly string[] logoFileTypes = { ".png", ".jpg" };

    internal override void OnInit(Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification, DeviceList deviceList)
    {
        sendNotificationHandler = sendNotification;

        // Fall back to Old Combo Module
        if (deviceList.Devices.Count == 0)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Device Setup Path Not Found, Adding Default Modules");

            deviceList.Devices.Add(new DeviceInfo()
            {
                Id = (uint)HashCode32.GetHashCode32("ESP32 Onboard Combo Module"),
                Name = "ESP32 Onboard Combo Module",
                DeviceType = DeviceTypes.OnboardWirelessComboModule
            });
        }
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
            SetUsbRoleMode(UsbMode.Host);

            tokenSource = new CancellationTokenSource();
            task = Task.Run(() =>
            {
                string blockCommand = "/sbin/blkid -l -t TYPE=\"{0}\" -o device";

                while (!tokenSource.IsCancellationRequested)
                {
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

            SetUsbRoleMode(UsbMode.Device);
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

    internal override void SetUsbRoleMode(UsbMode mode)
    {
        int returnCode = 0;
        returnCode = USBRoleSwap.ChangeModes(mode);

        if (returnCode != 0)
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Ahsoka.Services (ST): SetPlatformUsbRoleMode failed: returnCode{returnCode}");
        else
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Ahsoka.Services (ST): SetPlatformUsbRoleMode succeeded");
    }
}


