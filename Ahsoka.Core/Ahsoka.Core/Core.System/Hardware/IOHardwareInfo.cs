using System.Collections.Generic;
namespace Ahsoka.Core.Hardware;

/// <summary>
/// Information about the Raw Physical IO Ports supported on the Hardware
/// </summary>
internal partial class IOHardwareInfo
{
    /// <summary>
    /// List of Analog Input ID's
    /// </summary>
    public List<int> AnalogInputs { get; set; } = new List<int>();

    /// <summary>
    /// List of Analog Output ID's
    /// </summary>
    public List<int> AnalogOutputs { get; set; } = new List<int>();

    /// <summary>
    /// List of Digital Input ID's
    /// </summary>
    public List<int> DigitalInputs { get; set; } = new List<int>();

    /// <summary>
    /// List of Digital Output ID's
    /// </summary>
    public List<int> DigitalOutputs { get; set; } = new List<int>();

}
