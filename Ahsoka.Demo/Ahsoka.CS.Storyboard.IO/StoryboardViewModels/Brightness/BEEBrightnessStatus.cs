using Ahsoka.ServiceFramework;
using Ahsoka.Utility;

namespace Ahsoka.Application.Storyboard;

[DataServiceInfo("DisplayBrightness", true)]
public partial class BEEBrightnessStatus
{
    public override void OnUpdateStoryboard()
    {
        Percent = (byte)ApplicationViewModelRoot.Default.SystemClient.GetBrightness().Percentage;
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"C# Brightness Status: {Percent}");
    }
}
