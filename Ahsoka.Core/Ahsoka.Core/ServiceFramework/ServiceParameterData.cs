using Ahsoka.Installer;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// Collection of Known Parameter Info for a Service
/// </summary>
public class ServiceParameterData : List<ParameterData>
{
    /// <summary>
    /// Name of the Parameter
    /// </summary>
    public string Service { get; set; }
}

/// <summary>
/// Information about a Parameter
/// </summary>
public class ParameterData
{
    /// <summary>
    /// Name of the Parameter
    /// </summary>
    public int SystemUniqueID { get; set; }

    /// <summary>
    /// Name of the Parameter
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Minimum Value
    /// </summary>
    public double MinimumValue { get; set; }

    /// <summary>
    /// Minimum Value
    /// </summary>
    public double MaximumValue { get; set; }

    /// <summary>
    /// Minimum Value
    /// </summary>
    public object DefaultValue { get; set; }

    /// <summary>
    /// Minimum Value
    /// </summary>
    public ParameterValueTypes ValueType { get; set; }

    /// <summary>
    /// Valid Values
    /// </summary>
    public Dictionary<int, string> Enumerations { get; set; }
}

/// <summary>
///  Value Types
/// </summary>
public enum ParameterValueTypes
{
    /// <summary>
    /// String Values
    /// </summary>
     String,

     /// <summary>
     /// Unsigned Integer
     /// </summary>
     UnsignedInteger,

     /// <summary>
     /// Signed Integer
     /// </summary>
     SignedInteger,

     /// <summary>
     /// Bool Value
     /// </summary>
     Boolean,

     /// <summary>
     /// Single Precision Float
     /// </summary>
     Float,

     /// <summary>
     /// Double Precision Float
     /// </summary>
     Double,
}
