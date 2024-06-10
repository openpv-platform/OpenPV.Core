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

    /// <summary>
    /// Default Timeout for Discovery Calls
    /// </summary>
    public int DiscoveryTimeout { get; set; } = 10000;

    /// <summary>
    /// Default Timeout for Pairing Requests
    /// </summary>
    public int PairingTimeout { get; set; } = 5000;

    /// <summary>
    /// Default Timeout for Connection Requests.
    /// </summary>
    public int ConnectionTimeout { get; set; } = 5000;


    internal override void SetProperty(string propertyName, string value)
    {
        if (propertyName == "index")
            Index = Int32.Parse(value);
        else if (propertyName == "deviceInterfacePath")
            DeviceInterfacePath = value;
        else if (propertyName == "stack")
            Stack = Enum.Parse<Stack>(value, true);
        else if (propertyName == "connectionTimeout")
            ConnectionTimeout = Int32.Parse(value);
        else if (propertyName == "pairingTimeout")
            PairingTimeout = Int32.Parse(value);
        else if (propertyName == "discoveryTimeout")
            DiscoveryTimeout = Int32.Parse(value);

    }
}

/// <summary>
/// Types of Bluetooth Stacks used in the System
/// </summary>
public enum Stack
{
    /// <summary>
    /// Built in Microsoft Bluetooth Stack for Windows
    /// </summary>
    Windows,

    /// <summary>
    /// Bluez Stack using DBUS Interface.
    /// </summary>
    Bluez,
}