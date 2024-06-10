using Ahsoka.ServiceFramework;
using Ahsoka.Services.System;
using Ahsoka.Utility;

using System;

namespace Ahsoka.Application.Storyboard;

[DataServiceInfo("DisplayBrightness")]
public partial class FEEBrightnessRequest
{
    private static readonly byte BrightnessMin = 5;
    private static readonly byte BrightnessMax = 100;

    public override void OnUpdateModel(string target)
    {
        //Bound the value
        byte request = Percent;
        request = Math.Min(request, BrightnessMax);
        request = Math.Max(request, BrightnessMin);

        //Make request to the model
        Services.System.BrightnessInfo brightnessRequest = new()
        {
            Percentage = request,
        };
        BrightnessInfo brightnessStatus = ApplicationViewModelRoot.Default.SystemClient.SetBrightness(brightnessRequest);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"C# Brightness Request: {Percent}");


        //Update the UI
        ApplicationViewModelRoot.Default.BrightnessModel.Percent = (byte)brightnessStatus.Percentage;
        ApplicationViewModelRoot.Default.BrightnessModel.UpdateStoryboard();

    }
}
