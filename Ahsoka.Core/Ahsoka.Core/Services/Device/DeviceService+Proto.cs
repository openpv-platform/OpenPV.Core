using ProtoBuf;
using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.Device;

[ProtoContract]
public class DeviceMessageTypes
{
    public enum Ids
    {
        NoMessage = 0,

        GetBrightness = 1,
        SetBrightness = 2,

        GetUsbInfo = 3,
        SetUsbInfo = 4,

        SetSerialConfig = 5,

        UpdateTouchTune = 6,

        GetDevices = 10,

        EnableDeviceNotifications = 11,

        EnableDevice = 12,
        DisableDevice = 13,

        OnboardDeviceReady = 14,

        UsbDeviceAdded = 15,
        UsbDeviceRemoved = 16,

        DeviceEnabled = 17,
        DeviceDisabled = 18,
    }
}

//---------------------------------
// Adapter Info
//---------------------------------
[ProtoContract]
public class DeviceInfoRequest
{
    [ProtoMember(1)]
    public SupportedFeatures FeatureSearch { get; set; }
}

//---------------------------------
// Adapter Info
//---------------------------------
[ProtoContract]
public class DeviceList
{
    [ProtoMember(1, IsRequired = true)]
    public List<DeviceInfo> Devices { get; set; } = new();
}

[ProtoContract]
public class UsbDeviceInfo
{
    [ProtoMember(1)]
    public string DevicePath { get; set; }

    [ProtoMember(2)]
    public DeviceInfo DeviceInfo { get; set; }

    [ProtoMember(3)]
    public uint VendorId { get; set; }

    [ProtoMember(4)]
    public uint ProductId { get; set; }
}

//---------------------------------
// Adapter Info
//---------------------------------
[ProtoContract]
public class DeviceInfo
{
    [ProtoMember(1)]
    public uint Id { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; }

    [ProtoMember(3)]
    public DeviceState State { get; set; }

    [ProtoMember(4)]
    public DeviceTypes DeviceType { get; set; }

    [ProtoMember(5)]
    public string CustomStartup { get; set; }

    [ProtoMember(6)]
    public bool AutoEnable { get; set; }


    /// <summary>
    /// Get List of Services supporting this Type
    /// </summary>
    /// <returns></returns>
    public static SupportedFeatures[] GetSupportedServices(DeviceTypes deviceTypes)
    {
        switch (deviceTypes)
        {
            case DeviceTypes.OnboardWirelessComboModule:
                return [SupportedFeatures.Wifi, SupportedFeatures.BluetoothLe, SupportedFeatures.BluetoothClassic];
            case DeviceTypes.OnboardWifiModule:
                return [SupportedFeatures.Wifi];
            case DeviceTypes.OnboardBtModule:
                return [SupportedFeatures.BluetoothLe, SupportedFeatures.BluetoothClassic];
            case DeviceTypes.OnboardEthernet:
                return [SupportedFeatures.Ethernet];
            case DeviceTypes.UsbCellularModule:
                return [SupportedFeatures.Cellular];
            case DeviceTypes.IoControlModule:
                return [SupportedFeatures.Can, SupportedFeatures.Io];
            case DeviceTypes.DeviceTypeUnknown:
            default:
                return [SupportedFeatures.FeaturesUnknown];
        }
    }
}

public enum DeviceLocation
{
    AnyLocation = 0,
    Native = 1,
    Usb = 2,
}

public enum DeviceState
{
    NoState = 0,
    Ready = 1,
    Disabled = 2,
    Enabled = 3,
}

// We aren't going to enumerate every field in the rs485_config because the only other field is padding.
[ProtoContract]
public class RS485ConfigNotification
{
    [ProtoMember(1)]
    public uint Flags { get; set; }

    [ProtoMember(2)]
    public uint DelayRtsBeforeSend { get; set; }

    [ProtoMember(3)]
    public uint DelayRtsAfterSend { get; set; }
}

// USB
[ProtoContract]
public class UsbInfo
{
    [ProtoMember(1)]
    public UsbPort Port { get; set; }

    [ProtoMember(2)]
    public UsbMode Mode { get; set; }

    [ProtoMember(3)]
    public string MountPoint { get; set; }
}


public enum UsbPort
{
    Primary = 0,
    Secondary = 1,
}

public enum UsbMode
{
    Default = 0,
    Host = 1,
    Device = 2,
}

[ProtoContract]
public class TouchTuneInfo
{
    [ProtoMember(1)]
    public string FirmwareFileName { get; set; }

    [ProtoMember(2)]
    public string ConfigurationFileName { get; set; }
}

[ProtoContract]
public class TouchTuneInfoReply
{
    [ProtoMember(1)]
    public MessageReplyStatus ReplyStatus { get; set; }

    [ProtoMember(2)]
    public string ReplyReason { get; set; }

    [ProtoMember(3)]
    public Dictionary<string, string> VersionInfo { get; set; }
}

public enum MessageReplyStatus
{
    Undetermined = 0,
    Error = 1,
    Success = 2,
    Timeout = 3,
}



//---------------------------------
// Hardware Configuration
//---------------------------------
[ProtoContract]
public class HardwareConfiguration
{
    [ProtoMember(1, IsRequired = true)]
    public List<HardwareItem> HardwareItems { get; set; } = new();

    [ProtoMember(2)]
    public UsbMode UsbStartStatePrimary { get; set; }

    [ProtoMember(3)]
    public UsbMode UsbStartStateSecondary { get; set; }
}

//---------------------------------
// Hardware Configuration Item
//---------------------------------
[ProtoContract]
public class HardwareItem
{
    [ProtoMember(1)]
    public string Name { get; set; }

    [ProtoMember(2)]
    public DeviceTypes DeviceType { get; set; }

    [ProtoMember(3)]
    public DeviceState DefaultState { get; set; }

    [ProtoMember(4)]
    public string CustomStartup { get; set; }

    [ProtoMember(5)]
    public bool AutoEnable { get; set; }

}


public enum SupportedFeatures
{
    FeaturesUnknown = 0,
    Wifi = 1,
    BluetoothClassic = 2,
    BluetoothLe = 3,
    Ethernet = 4,
    Cellular = 5,
    Can = 6,
    Io = 7,
}


public enum DeviceTypes
{
    DeviceTypeUnknown = 0,
    OnboardWirelessComboModule = 1,
    OnboardWifiModule = 2,
    OnboardBtModule = 3,
    OnboardEthernet = 4,
    UsbCellularModule = 5,
    UsbEthernet = 6,
    IoControlModule = 10,
}