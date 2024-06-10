using Ahsoka.Services.System;

namespace Ahsoka.Application.Storyboard;
public partial class BEESDKVersion
{
    public override void OnUpdateStoryboard()
    {
        SdkVersion = SystemServiceDataKeys.SDKVersion;
    }
}
