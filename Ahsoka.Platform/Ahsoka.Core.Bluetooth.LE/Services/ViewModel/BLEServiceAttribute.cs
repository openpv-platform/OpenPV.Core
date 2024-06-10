using System;

namespace Ahsoka.Services.Bluetooth.LE;

/// <summary>
/// Attribute used to decorate Ahsoka BLE GATT Services
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BLEServiceAttribute : Attribute
{
    /// <summary>
    /// UUID for this Service
    /// </summary>
    public string UUID { get; init; }

    /// <summary>
    /// Name of this Service
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Is Service Primary BLE Service
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Should this service be added to the Advertisement List
    /// </summary>
    public bool Advertise { get; set; }

    /// <summary>
    /// Default Constructor
    /// </summary>
    /// <param name="name"></param>
    /// <param name="uUID"></param>
    /// <param name="isPrimary"></param>
    /// <param name="advertise"></param>
    public BLEServiceAttribute(string name, string uUID, bool isPrimary = true, bool advertise = true)
    {
        UUID = uUID;
        Name = name;
        IsPrimary = isPrimary;
        Advertise = advertise;
    }
}
