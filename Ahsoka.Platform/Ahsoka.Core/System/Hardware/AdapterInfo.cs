using Ahsoka.Utility;
using System;

namespace Ahsoka.Services.Network;

/// <summary>
/// Adapter Information 
/// </summary>
public partial class AdapterInfo : ICanDeserializeBase
{
    /// <summary>
    /// Decode Object for JsonDeserialization
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="value"></param>
    internal override void SetProperty(string propertyName, string value)
    {
        if (propertyName == "adapterId")
            this.AdapterId = value;
        else if (propertyName == "adapterType")
            AdapterType = Enum.Parse<NetworkAdapterType>(value, true);
        else if (propertyName == "module")
            Module = Enum.Parse<ModuleType>(value, true);
        else if (propertyName == "description")
            Description = value;
    }

}