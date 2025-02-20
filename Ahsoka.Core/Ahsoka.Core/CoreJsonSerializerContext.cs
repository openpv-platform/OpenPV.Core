using Ahsoka.Core.Hardware;
using Ahsoka.Core.Spark;
using Ahsoka.Installer;
using Ahsoka.Services.Network;
using Ahsoka.Services.Spark;
using Ahsoka.Services.System;
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
[JsonSerializable(typeof(RuntimeConfiguration))]
[JsonSerializable(typeof(HardwareInfo))]
[JsonSerializable(typeof(TargetConnectionInfo))]
[JsonSerializable(typeof(ProvisionDisplayRequest))]
[JsonSerializable(typeof(DeviceKeyInfo))]
[JsonSerializable(typeof(SparkConnectionInfo))]
[JsonSerializable(typeof(CustomPackageInfo))]
[JsonSerializable(typeof(DeviceKeyInfo))]
[JsonSerializable(typeof(ManufacturingInfo))]
[JsonSerializable(typeof(ExtensionConfiguration))]
[JsonSerializable(typeof(SystemSettings))]
[JsonSerializable(typeof(FactoryManifest))]
[JsonSerializable(typeof(SecurityConfiguration))]
[JsonSerializable(typeof(PackageContents))]
[JsonSerializable(typeof(PackageHeader))]
[JsonSerializable(typeof(LicenseList))]
[JsonSerializable(typeof(NetworkProfilesList))]
[JsonSerializable(typeof(SparkSubscription))]
[JsonSerializable(typeof(PackageInformation))]
internal partial class CoreJsonSerializerContext : JsonSerializerContext
{

}
