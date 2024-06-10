using Ahsoka.Services.System;

namespace Ahsoka.Application.Storyboard;
public partial class BEETouchHardwareVersion
{
    public override void OnUpdateStoryboard()
    {
        TouchHardwareVersion = SystemServiceDataKeys.TouchHardwareVersion;
    }
}
