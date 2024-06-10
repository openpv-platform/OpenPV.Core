using Ahsoka.Services.System;

namespace Ahsoka.Demo.Storyboard;
public partial class BrightnessUpdate
{
    public void Init()
    {
        DemoViewModelRoot.Default.SystemClient.GetBrightness();
    }

    internal void HandleMessage(BrightnessInfo response)
    {
        Percentage = (ushort)response.Percentage;
        UpdateStoryboard();
    }
}
