using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ahsoka.Core.Hardware;

/// <summary>
/// Information about the Bluetooth Modules available in the system.
/// </summary>
internal class BluetoothHardwareInfo
{
    /// <summary>
    /// Collection of Module / Adapter Information Objects.
    /// </summary>
    [JsonInclude]
    public List<BluetoothHardwareAdapter> BluetoothAdapters { get; init; } = new List<BluetoothHardwareAdapter>();
}

/// <summary>
/// Bluetooth Adapter Information indicating the Device Paths and Default values.
/// </summary>
internal class BluetoothHardwareAdapter
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

}

/// <summary>
/// Types of Bluetooth Stacks used in the System
/// </summary>
internal enum Stack
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