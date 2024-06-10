using Ahsoka.ServiceFramework;
using Ahsoka.Services.IO;
using Ahsoka.Services.IO.RCD;
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
internal class TIServiceImplementation : IServiceImplementation
{
    #region Fields
    //Task task = null;
    //CancellationTokenSource tokenSource = null;
    //string currentMountPoint = "";
    private bool buzzerInit = false;
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

    BuzzerConfig IServiceImplementation.GetBuzzerConfig()
    {
        BuzzerConfig buzzerConfig = new()
        {
            IsEnabled = false,
            VolumePct = 100,
            FrequencyInHz = 0
        };

        return buzzerConfig;
    }

    void IServiceImplementation.SetBuzzerConfig(BuzzerConfig buzzerConfig)
    {
        if (!buzzerInit)
            BuzzerInit();

        }

    void IServiceImplementation.SetBrightness(int percent)
    {
        if (percent < 0)
            percent = 0;
        else if (percent > 100)
            percent = 100;

        //int brightnessSteps = int.Parse(File.ReadAllText(Path.Combine(backlightDriverPath, "max_brightness")));
        //int rawValue = (int)Math.Ceiling(percent / (100.0f / brightnessSteps));
        //File.WriteAllText(Path.Combine(backlightDriverPath, "brightness"), rawValue.ToString());
    }

    int IServiceImplementation.GetBrightness()
    {
        //using var stopwatch = new AhsokaStopwatch("GetBrightness");

        //int brightnessSteps = int.Parse(File.ReadAllText(Path.Combine(backlightDriverPath, "max_brightness")));
        //string rawValue = File.ReadAllText(Path.Combine(backlightDriverPath, "brightness"));
        //return (int)Math.Floor(int.Parse(rawValue) * (100.0f / brightnessSteps));
        return 100;
    }

    public UsbInfo GetUsbInfo()
    {
        UsbInfo usbInfo = new()
        {
            MountPoint = ""
        };

        /*string usbRole = File.ReadAllText(usbRoleDriverPath).Trim();
        if ("host" == usbRole)
            usbInfo.Mode = UsbMode.Host;
        else if ("none" == usbRole)
            usbInfo.Mode = UsbMode.Device;
        else
            usbInfo.Mode = UsbMode.Unknown;*/

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

    public VoltageValue GetVBat()
    {
        /*
            Public function to read and return the voltage value at the
            VBAT Pin. Makes use of existing RCD IO Service ADC utility.
        */
        double VBatVoltageDivider = ((56 + 5.1) / 5.1);

        var response = new VoltageValue();
        float RawValue = ADCUtils.GetVoltsRawValue(ADCInput.VBAT);

        // apply voltage divider to calculate volts at the input pin
        response.MilliVolts = RawValue * VBatVoltageDivider;
        return response;
    }

    IgnitionStates state = IgnitionStates.Unknown;
    public IgnitionState GetIGNPin()
    {
        double returnValue = -1;

        // Return Unknown Until System is Ready
        if (File.Exists("/sys/devices/platform/soc/48003000.adc/48003000.adc:adc@0/iio:device0/in_voltage_scale"))
        {
            /*
                Public function to read and return the voltage value at the
                Ignition (IGN) Pin. Makes use of existing RCD IO Service ADC utility.
            */
            double IGNPinVoltageDivider = ((56 + 5.1) / 5.1);

            float RawValue = ADCUtils.GetVoltsRawValue(ADCInput.IGN_PIN);

            // apply voltage divider to calculate volts at the input pin
            returnValue = RawValue * IGNPinVoltageDivider;
        }

        // Check whether IGN_PIN is grounded.  LMR50410 enable low level is >=0.95V.
        // Note that this threshold is less than the minimum AIN pull-up voltage of 2.5V,
        // and the UVLO shut-down threshold of 4.8V.
        if (returnValue is not (-1) and < 950)
        {
            state = IgnitionStates.Off;
        }
        // LMR50410 enable high level is <=1.36V.  Note that this threshold handles 6V VBAT,
        // and treats short to AIN or DIN as "on".  DIN maximum pull-up voltage is 1/2 Vbat.
        else if (returnValue > 1360)
        {
            state = IgnitionStates.On;
        }

        return new IgnitionState() { MilliVolts = returnValue, State = state };
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
            { SystemServiceDataKeys.BSPVersion, stdout.Replace("BSP Version:","").Trim() }
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
