using Ahsoka.Services.System;

namespace Ahsoka.Application.Storyboard;
public partial class BEEApplicationVersion
{
    public override void OnUpdateStoryboard()
    {
        ApplicationVersion = SystemServiceDataKeys.ApplicationVersion;
    }
}
