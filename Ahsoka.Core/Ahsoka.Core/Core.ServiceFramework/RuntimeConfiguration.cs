using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ahsoka.Core;

/// <summary>
/// High Level Support Category
/// </summary>
public enum PlatformFamily
{
    /// <summary>
    /// Supports Any Family
    /// </summary>
    Any,

    /// <summary>
    /// Supports Windows Intel 64bit
    /// </summary>
    Windows64 = 2,

    /// <summary>
    /// Supports macOS Arm 64Bit
    /// </summary>
    MacOSArm64 = 3,

    /// <summary>
    /// Supports Select Series (32bit Arm Linux)
    /// </summary>
    OpenViewLinux = 4,

    /// <summary>
    /// Supports Pro Series (64bit Arm Linux)
    /// </summary>
    OpenViewLinuxPro = 5,

    /// <summary>
    /// Supports Ubuntu on Intel 64bit
    /// </summary>
    Ubuntu64 = 8,

    /// <summary>
    /// Reserved for Future Platforms
    /// </summary>
    Reserved = 16,

    /// <summary>
    /// Supports PowerVision (QNX) Displays
    /// </summary>
    PowerVision = 32
}

/// <summary>
/// Configuration for an OpenPV Service Impelmentation
/// </summary>
[ProtoContract]
public partial class RuntimeConfiguration
{
    /// <summary>
    /// Indicates if Start Script Should Auto Start a Runtime 
    /// </summary>
    public StartOptions ServiceStartup { get; set; }

    /// <summary>
    /// Indicates if the Start Script should Release the System D Unit (Ahsoka.Application)
    /// </summary>
    public StartOptions ReleaseOption { get; set; }

    /// <summary>
    /// Info around Extensions Used in the System
    /// </summary>
    public List<ExtensionInfo> ExtensionInfo { get; set; }

    /// <summary>
    /// Delay Before Starting Services or Releaseing SystemD
    /// </summary>
    public Int32 StartupDelay { get; set; }

    /// <summary>
    /// Key Used for Securing Services 
    /// </summary>
    public string ServiceKey { get; set; }

    /// <summary>
    /// Should Security Service should issue a local Challenge before accepting Calls.
    /// </summary>
    public ChallengeOptions RequireChallenge { get; set; }

    /// <summary>
    /// List of Configured Services
    /// </summary>
    [ProtoMember(1, IsRequired = true)]
    public List<ServiceConfiguration> Services { get; set; } = new();

    /// <summary>
    /// Sets Defaults for Runtime Configurations
    /// </summary>
    public RuntimeConfiguration()
    {
        this.StartupDelay = 3;

    }
}

/// <summary>
/// Options for Security Service
/// </summary>
public enum ChallengeOptions
{
    /// <summary>
    /// Don't require local Challenge
    /// </summary>
    NotRequired,

    /// <summary>
    /// Require a local challenge for Remote Clients
    /// </summary>
    RequireDisplayChallenge
}


/// <summary>
/// Configuration for an OpenPV Service
/// </summary>
[ProtoContract]
public partial class ServiceConfiguration
{
    /// <summary>
    /// Name of the Service
    /// </summary>
    [ProtoMember(2)]
    public string ServiceName { get; set; }

    /// <summary>
    /// TCP Listen Address (Typically * or LocalHost)
    /// </summary>
    [ProtoMember(3)]
    public string TcpListenAddress { get; set; } = string.Empty;

    /// <summary>
    /// TCP Connection Address
    /// </summary>
    [ProtoMember(4)]
    public string TcpConnectionAddress { get; set; } = string.Empty;

    /// <summary>
    ///  Data Port for this Service
    /// </summary>
    [ProtoMember(5)]
    public int DataChannel { get; set; }

    /// <summary>
    /// Is Service an Extension
    /// </summary>
    [ProtoMember(8)]
    public bool IsExtension { get; set; }

    /// <summary>
    /// Configurable Behaviors for the Service
    /// </summary>
    [ProtoMember(10)]
    public EndPointBehaviors Behaviors { get; set; }

    /// <summary>
    /// Produces the TCP URI for the data communication channel that is typically used by sockets utilizing ZMQ as the
    /// backing transport.
    /// </summary>
    [JsonIgnore]
    public string DataURI
    {
        get
        {
            return $"tcp://{this.TcpConnectionAddress}:{this.DataChannel}";
        }
    }

    /// <summary>
    /// Produces the TCP URI for the data listening channel that is typically used by sockets utilizing ZMQ as the
    /// backing transport.
    /// </summary>
    [JsonIgnore]
    public string DataListenURI
    {
        get
        {
            return $"tcp://{this.TcpListenAddress}:{this.DataChannel}";
        }
    }
}


/// <summary>
/// Security Configuration for the Service Runtime
/// </summary>
[ProtoContract]
public partial class SecurityConfiguration
{
    /// <summary>
    /// Does Application Require a Local Challenge
    /// </summary>
    [ProtoMember(1)]
    public ChallengeOptions RequireChallenge { get; set; }

    /// <summary>
    /// Unique Application ID
    /// </summary>
    [ProtoMember(2)]
    public byte[] ApplicationId { get; set; }

    /// <summary>
    /// Public Key for Validating Service Access
    /// </summary>
    [ProtoMember(3)]
    public string TokenPublicKey { get; set; }

    /// <summary>
    /// Private Key for Generating API Keys (if Available)
    /// </summary>
    [ProtoMember(4)]
    public string TokenPrivateKey { get; set; }
}

/// <summary>
/// Optional EndPoint Behaviors
/// </summary>
public enum EndPointBehaviors
{
    /// <summary>
    /// None
    /// </summary>
    NoBehavior,

    /// <summary>
    /// AutoStart a Service Instance when Client Starts
    /// </summary>
    AutoStart,

    /// <summary>
    /// Secure this Service with an API Key
    /// </summary>
    SecurityEnabled,
}


/// <summary>
/// Startup Options
/// </summary>
public enum StartOptions
{
    /// <summary>
    /// Don't AutoStart Services or Release SystemD
    /// </summary>
    NoAutoStart,

    /// <summary>
    /// Run Before App Start
    /// </summary>
    BeforeApplicationStart,

    /// <summary>
    /// Run After SystemD
    /// </summary>
    AfterApplicationStart,
}

/// <summary>
/// Enable or Disable this Service
/// </summary>
public enum ServiceEnableDisable
{
    /// <summary>
    /// Disabled
    /// </summary>
    Disabled,

    /// <summary>
    /// Enabled
    /// </summary>
    Enabled,
}


/// <summary>
/// Extension Information
/// </summary>
public partial class ExtensionInfo
{
    /// <summary>
    /// Extension Name
    /// </summary>
    public string ExtensionName { get; set; }

    /// <summary>
    /// Specific Version
    /// </summary>
    public string SpecificVersion { get; set; }

    /// <summary>
    /// Specific Version
    /// </summary>
    public string ConfigurationFile { get; set; }
}



