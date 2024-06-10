using Ahsoka.System;
using Ahsoka.Utility;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ahsoka.CustomerTools.OpenPVTools;

internal class PlatformSupportPackage : ViewModelBase
{
    #region Fields
    #endregion

    #region Properties
    public PlatformFamily TargetPlatform { get; set; }

    public string Platform { get; set; }

    public string PlatformDescription { get; set; }

    public string FirmwareSparkChannelName { get; set; }

    public string FirmwareSparkVersionFilter { get { return FirmwarePreRequisites.GetBSPInfo(TargetPlatform).FirmwareFilter; } }

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
    public override string ToString() { return Platform; }

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
