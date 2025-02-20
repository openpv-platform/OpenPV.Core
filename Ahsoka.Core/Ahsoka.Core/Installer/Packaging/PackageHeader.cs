#pragma warning disable CS1591
using Ahsoka.Core;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

namespace Ahsoka.Installer;

[ProtoContract(IgnoreUnknownSubTypes = true)]
public class PackageHeader
{
    [ProtoMember(1)]
    [DefaultValue(PackageType.opvEncrypted)]
    public PackageType PackageType { get; set; } = PackageType.opvEncrypted;

    [ProtoMember(2)]
    [DefaultValue(1)]
    public int PackageStructureVersion { get; set; } = 1;

    [ProtoMember(3)]
    public PlatformFamily PlatformFamily { get; set; }

    [ProtoMember(4)]
    public string PlatformQualifier { get; set; }

    [ProtoMember(5)]
    [DefaultValue(SecurityModes.None)]
    public SecurityModes SecurityMode { get; set; } = SecurityModes.None;

    [ProtoMember(6)]
    [JsonIgnore]
    internal byte[] SecurityPassKeyData { get; set; }
    public string SecurityPassKey { get; set; }

    [ProtoMember(7)]
    [JsonIgnore]
    internal byte[] SecurityModelNumberData { get; set; }
    public string SecurityModelNumber { get; set; }

    [ProtoMember(8)]
    [JsonIgnore]
    internal byte[] SecuritySerialNumberData { get; set; }
    public string SecuritySerialNumber { get; set; }

    [ProtoMember(9, IsRequired = true)]
    public Guid ApplicationId { get; set; } = Guid.Empty;

    [ProtoMember(100, IsRequired = true)]
    public List<PackageComponent> Components { get; set; } = new List<PackageComponent>();
}


[ProtoContract()]
public class PackageComponent
{
    [ProtoMember(1)]
    internal string ComponentTypeId { get; set; }

    [ProtoMember(2)]
    public string Version { get; set; }

    [ProtoMember(3)]
    public Int64 DataSize { get; set; }

    [ProtoMember(4)]
    public uint DataCrc { get; set; }

    [ProtoMember(5, IsRequired = true)]
    public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();

    // Runtime Values
    public Guid ComponentType
    {
        get { return Guid.Parse(ComponentTypeId); }
        set { ComponentTypeId = value.ToString(); }
    }


    public Stream Data { get; set; }

    internal long DataFileOffset { get; set; }
}


public enum PackageType
{
    unknown = 0,
    opvEncrypted = 1,
    opvClear = 2,
}

public enum ApplicationType
{
    Unknown = 0,
    Script = 1,
    Dotnet = 2,
    Cpp = 4,
    PowerVision = 6,
}

public enum SecurityModes
{
    None = 0,
    PassKeyOnInstall = 1,
    PassKeyOnOverwrite = 2,
    ModelNumberLocked = 4,
    SerialNumberLocked = 8,
    UnencryptedFile = 16,
}

public static class PlatformExtensions
{
    public static string GetFileExtension(this PackageType packageType, bool includeSeparator = true)
    {
        var sb = new StringBuilder();

        if (includeSeparator)
            sb.Append('.');

        if (packageType == PackageType.opvEncrypted)
            sb.Append("opv");
        else
            return string.Empty;

        return sb.ToString();
    }

    public static string GetRuntimeId(this PlatformFamily family)
    {
        return family switch
        {
            PlatformFamily.Windows64 => "win",
            PlatformFamily.MacOSArm64 => "macOS",
            PlatformFamily.OpenViewLinux => "linux",
            _ => "any",
        };
    }

}