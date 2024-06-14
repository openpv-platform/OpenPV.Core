using Ahsoka.Utility;
using System;
using System.Collections.Generic;

namespace Ahsoka.System.Hardware;

/// <summary>
/// Information about the Bluetooth Modules available in the system.
/// </summary>
public class BluetoothHardwareInfo : ICanDeserializeBase
{
    /// <summary>
    /// Collection of Module / Adapter Information Objects.
    /// </summary>
    public List<BluetoothHardwareAdapter> BluetoothAdapters { get; init; } = new List<BluetoothHardwareAdapter>();

    internal override ICanDeserializeBase CreatePropertyObject(string propertyName)
    {
        if (propertyName == "bluetoothAdapters")
        {
            var obj = new BluetoothHardwareAdapter();
            BluetoothAdapters.Add(obj);
            return obj;
        }

        return null;
    }
}

/// <summary>
/// Bluetooth Adapter Information indicating the Device Paths and Default values.
/// </summary>
public class BluetoothHardwareAdapter : ICanDeserializeBase
{
    /// <summary>
    /// Adapter Index
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Unique Path for this Adapter
    /// </summary>
    public string DeviceInterfacePath { get; set; }

    /// <summary>
    /// Type of Bluetooth Stack used on this Adapter
    /// </summary>
    public Stack Stack { get; set; }

    internal override void SetProperty(string propertyName, string value)
    {
        if (propertyName == "index")
            Index = Int32.Parse(value);
        else if (propertyName == "deviceInterfacePath")
            DeviceInterfacePath = value;
        else if (propertyName == "stack")
            Stack = Enum.Parse<Stack>(value, true);
    }
}

/// <summary>
/// Types of Bluetooth Stacks used in the System
/// </summary>
public enum Stack
{
    /// <summary>
    /// Simulated Bluetooth Stack for Desktop / Pipeline Testing
    /// </summary>
    Desktop,

    /// <summary>
    /// Bluez Stack using DBUS Interface.
    /// </summary>
    Bluez,
}