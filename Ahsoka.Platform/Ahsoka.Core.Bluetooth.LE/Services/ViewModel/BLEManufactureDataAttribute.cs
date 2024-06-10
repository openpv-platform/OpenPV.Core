using System;

namespace Ahsoka.Services.Bluetooth.LE;

/// <summary>
/// Attribute used to Decorate Service Characteristic Values
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BLEManufactureDataAttribute : Attribute
{
    /// <summary>
    /// Data Key
    /// </summary>
    public ushort Key { get; init; }

    /// <summary>
    /// Data Values
    /// </summary>
    public Byte[] Data { get; init; }

    /// <summary>
    /// Default Constructor
    /// </summary>
    /// <param name="key"></param>
    /// <param name="data"></param>
    public BLEManufactureDataAttribute(ushort key, params byte[] data)
    {
        Key = key;
        Data = data;
    }
}