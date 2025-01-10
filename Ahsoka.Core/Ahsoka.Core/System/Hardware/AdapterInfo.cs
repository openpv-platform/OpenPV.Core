using Ahsoka.Utility;
using EnovationCloud.Core.DataTransferObjects;
using Newtonsoft.Json;
using System;

namespace Ahsoka.Services.Network;

/// <summary>
/// Adapter Information 
/// </summary>
public partial class AdapterInfo : ICanDeserializeBase
{
    // These are left for Back Compatiblity
    [JsonProperty]
    internal string AdapterId { get; set; }

    [JsonProperty]
    internal string AdapterType { get; set; }

    [JsonProperty]
    internal string Module { get; set; }

    [JsonProperty]
    internal string Description { get; set; }

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
            AdapterType = value;
        else if (propertyName == "module")
            Module = value;
        else if (propertyName == "description")
            Description = value;
    }

}
