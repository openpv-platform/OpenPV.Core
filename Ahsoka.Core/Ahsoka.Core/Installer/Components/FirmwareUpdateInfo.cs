using Ahsoka.Core;
using System.Collections.Generic;

namespace Ahsoka.Installer;

internal class FirmwareUpdateInfo
{
    #region Properties
    public PlatformFamily PlatformFamily { get; set; }

    public string UpgradesTo { get; set; }

    public List<string> UpgradesFrom
    {
        get; set;
    }
    #endregion

}
