using Ahsoka.Services.Network;
using System.Collections.Generic;

namespace Ahsoka.Core.Hardware;

/// <summary>
/// Hardware Information about the Network Adapters for the Target.
/// </summary>
public class NetworkHardwareInfo
{
    /// <summary>
    /// Collection of Adapter Configurations for the Hardware
    /// </summary>
    public List<AdapterInfo> AdapterInfoList { get; init; } = new List<AdapterInfo>();


}
