using System;
using System.Collections.Generic;
using Ahsoka.Utility;

namespace Ahsoka.Services.Bluetooth.LE;

/// <summary>
/// Attribute used to Decorate Service Characteristic Values
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class BLECharacteristicAttribute : ViewModelPropertyBaseAttribute
{
    /// <summary>
    /// Characteristic UUID
    /// </summary>
    public string UUID { get; init; }

    /// <summary>
    /// Characteristic Name
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Flags
    /// </summary>
    public List<BLECharacteristicFlag> Flags { get; init; }

    /// <summary>
    /// Default Constructor
    /// </summary>
    /// <param name="name"></param>
    /// <param name="uUID"></param>
    /// <param name="flags"></param>
    public BLECharacteristicAttribute(string name, string uUID, params BLECharacteristicFlag[] flags)
    {
        UUID = uUID;
        Flags = new(flags);
        Name = name;
    }
}

/// <summary>
/// Attribute for Specifying a Read Method
/// Method should accept BLECharacteristicValue as its only argument.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class BLECharacteristicReadMethodAttribute : BLECharacteristicAttribute
{
    ///<inheritdoc/>
    public BLECharacteristicReadMethodAttribute(string name, string uUID, params BLECharacteristicFlag[] flags) :
        base(name, uUID, flags)
    {

    }
}

/// <summary>
/// Attribute for Specifying a Write Method
/// Method should accept BLECharacteristicValue as its only argument.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class BLECharacteristicWriteMethodAttribute : BLECharacteristicAttribute
{
    ///<inheritdoc/>
    public BLECharacteristicWriteMethodAttribute(string name, string uUID, params BLECharacteristicFlag[] flags) :
        base(name, uUID, flags)
    {

    }
}