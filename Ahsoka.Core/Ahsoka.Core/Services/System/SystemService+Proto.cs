using Ahsoka.Core;
using Ahsoka.Core.Utility;
using ProtoBuf;
using System;
using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.System;

[ProtoContract]
public class SystemMessageTypes
{
    public enum Ids
    {
        NoMessage = 0,

        Echo = 1,
        GetServiceInfo = 4,
        GetHardwareInformation = 8,

        LoadApplication = 10,
        LoadApplicationData = 12,
        LoadApplicationCancelled = 14,

        GetBrightness = 15,
        SetBrightness = 17,

        SetBootLogo = 30,

        TestNotif = 31,
        RequestNotif = 32,

        RequestShutdown = 100,
    }
}


// Echo (Test Method)
[ProtoContract]
public class EchoValue
{
    [ProtoMember(1)]
    public string DataValue { get; set; }
}

// Service Info
[ProtoContract]
public class ServiceInfo
{
    [ProtoMember(1, IsRequired = true)]
    public List<ServiceStats> ServiceStats { get; set; } = new();
}

// GetHardwareInformation Messages
[ProtoContract]
public class ServiceStats
{
    [ProtoMember(1)]
    public string ServiceName { get; set; }

    [ProtoMember(2)]
    public string ServiceAddress { get; set; }

    [ProtoMember(3)]
    public int ReceiveMessageCount { get; set; }

    [ProtoMember(4)]
    public int SendMessageCount { get; set; }
}

[ProtoContract]
public class GetHardwareInformationReply
{
    [ProtoMember(1)]
    public PlatformFamily PlatformFamily { get; set; }

    [ProtoMember(2)]
    public string PlatformQualifier { get; set; }

    [ProtoMember(3)]
    public string PartNumber { get; set; }

    [ProtoMember(4)]
    public string SerialNumber { get; set; }

    [ProtoMember(5)]
    public bool IsInstaller { get; set; }

    [ProtoMember(6)]
    public string ApplicationName { get; set; }

    [ProtoMember(7)]
    public string DeviceKey { get; set; }

    [ProtoMember(8)]
    public string DeviceId { get; set; }

    [ProtoMember(9)]
    public double CpuTempInC { get; set; }

    [ProtoMember(10, IsRequired = true)]
    public Dictionary<string, string> VersionInfo { get; set; } = new Dictionary<string, string>();
}

/// LoadApplication Messages
[ProtoContract]
public class LoadApplication : IFileTransmit
{
    [ProtoMember(1)]
    public string FileName { get; set; }

    [ProtoMember(2)]
    public int FileSize { get; set; }

    [ProtoMember(3)]
    public int TimeoutInMs { get; set; }

    [ProtoMember(4)]
    public bool Autoload { get; set; }
}

[ProtoContract]
public class LoadApplicationReply : IFileTransmitReply
{
    [ProtoMember(1)]
    public int UploadId { get; set; }

    [ProtoMember(2)]
    public string FileName { get; set; }

    [ProtoMember(3)]
    public int MaxBytesPerMessage { get; set; }

    [ProtoMember(4)]
    public MessageReplyStatus ReplyStatus { get; set; }

    [ProtoMember(5)]
    public string ReplyReason { get; set; }
}

[ProtoContract]
public class LoadApplicationData : IFileTransmitData
{
    [ProtoMember(1)]
    public int UploadId { get; set; }

    [ProtoMember(2)]
    public Byte[] MessageData { get; set; }

    [ProtoMember(3)]
    public int Crc32 { get; set; }
}

[ProtoContract]
public class LoadApplicationDataReply : IFileTransmitDataReply
{
    [ProtoMember(1)]
    public int UploadId { get; set; }

    [ProtoMember(4)]
    public MessageReplyStatus ReplyStatus { get; set; }

    [ProtoMember(5)]
    public string ReplyReason { get; set; }
}

[ProtoContract]
public class LoadApplicationCancelled : IFileTransmitCancelled
{
    [ProtoMember(1)]
    public int UploadId { get; set; }
}

[ProtoContract]
public class LoadApplicationCompleteEventArgs : EventArgs
{
    [ProtoMember(1)]
    public bool Autoload { get; set; }

    [ProtoMember(2)]
    public string FileName { get; set; }
}

// Brightness
[ProtoContract]
public class BrightnessInfo
{
    [ProtoMember(1)]
    public int Percentage { get; set; }

    [ProtoMember(2)]
    public bool Persist { get; set; }

    /// <summary>
    /// Default CTOR
    /// </summary>
    public BrightnessInfo()
    {
        this.Persist = true;
    }
}

public enum MessageReplyStatus
{
    Undetermined = 0,
    Error = 1,
    Success = 2,
    Timeout = 3,
    Continue = 4,
    Cancelled = 5,
}

// Echo (Test Method)
[ProtoContract]
public class ShutdownInformation
{
    [ProtoMember(1)]
    public ShutdownTypes ShutdownType { get; set; }

    [ProtoMember(2)]
    public int ShutdownCode { get; set; }
}


public enum ShutdownTypes
{
    ShutdownServices = 0,
    ShutdownApplication = 1,
    Reboot = 2,
}

[ProtoContract]
public class BootLogoInfo
{
    [ProtoMember(1)]
    public string Path { get; set; }

    [ProtoMember(2)]
    public BootLogoRotation Rotation { get; set; }
}

public enum BootLogoRotation
{
    NoRotation = 0,
    Rotate90 = 90,
    Rotate180 = 180,
    Rotate270 = 270,
}