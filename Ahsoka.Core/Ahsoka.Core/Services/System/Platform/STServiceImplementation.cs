using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.System.Platform;

[ExcludeFromCodeCoverage]
internal class STServiceImplementation : IServiceImplementation
{
    Task task = null;
    CancellationTokenSource tokenSource = null;
    string currentMountPoint = "";
    readonly string[] logoFileTypes = { ".png", ".jpg" };

    #region NativeLibrary
    public const string NativeLibraryName = "AhsokaLib/Ahsoka.Services.OpenViewLinux.so";
    private const string firmwareModelNumberPath = "/sys/firmware/devicetree/base/model";
    private const string firmwareSerialNumberPath = "/sys/firmware/devicetree/base/serial-number";
    private const string backlightDriverPath = "/sys/devices/platform/panel_backlight/backlight/panel_backlight";
    private const string usbRoleDriverPath = "/sys/devices/platform/soc/49000000.usb-otg/usb_role/49000000.usb-otg-role-switch/role";

    [DllImport(NativeLibraryName)]
    private static extern int EchoValue(int x);

    [DllImport(NativeLibraryName)]
    private static extern void SetUsbRoleMode(int mode);
    #endregion

    #region RS485
    // NOTE: BMG (Aug. 10, 2022) This makes the assumption that the only serial port that is going to have it's
    // configuration changed, whether rs485 or not, is going to be the /dev/ttySTM1 that is used in the current S50 and
    // S70 boards. This is likely going to have to change at some point with some board specific configuration being
    // referenced and more than one serial port being able to be handled at once.
    private const string SerialPortName = "/dev/ttySTM1\0";

    private void CallIoctl(SerialRS485 serialRS485Config)
    {
        SafeFileHandle safeFileHandle;

        safeFileHandle = LinuxSystemUtilities.Open(SerialPortName, SerialRS485Constants.O_RDWR | SerialRS485Constants.O_NONBLOCK);
        if (safeFileHandle.IsInvalid)
        {
            throw new Exception($"There was an error opening the serial port handle. Errno: {LinuxSystemUtilities.Errno}");
        }

        using (safeFileHandle)
        {
            int result = LinuxSystemUtilities.Ioctl(safeFileHandle, SerialRS485Constants.TIOCSRS485, ref serialRS485Config);
            if (result == -1)
            {
                throw new Exception($"Ioctl returned an error. Errno: {LinuxSystemUtilities.Errno}");
            }
        }
    }
    #endregion

    #region Interface
    string IServiceImplementation.GetHardwareModel()
    {
        return File.ReadAllText(firmwareModelNumberPath).Trim();
    }

    string IServiceImplementation.GetHardwareSerialNumber()
    {
        return File.ReadAllText(firmwareSerialNumberPath).Trim().Replace("\0", "");
    }

    void IServiceImplementation.SetBrightness(int percent)
    {
        if (percent < 0)
            percent = 0;
        else if (percent > 100)
            percent = 100;

        int brightnessSteps = int.Parse(File.ReadAllText(Path.Combine(backlightDriverPath, "max_brightness")));
        int rawValue = (int)Math.Ceiling(percent / (100.0f / brightnessSteps));
        File.WriteAllText(Path.Combine(backlightDriverPath, "brightness"), rawValue.ToString());
    }

    int IServiceImplementation.GetBrightness()
    {
        using var stopwatch = new AhsokaStopwatch("GetBrightness");

        int brightnessSteps = int.Parse(File.ReadAllText(Path.Combine(backlightDriverPath, "max_brightness")));
        string rawValue = File.ReadAllText(Path.Combine(backlightDriverPath, "brightness"));
        return (int)Math.Floor(int.Parse(rawValue) * (100.0f / brightnessSteps));
    }

    public UsbInfo GetUsbInfo()
    {
        UsbInfo usbInfo = new()
        {
            MountPoint = currentMountPoint,
        };

        string usbRole = File.ReadAllText(usbRoleDriverPath).Trim();
        if ("host" == usbRole)
            usbInfo.Mode = UsbMode.Host;
        else if ("none" == usbRole)
            usbInfo.Mode = UsbMode.Device;
        else
            usbInfo.Mode = UsbMode.Unknown;

        return usbInfo;
    }

