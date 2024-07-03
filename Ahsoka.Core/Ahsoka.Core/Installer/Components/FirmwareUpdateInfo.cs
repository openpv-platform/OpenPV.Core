using Ahsoka.System;
using System.Collections.Generic;

namespace Ahsoka.DeveloperTools.OpenPVTools;

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
