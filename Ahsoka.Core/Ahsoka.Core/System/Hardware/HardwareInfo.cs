using Ahsoka.Utility;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Ahsoka.System.Hardware;

/// <summary>
/// Root Class for Accessing Hardware Information including supported Peripherals and Platform Information
/// </summary>
public partial class HardwareInfo : ICanDeserializeBase
{
    /// <summary>
    /// The Family the platform belongs to.  Typically cooresponds to the OS it runs
    /// Matches FactoryInfo
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public PlatformFamily PlatformFamily { get { return FactoryInfo.PlatformFamily; } }

    /// <summary>
    /// The Qualifier for the platform.   Typically refers to a high level variant such as S50 (5") or S70 (7")
    /// Matches FactoryInfo
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public string PlatformQualifier { get { return FactoryInfo.PlatformQualifier; } }

    /// <summary>
    /// Combination of the Family and Qualifier
    /// Matches FactoryInfo
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public string PlatformDescription { get { return FactoryInfo.PlatformDescription; } }

    /// <summary>
    /// Information about the Platform and Factory Defined Information such as Serial Number / Part Number. 
    /// </summary>
    public FactoryInfo FactoryInfo { get; set; }

    /// <summary>
    /// Information about the supported Bluetooth Modules on the system.
    /// </summary>
    [JsonProperty]
    internal BluetoothHardwareInfo BluetoothInfo { get; set; }

    /// <summary>
    /// Information about the supported Network Interfaces on the system.
    /// </summary>
    public NetworkHardwareInfo NetworkInfo { get; set; }

    /// <summary>
    /// Information about the support Raw IO for the system.
    /// </summary>
    [JsonProperty]
    internal IOHardwareInfo IOInfo { get; set; }

    /// <summary>
    /// Information about the support Raw IO for the system.
    /// </summary>
    [JsonProperty]
    internal CANHardwareInfo CANInfo { get; set; }

    /// <summary>
    /// Information about the support Raw IO for the system.
    /// </summary>
    public ScreenInfo ScreenInfo { get; set; } = new ScreenInfo() { ScreenHeight = 480, ScreenWidth = 800 };

    /// <summary>
    /// Information about the location of various Paths on the system.
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public TargetPathInfo TargetPathInfo
    {
        get; init;
    }

    /// <summary>
    /// Default Constructor
    /// </summary>
    public HardwareInfo()
    {
        TargetPathInfo = new(this);
    }

    /// <summary>
    /// Fetches the signing key for the system for use in validation and encryption
    /// </summary>
    /// <param name="publicKeyOnly"></param>
    /// <returns></returns>
    internal string GetDeviceSigningKey(bool publicKeyOnly = true)
    {
        // Convert Public Keys to PEM if Not Available
        string publicPEMPath = TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningPublicKeyPEMFile);
        string publicXMLPath = TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningPublicKeyFile);
        if (!File.Exists(publicPEMPath) && File.Exists(publicXMLPath))
        {
            string data = File.ReadAllText(publicXMLPath);
            File.WriteAllText(publicPEMPath, RSAEncryption.ConvertXMLToPublicPEM(data));
        }

        // Convert PRivate Keys to PEM if Not Available
        string privatePEMPath = TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningKeyPEMFile);
        string privateXMLPath = TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningKeyFile);
        if (!File.Exists(privatePEMPath) && File.Exists(privateXMLPath))
        {
            string data = File.ReadAllText(TargetPathInfo.GetFactoryPath(FactoryPaths.HardwareDeviceSigningKeyFile));
            File.WriteAllText(privatePEMPath, RSAEncryption.ConvertXMLToPrivatePEM(data));
        }

        // Fetch Correct Key
        var deviceFile = publicKeyOnly ? publicPEMPath : privatePEMPath;

        if (File.Exists(deviceFile))
            return File.ReadAllText(deviceFile);
        else
            return null;
    }

    /// <inheritdoc />
    internal override ICanDeserializeBase CreatePropertyObject(string propertyName)
    {
        if (propertyName == "factoryInfo")
            return FactoryInfo = new();
        else if (propertyName == "networkInfo")
            return NetworkInfo = new();
        else if (propertyName == "screenInfo")
            return ScreenInfo = new();

        return null;
    }
}

/// <summary>
/// Static Information about the Factory Folder
/// </summary>
internal class FactoryManifest
{
    public string DeviceId { get; set; }

    public string DevicePublicKey { get; set; }

    public List<FactoryManifestFileInfo> FileInfo { get; init; } = new List<FactoryManifestFileInfo>();
}

/// <summary>
/// Information about a single file in the Factory Hardware Folder
/// </summary>
internal class FactoryManifestFileInfo
{
    public string Path { get; set; }
    public string CheckSum { get; set; }
}

/// <summary>
/// Interface for specifing information needed to connect to a target
/// </summary>
public interface IConnectionInfo
{
    /// <summary>
    /// Host Name or IP Address of the Target
    /// </summary>
    string HostName { get; set; }

    /// <summary>
    /// User Name for the Target.  Note that Passwords are not used as Secure Targets use a Private Key File.
    /// </summary>
    string UserName { get; set; }

    /// <summary>
    /// Path to the Private Key to be used when communiating with the target over SSH, sFTP or SCP.
    /// </summary>
    string PrivateKeyFile { get; }

    /// <summary>
    ///  Optional Port for Connections over SSH 
    /// </summary>
    int Port { get; set; }
}

/// <summary>
/// Interface for specifying information needed to connect to a specific target platform instance.
/// </summary>
public interface ITargetConnectionInfo : IConnectionInfo
{
    /// <summary>
    /// Platform Family, used for looking up Hardware Information Default Values
    /// </summary>
    public PlatformFamily PlatformFamily { get; set; }

    /// <summary>
    /// Platform Qualifier, used for looking up Hardware Information Default Values
    /// </summary>
    public string PlatformQualifier { get; set; }
}

/// <summary>
/// Connection structure used for Command Line Methods which require Connection Information.
/// </summary>
public class TargetConnectionInfo : ITargetConnectionInfo
{
    /// <summary>
    /// Platform Family, used for looking up Hardware Information Default Values
    /// </summary>
    public PlatformFamily PlatformFamily { get; set; }

    /// <summary>
    /// Platform Qualifier, used for looking up Hardware Information Default Values
    /// </summary>
    public string PlatformQualifier { get; set; }

    /// <summary>
    /// Host Name or IP Address of the Target
    /// </summary>
    public string HostName { get; set; }

    /// <summary>
    /// User Name for the Target.  Note that Passwords are not used as Secure Targets use a Private Key File.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Path to the Private Key to be used when communiating with the target over SSH, sFTP or SCP.
    /// </summary>
    public string PrivateKeyFile { get; set; }

    /// <summary>
    /// Port
    /// </summary>
    public int Port { get; set; }
}

