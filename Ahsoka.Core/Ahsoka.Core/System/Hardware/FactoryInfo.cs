using Ahsoka.Utility;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Ahsoka.System.Hardware;

/// <summary>
/// Structure used for passing Factory Prep data to the command line in production.
/// </summary>
public class ManufacturingInfo : ICanDeserializeBase
{
    /// <summary>
    /// Mac Address for the Display
    /// </summary>
    public string MacAddress { get; set; }

    /// <summary>
    /// Part Number for the Display
    /// </summary>
    public string PartNumber { get; set; }

    /// <summary>
    ///  Serial Number for the Display
    /// </summary>
    public string SerialNumber { get; set; }

    /// <inheritdoc/>
    internal override void SetProperty(string propertyName, string value)
    {
        if (propertyName == "macAddress")
            this.MacAddress = value;
        else if (propertyName == "serialNumber")
            this.SerialNumber = value;
        else if (propertyName == "partNumber")
            this.PartNumber = value;
    }
}

/// <summary>
/// Factory Information Stored in the Factory / Hardware Folder and stored in production and during prep.
/// Contains OTP data about the device.
/// </summary>
public class FactoryInfo : ManufacturingInfo
{
    /// <summary>
    /// Platform Family for the Device - Typically cooresponds to the OS.
    /// </summary>
    public PlatformFamily PlatformFamily { get; set; }

    /// <summary>
    ///  Qualifier for the Device - Typically cooresponds to the major variant class such as S50, S70 
    /// </summary>
    public string PlatformQualifier { get; set; }

    /// <summary>
    /// Unique ID for the Device used for tracing the device in various systems (Cloud, OTP, etc.)
    /// </summary>
    public Guid DeviceId { get; set; }

    /// <summary>
    /// Combination of Family and Qualifier used for single view lists and selectinos.
    /// </summary>
    [JsonIgnore]
    public string PlatformDescription { get { return $"{PlatformFamily} ({PlatformQualifier})"; } }

    /// <summary>
    /// Hardware Model is a free form description of teh hardware available on some platforms.   Such as "Raspberry Pi" or "Dell Latitude PC"
    /// </summary>
    public string HardwareModel { get; set; }

    /// <summary>
    ///  Customer Specified Value used in Special Custom Hardware Variations.
    /// </summary>
    public string SecurityModelNumber { get; set; }

    /// <summary>
    /// Location of the Ahsoka Root Folder on this target platfrom. 
    /// </summary>
    public string RootPath { get; set; }

    /// <summary>
    /// Generates a Device ID from the Unique Hardware ID (typically the Processor Serial Number)
    /// returns the same GUID for a give piece of hardware based on the MD5 Hash of the Hardware ID.
    /// </summary>
    /// <param name="platformFamily"></param>
    /// <param name="hardwareID"></param>
    /// <returns></returns>
    internal static Guid CreateDeviceId(PlatformFamily platformFamily, string hardwareID)
    {
        string id = platformFamily + hardwareID;
        byte[] hash = MD5.HashData(new UTF8Encoding().GetBytes(id));
        return new Guid(hash);
    }

    /// <inheritdoc/>
    internal override void SetProperty(string propertyName, string value)
    {
        if (propertyName == "deviceId")
            this.DeviceId = Guid.Parse(value);
        else if (propertyName == "platformFamily")
            this.PlatformFamily = Enum.Parse<PlatformFamily>(value);
        else if (propertyName == "platformQualifier")
            this.PlatformQualifier = value;
        else if (propertyName == "rootPath")
            this.RootPath = value;
        else if (propertyName == "hardwareModel")
            this.HardwareModel = value;
        else if (propertyName == "securityModelNumber")
            this.SecurityModelNumber = value;
        else
            base.SetProperty(propertyName, value);
    }
}


