using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace Ahsoka.Core.Hardware;

/// <summary>
/// Information about the Raw Physical IO Ports supported on the Hardware
/// </summary>
internal partial class CANHardwareInfo
{
    /// <summary>
    /// Collection of Module / Adapter Information Objects.
    /// </summary>
    [JsonInclude]
    public List<CanPort> CANPorts { get; init; } = new List<CanPort>();
}


/// <summary>
/// Can Port Info
/// </summary>
internal class CanPort
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


}