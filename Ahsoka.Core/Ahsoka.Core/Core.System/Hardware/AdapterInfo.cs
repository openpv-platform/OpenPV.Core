using System.Text.Json.Serialization;

namespace Ahsoka.Services.Network;

/// <summary>
/// Adapter Information 
/// </summary>
public partial class AdapterInfo
{
    // These are left for Back Compatiblity
    [JsonInclude]
    internal string AdapterId { get; set; }

    [JsonInclude]
    internal string AdapterType { get; set; }

    [JsonInclude]
    internal string Module { get; set; }

    [JsonInclude]
    internal string Description { get; set; }

}
