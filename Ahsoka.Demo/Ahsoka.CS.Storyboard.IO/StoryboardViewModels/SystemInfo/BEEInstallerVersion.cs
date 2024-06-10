using Ahsoka.Services.System;

namespace Ahsoka.Application.Storyboard;
public partial class BEEInstallerVersion
{
    public override void OnUpdateStoryboard()
    {
        InstallerVersion = SystemServiceDataKeys.InstallerVersion;
    }
}
