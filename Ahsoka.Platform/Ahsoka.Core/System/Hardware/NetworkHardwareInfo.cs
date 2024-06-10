using Ahsoka.Services.Network;
using Ahsoka.Utility;
using System.Collections.Generic;

namespace Ahsoka.System.Hardware;

/// <summary>
/// Hardware Information about the Network Adapters for the Target.
/// </summary>
public class NetworkHardwareInfo : ICanDeserializeBase
{
    /// <summary>
    /// Collection of Adapter Configurations for the Hardware
    /// </summary>
    public List<AdapterInfo> AdapterInfoList { get; init; } = new List<AdapterInfo>();

    ///<inheritdoc/>
    internal override ICanDeserializeBase CreatePropertyObject(string propertyName)
    {
        if (propertyName == "adapterInfoList")
        {
            var adapter = new AdapterInfo();
            AdapterInfoList.Add(adapter);
            return adapter;
        }
        return null;
    }

}
