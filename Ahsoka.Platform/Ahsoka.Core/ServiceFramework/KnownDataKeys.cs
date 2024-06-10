using System.Collections.Generic;
using static NetMQ.NetMQSelector;
using System.Linq;
using System.Reflection;

namespace Ahsoka.ServiceFramework;

/// <summary>
///  List of Known Data Service Keys
/// </summary>
public abstract class KnownDataKeys
{
    internal List<string> GetDataValues()
    {
        var values = new List<string>();

        FieldInfo[] fieldInfos = this.GetType().GetFields(BindingFlags.Public |
                 BindingFlags.Static | BindingFlags.FlattenHierarchy);

        var fields = fieldInfos.Where(fi => fi.IsLiteral && !fi.IsInitOnly).ToList();

        foreach (var fieldInfo in fields)
        {
            values.Add(fieldInfo.GetRawConstantValue().ToString());
        }

        return values;
    }
}