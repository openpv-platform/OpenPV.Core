using System;

namespace Ahsoka.Demo.Storyboard;
public partial class ChangeBrightness
{
    public override void OnUpdateModel(string target)
    {
        var newBrightness = Increase == 1 ?
                Math.Min(DemoViewModelRoot.Default.BrightnessModel.Percentage + (short)Value, 100) :
                Math.Max(DemoViewModelRoot.Default.BrightnessModel.Percentage - (short)Value, 5);

        var value = DemoViewModelRoot.Default.SystemClient.SetBrightness(new Services.System.BrightnessInfo() { Percentage = newBrightness });
        DemoViewModelRoot.Default.BrightnessModel.HandleMessage(value);

    }
}
