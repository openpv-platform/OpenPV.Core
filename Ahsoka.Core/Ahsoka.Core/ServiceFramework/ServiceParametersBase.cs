using Ahsoka.Installer;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ahsoka.ServiceFramework;

/// <summary>
///  List of Known Data Service Keys
/// </summary>
public abstract class ServiceParametersBase
{
    /// <summary>
    /// Fetch all known values in the Loaded Extensions and Core
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, ServiceParameterData> GetAllValues(PackageInformation info = null)
    {
        Dictionary<string, ServiceParameterData> allValues = new ();

        foreach (var value in ClassLoader.GetSubclasses(typeof(AhsokaMessagesBase)))
        {
            AhsokaMessagesBase service = ClassLoader.CreateType(value,null) as AhsokaMessagesBase;
            var values = service.GetServiceParameters();
            if (values != null)
            {
                var parms = values.GetParameters(info);
                allValues.Add(parms.Service, parms);
            }
        }

        return allValues;
    }

    /// <summary>
    /// Gets a list of Parameters Available to the System
    /// </summary>
    /// <returns></returns>
    internal ServiceParameterData GetParameters(PackageInformation info)
    {
        var values = new ServiceParameterData();
       
        OnGetParameters(out string service, values, info);
        AssignKeys(service, values);

        return values;
    }

    private void AssignKeys(string service, ServiceParameterData values)
    {
        values.Service = service;

        foreach (var parameter in values)
        {
            if (parameter.SystemUniqueID == 0)
            {
                // Generate Key (32bit Hash
                int value = HashCode32.GetHashCode32(service +  parameter.Name);
                while (values.Any(x => x.SystemUniqueID == value))
                    value++;
                parameter.SystemUniqueID = value;
            }
        }
    }

    /// <summary>
    /// Pupulate the Parameters Availabe in the System
    /// </summary>
    /// <param name="service"></param>
    /// <param name="values"></param>
    /// <param name="packageInfo"></param>
    protected abstract void OnGetParameters(out string service, List<ParameterData> values, PackageInformation packageInfo);

}

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
