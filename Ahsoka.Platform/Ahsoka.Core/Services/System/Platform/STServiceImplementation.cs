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
internal class STServiceImplementation : IServiceImplementation
{
    Task task = null;
    CancellationTokenSource tokenSource = null;
    string currentMountPoint = "";
    private bool buzzerInit = false;
    readonly string[] logoFileTypes = { ".png", ".jpg" };

    #region NativeLibrary
    public const string NativeLibraryName = "AhsokaLib/Ahsoka.Services.ST.so";
    private const string firmwareModelNumberPath = "/sys/firmware/devicetree/base/model";
    private const string firmwareSerialNumberPath = "/sys/firmware/devicetree/base/serial-number";
    private const string backlightDriverPath = "/sys/devices/platform/panel_backlight/backlight/panel_backlight";
    private const string usbRoleDriverPath = "/sys/devices/platform/soc/49000000.usb-otg/usb_role/49000000.usb-otg-role-switch/role";
    private const string buzzerExportPath = "/sys/class/pwm/pwmchip4/export";
    private const string buzzerDriverPath = "/sys/class/pwm/pwmchip4/pwm0";

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

    private void BuzzerInit()
    {
        if (!Directory.Exists(buzzerDriverPath))
        {
            Console.WriteLine("Exporting Channel - Initialzing Buzzer");
            File.WriteAllText(buzzerExportPath, "0");
        }
        buzzerInit = true;
    }

    BuzzerConfig IServiceImplementation.GetBuzzerConfig()
    {
        if (!buzzerInit)
            BuzzerInit();

        int volumeAdjusted = Int32.Parse(File.ReadAllText(Path.Combine(buzzerDriverPath, "duty_cycle")).Trim());
        int period = Int32.Parse(File.ReadAllText(Path.Combine(buzzerDriverPath, "period")).Trim());

        BuzzerConfig buzzerConfig = new()
        {
            IsEnabled = Int32.Parse(File.ReadAllText(Path.Combine(buzzerDriverPath, "enable")).Trim()) == 1 ? true : false,
            VolumePct = volumeAdjusted == 0 ? 0 : (int)(volumeAdjusted / (period * 0.5 * 0.01)),
            FrequencyInHz = period == 0 ? 0 : 1000000000 / period
        };

        return buzzerConfig;
    }

    void IServiceImplementation.SetBuzzerConfig(BuzzerConfig buzzerConfig)
    {
        if (!buzzerInit)
            BuzzerInit();

        // NOTE: Frequency is bounded within a range of [1, 5000]. Volume% is bounded within a range of [0, 100].
        buzzerConfig.FrequencyInHz = Math.Max(Math.Min(buzzerConfig.FrequencyInHz, 5000), 1);
        buzzerConfig.VolumePct = Math.Max(Math.Min(buzzerConfig.VolumePct, 100), 0);

        int period = 1000000000 / buzzerConfig.FrequencyInHz;
        int dutyCycle = (int)((period / 2.0f) * (buzzerConfig.VolumePct / 100.0f)); // Ensure < Period
        int enable = buzzerConfig.IsEnabled ? 1 : 0;

        // NOTE: duty_cycle must be lower than or equal to period. The following conditional makes sure to not write a period less than current duty_cycle.
        int currDutyCycle = Int32.Parse(File.ReadAllText(Path.Combine(buzzerDriverPath, "duty_cycle")).Trim());
        if (currDutyCycle > period)
            File.WriteAllText(Path.Combine(buzzerDriverPath, "duty_cycle"), "0");

        File.WriteAllText(Path.Combine(buzzerDriverPath, "period"), period.ToString());
        File.WriteAllText(Path.Combine(buzzerDriverPath, "duty_cycle"), dutyCycle.ToString());
        File.WriteAllText(Path.Combine(buzzerDriverPath, "enable"), enable.ToString()); // period can not be 0 when enabling
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
            MountPoint = ""
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
                while (!tokenSource.IsCancellationRequested)
                {
                    ProcessUtility.RunProcessScript("/sbin/blkid -l -t TYPE=\"vfat\" -o device", null, out string stdout, out string stderr);
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
            { SystemServiceDataKeys.TouchVendorID, VendorId },
            { SystemServiceDataKeys.TouchProductID, ProductId },
            { SystemServiceDataKeys.TouchHardwareVersion, HardwareVersion },
            { SystemServiceDataKeys.TouchFirmwareVersion, FirmwareVersion },
            { SystemServiceDataKeys.TouchConfigCRC, ConfigCrc }
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
