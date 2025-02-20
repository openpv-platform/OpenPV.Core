using Ahsoka.Installer.UI.InstallSystem;
using System.Text.Json.Serialization;

namespace Ahsoka.Core;

/// <summary>
/// System Class for Handling Source Generated Json Files
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
    IncludeFields = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(KeyBoard))]
internal partial class InstallerJsonSerializerContext : JsonSerializerContext
{

}



