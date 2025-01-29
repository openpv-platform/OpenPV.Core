#pragma warning disable CS1591
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ahsoka.System;

internal class ExtensionConfiguration
{
    internal string FilePath { get; set; }

    /// <summary>
    /// Name of the Extension
    /// </summary>
    public string ExtensionName { get; set; }

    /// <summary>
    /// Flag indicating if the Extension container CLI Commands
    /// </summary>
    public bool HasCommands { get; set; }

    /// <summary>
    /// Flag indicating if the Extension has a User Interface
    /// </summary>
    public bool HasUx { get; set; }

    /// <summary>
    /// Flag indicating if the Extension has a SDK Extension
    /// </summary>
    public bool HasSdkGenerator { get; set; }

    /// <summary>
    /// Flag indicating if the Extension has an Installer Slot
    /// </summary>
    public bool HasInstallerSlot { get; set; }

    /// <summary>
    /// Nuget package containing the Primary Package Library
    /// </summary>
    public string PackageName { get; set; }

    /// <summary>
    /// Set of Services that are hosted by this Extension
    /// </summary>
    public List<ServiceConfiguration> ServiceConfigurations { get; set; }

    /// <summary>
    /// Name of the Nuget Package which contains the User Interface Extension Library
    /// </summary>
    public string UxPackageName { get; set; }

    /// <summary>
    /// Name of the Model Class used for Avalonia View Model Extensions
    /// </summary>
    public string UxViewModelName { get; set; }

    /// <summary>
    /// Name of the Class Providing the SDK Generator
    /// </summary>
    public string SdkGeneratorName { get; set; }

    /// <summary>
    /// FileName for the Primary Extension Library
    /// </summary>
    public string LibraryFilename { get { return PackageName + ".dll"; } }

    /// <summary>
    /// FileName for User Interface Extension Library
    /// </summary>
    public string UXLibraryFilename { get { return UxPackageName + ".dll"; } }

    /// <summary>
    /// Minimum Version that will be accepted 
    /// </summary>
    public string MinVersion { get; set; }

    /// <summary>
    /// Maximum Version that will be accepted 
    /// </summary>
    public string MaxVersion { get; set; }

    /// <summary>
    /// Require Exact Match (for Core Modules) 
    /// </summary>
    public bool RequireExactVersion { get; set; }

    /// <summary>
    /// Maximum Version that will be accepted 
    /// </summary>
    public string InstalledVersion { get; set; }

    /// <summary>
    /// Maximum Version that will be accepted 
    /// </summary>
    [JsonIgnore]
    public string AvailableVersion { get; set; }

}