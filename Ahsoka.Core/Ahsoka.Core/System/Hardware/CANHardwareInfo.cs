using Ahsoka.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
namespace Ahsoka.System.Hardware;

/// <summary>
/// Information about the Raw Physical IO Ports supported on the Hardware
/// </summary>
internal partial class CANHardwareInfo : ICanDeserializeBase
{
    /// <summary>
    /// Collection of Module / Adapter Information Objects.
    /// </summary>
    [JsonProperty]
    public List<CanPort> CANPorts { get; init; } = new List<CanPort>();
}


/// <summary>
/// Can Port Info
/// </summary>
internal class CanPort : ICanDeserializeBase
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