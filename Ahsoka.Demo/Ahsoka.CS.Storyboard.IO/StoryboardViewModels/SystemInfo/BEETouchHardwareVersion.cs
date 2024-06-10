using Ahsoka.Services.System;

namespace Ahsoka.Application.Storyboard;
public partial class BEEBSPVersion

{
    public override void OnUpdateStoryboard()
    {
        BspVersion = SystemServiceDataKeys.BSPVersion;
    }
}