    public void SetUsbInfo(UsbInfo usbInfo)
    {
        if (usbInfo.Mode == UsbMode.Host)
        {
            SetUsbRoleMode(0);

            tokenSource = new CancellationTokenSource();
            task = Task.Run(() =>
            {
                string blockCommand = "/sbin/blkid -l -t TYPE=\"{0}\" -o device";

                while (!tokenSource.IsCancellationRequested)
                {
                    // Try fat and exfat
                    ProcessUtility.RunProcessScript(string.Format(blockCommand, "vfat"), null, out string stdout, out string stderr);
                    if (stdout == "")
                        ProcessUtility.RunProcessScript(string.Format(blockCommand, "exfat"), null, out  stdout, out  stderr);

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

            SetUsbRoleMode(1);
        }
    }

    void IServiceImplementation.SetRS485Config(SerialRS485 serial_rs485_config)
    {
        CallIoctl(serial_rs485_config);
    }

    void IServiceImplementation.Reboot()
    {
        LinuxSystemUtilities.Reboot();
    }

    void IServiceImplementation.ShutdownApplication(int shutDownCode)
    {
        // Request Application Unit to Shutdown
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Requesting Application Service Shut Down...");
        ProcessUtility.RunProcessAsync("systemctl", $"stop Ahsoka.Application", null, null, CancellationToken.None);

        // Exit Application
        Environment.Exit(shutDownCode);
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
        // Touch IDs
        string VendorId = "DNE";
        string ProductId = "DNE";
        try
        {
            string fileData = File.ReadAllText("/sys/bus/i2c/drivers/atmel_mxt_ts/0-004a/object");
            const string search = "T38:\n"; // Section with Version Info
            string[] versionData = fileData.Substring(fileData.IndexOf(search) + search.Length, 11).Split(' ');
            VendorId = $"0x{versionData[1]}{versionData[0]}";
            ProductId = $"0x{versionData[3]}{versionData[2]}";
        }
        catch { } // Prevent crash if not there.

        // Touch Versions and CRC
        string FirmwareVersion = "DNE";
        string HardwareVersion = "DNE";
        string ConfigCrc = "DNE";
        try
        {
            FirmwareVersion = File.ReadAllText("/sys/devices/platform/soc/5c002000.i2c/i2c-0/0-004a/fw_version"); // STUB - weird case where these didn't exist after stop
            HardwareVersion = File.ReadAllText("/sys/devices/platform/soc/5c002000.i2c/i2c-0/0-004a/hw_version");
            ConfigCrc = File.ReadAllText("/sys/devices/platform/soc/5c002000.i2c/i2c-0/0-004a/config_crc");
        }
        catch { } // Prevent crash if not there.

        Dictionary<string, string> results = new()
        {
            { SystemServiceMessages.TouchVendorID, VendorId },
            { SystemServiceMessages.TouchProductID, ProductId },
            { SystemServiceMessages.TouchHardwareVersion, HardwareVersion },
            { SystemServiceMessages.TouchFirmwareVersion, FirmwareVersion },
            { SystemServiceMessages.TouchConfigCRC, ConfigCrc }
        };

        return results;
    }

    public double GetCPUTemp()
    {
        string path = "/sys/devices/virtual/thermal/thermal_zone0/hwmon0/temp1_input";
        if (File.Exists(path))
        {
            var value = int.Parse(File.ReadAllText(path));
            return (double)value / 1000;
        }

        return 0;
    }

    public Dictionary<string, string> GetPlatformVersions()
    {
        ProcessUtility.RunProcessScript("bsp-info -v", "", out string stdout, out string _);

        Dictionary<string, string> results = new()
        {
            { SystemServiceMessages.BSPVersion, stdout.Replace("BSP Version:","").Trim() }
        };

        foreach (KeyValuePair<string, string> pair in GetScreenInfo())
            results.Add(pair.Key, pair.Value);

        return results;
    }

    void IServiceImplementation.SetBootLogo(string logoPath)
    {
        if (!File.Exists(logoPath) || !logoFileTypes.Any(x => logoPath.EndsWith(x))) return;
        string installPath = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(Ahsoka.System.Hardware.InstallerPaths.BootLogoFileName);
        File.Copy(logoPath, installPath, true);
    }
    #endregion
}
