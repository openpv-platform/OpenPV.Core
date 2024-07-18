using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;

namespace Ahsoka.Services.System.Platform;

[ExcludeFromCodeCoverage]
internal class TIServiceImplementation : IServiceImplementation
{
    #region Fields
    //Task task = null;
    //CancellationTokenSource tokenSource = null;
    //string currentMountPoint = "";
    readonly string[] logoFileTypes = { ".png", ".jpg" };

    public const string NativeLibraryName = "AhsokaLib/Ahsoka.Services.ST.so";
    private const string firmwareModelNumberPath = "/sys/firmware/devicetree/base/model";
    private const string firmwareSerialNumberPath = "/sys/block/mmcblk0/device/cid";
   
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

    private void BuzzerInit()
    {
       
    }

    void IServiceImplementation.SetBrightness(int percent)
    {
        if (percent < 0)
            percent = 0;
        else if (percent > 100)
            percent = 100;
    }

    int IServiceImplementation.GetBrightness()
    {
        return 100;
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

    public Dictionary<string, string> GetPlatformVersions()
    {
        ProcessUtility.RunProcessScript("uname -r", "", out string stdout, out string _);

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
