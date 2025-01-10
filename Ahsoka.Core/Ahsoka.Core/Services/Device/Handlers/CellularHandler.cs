using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ahsoka.Services.Device.Handlers;

internal class CellularHandler : DeviceHandlerBase
{
    string device = "/dev/cdc-wdm0";

    internal override List<UsbDeviceId> Init(DeviceInfo deviceInfo,  Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        base.Init(deviceInfo, sendNotification);

        // Add Valid VID/PID Items.
       return new() { new UsbDeviceId() { VendorId = 0x1bc7, ProductId = 0x1031 } };
    }

    internal override DeviceState GetDeviceState()
    {
        if (!Path.Exists(device))
            return DeviceState.NoState;

        string command = $"qmicli -d {device}  --dms-get-operating-mode";
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, command);

        ProcessUtility.RunProcess("/bin/sh", $"-c \"{command}", null, out string log, out string error);
        if (!log.Contains("'online'"))
            return DeviceState.Enabled;
        else
            return DeviceState.Disabled;

    }

    internal override void DisableDevice(DeviceInfo deviceInfo, Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        string command = $"qmicli -d {device} --dms-set-operating-mode='persistent-low-power'";
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, command);

        ProcessUtility.RunProcess("/bin/sh", $"-c \"{command}", null, out string log, out string error);
       
        if (!log.Contains ("Operating mode set successfully"))
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"CellularHandler log: {log}");
        if (error != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"CellularHandler error: {error}");

        
        base.DisableDevice(deviceInfo, sendNotification);
    }

    internal override void EnableDevice(DeviceInfo deviceInfo, Action<DeviceMessageTypes.Ids, object, byte[]> sendNotification)
    {
        string command = $"qmicli -d {device} --dms-set-operating-mode='online'";
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, command);

        ProcessUtility.RunProcess("/bin/sh", $"-c \"{command}", null, out string log, out string error);

        if (!log.Contains("Operating mode set successfully"))
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"CellularHandler log: {log}");
        if (error != "")
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"CellularHandler error: {error}");

        base.EnableDevice(deviceInfo, sendNotification);
    }

}