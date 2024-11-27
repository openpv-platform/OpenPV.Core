using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ahsoka.ServiceFramework;

/// <summary>
/// Collection of Known Parameter Info for a Service
/// </summary>
public class SystemParameterData : List<ParameterData>
{
    /// <summary>
    /// Name of the Parameter
    /// </summary>
    public string SystemName { get; set; } = string.Empty;

    /// <summary>
    /// Assign Service and Keys to All Objects
    /// </summary>
    /// <param name="service"></param>
    public void AssignKeys(string service = null)
    {
        if (service != null)
            SystemName = service;

        foreach (var parameter in this)
        {
            if (parameter.SystemUniqueID == 0)
            {
                // Generate Key (32bit Hash
                var value  = ParameterData.GenerateUniqueID(SystemName, parameter.ObjectName, parameter.Name);
                while (this.Any(x => x.SystemUniqueID == value))
                    value++;

                parameter.SystemUniqueID = value;
            }
        }
    }
}

/// <summary>
/// Information about a Parameter
/// </summary>
public class ParameterData
{
    /// <summary>
    /// Name of the Parameter
    /// </summary>
    public uint SystemUniqueID { get; set; }

    /// <summary>
    /// Name of the Object (Optional)
    /// </summary>
    public string ObjectName { get; set; }
    
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

    /// <summary>
    /// Convert Types to Parameter Types
    /// </summary>
    /// <param name="propertyType"></param>
    /// <returns></returns>
    public static ParameterValueTypes GetValueType(Type propertyType)
    {
        if (propertyType == typeof(int) || propertyType == typeof(long) || propertyType == typeof(short) || propertyType == typeof(sbyte))
            return ParameterValueTypes.SignedInteger;
        else if (propertyType == typeof(uint) || propertyType == typeof(ulong) || propertyType == typeof(ushort) || propertyType == typeof(byte))
            return ParameterValueTypes.UnsignedInteger;
        else if (propertyType == typeof(float))
            return ParameterValueTypes.Float;
        else if (propertyType == typeof(double))
            return ParameterValueTypes.Double;
        else if (propertyType == typeof(string))
            return ParameterValueTypes.String;
        else if (propertyType == typeof(bool))
            return ParameterValueTypes.Boolean;
        else
            return ParameterValueTypes.String; // Default Case
    }

    /// <summary>
    /// Generate a Unique ID For an System / Object / Property
    /// </summary>
    /// <param name="systemName"></param>
    /// <param name="objectName"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static uint GenerateUniqueID(string systemName, string objectName, string name)
    {
        return (uint)HashCode32.GetHashCode32($"{systemName}_{objectName}_{name}");
    }
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
