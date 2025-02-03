using Ahsoka.Core.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Ahsoka.DeveloperTools.OpenPVTools;

internal class PlatformSupportPackage : ViewModelBase
{
    #region Fields
    #endregion

    #region Properties
    public string HelpSparkChannelName { get; set; }

    public string ExtensionSparkChannelName { get; set; }

    public string ExtensionSparkVersionFilter { get { return GetCurrentMajorMinor(); } }

    public string DemoSparkChannelName { get; set; }

    public string DemoSparkVersionFilter
    {
        get { return GetCurrentMajorMinor(); } // Just Show Latest
    }

    public string PlatformSupportSparkChannelName { get; set; }

    public string PlatformSupportSparkVersionFilter
    {
        get { return GetCurrentMajorMinor(); }
    }

    public OpenPVToolBase Resources { get; set; }

    public List<OpenPVToolBase> Tools { get; set; }

    [JsonIgnore]
    public ObservableCollection<OpenPVToolBase> Downloads { get; init; } = new();

    [JsonIgnore]
    public ObservableCollection<OpenPVToolBase> DeveloperTools { get; init; } = new();
    #endregion

    #region Methods
    internal void CheckDownload()
    {
        foreach (var item in Downloads)
            item.CheckDownload();
    }

    private string GetCurrentMajor()
    {
        var version = VersionUtility.GetAppVersion();
        return $"{version.Major}.";

    }

    private string GetCurrentMajorMinor()
    {
        var version = VersionUtility.GetAppVersion();
        return $"{version.Major}.{version.Minor}";

    }
    #endregion
}
