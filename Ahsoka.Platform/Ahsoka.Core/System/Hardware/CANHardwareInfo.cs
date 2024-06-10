using Ahsoka.Utility;
using System;
using System.Collections.Generic;
namespace Ahsoka.System.Hardware;

/// <summary>
/// Information about the Raw Physical IO Ports supported on the Hardware
/// </summary>
public partial class CANHardwareInfo : ICanDeserializeBase
{
    /// <summary>
    /// Collection of Module / Adapter Information Objects.
    /// </summary>
    public List<CanPort> CANPorts { get; init; } = new List<CanPort>();

    internal override ICanDeserializeBase CreatePropertyObject(string propertyName)
    {
        if (propertyName == "canPorts")
        {
            var obj = new CanPort();
            CANPorts.Add(obj);
            return obj;
        }

        return null;
    }
}


/// <summary>
/// Can Port Info
/// </summary>
public class CanPort : ICanDeserializeBase
{
    /// <summary>
    /// Adapter Index
    /// </summary>
    public uint Port { get; set; }

    /// <summary>
    /// Unique Path for this Adapter
    /// </summary>
    public string SocketCanInterfacePath { get; set; }

    /// <summary>
    /// Unique Path for this Adapter
    /// </summary>
    public string CoprocessorSerialPath { get; set; }

    /// <summary>
    /// Unique Path for this Adapter
    /// </summary>
    public string CoprocessorFirmwarePath { get; set; }


    internal override void SetProperty(string propertyName, string value)
    {
        if (propertyName == "port")
            Port = UInt32.Parse(value);
        else if (propertyName == "socketCanInterfacePath")
            SocketCanInterfacePath = value;
        else if (propertyName == "coprocessorSerialPath")
            CoprocessorSerialPath = value;
        else if (propertyName == "coprocessorFirmwarePath")
            CoprocessorFirmwarePath = value;
    }
}