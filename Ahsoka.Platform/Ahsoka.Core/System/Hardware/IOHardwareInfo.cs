using Ahsoka.Utility;
using System;
using System.Collections.Generic;
namespace Ahsoka.System.Hardware;

/// <summary>
/// Information about the Raw Physical IO Ports supported on the Hardware
/// </summary>
public partial class IOHardwareInfo : ICanDeserializeBase
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

    internal override void SetProperty(string propertyName, string value)
    {
        if (propertyName == "analogInputs")
            AnalogInputs.Add(Int32.Parse(value));
        else if (propertyName == "analogOutputs")
            AnalogOutputs.Add(Int32.Parse(value));
        if (propertyName == "digitalInputs")
            DigitalInputs.Add(Int32.Parse(value));
        else if (propertyName == "digitalOutputs")
            DigitalOutputs.Add(Int32.Parse(value));
        base.SetProperty(propertyName, value);
    }
}
