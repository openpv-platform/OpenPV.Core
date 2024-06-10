using Ahsoka.Services.System;

namespace Ahsoka.Application.Storyboard;
public partial class BEETouchFirmwareVersion
{
    public override void OnUpdateStoryboard()
    {
        TouchFirmwareVersion = SystemServiceDataKeys.TouchFirmwareVersion;
    }
}
