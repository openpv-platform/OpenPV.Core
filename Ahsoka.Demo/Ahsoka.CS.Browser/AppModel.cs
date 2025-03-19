using Ahsoka.Core;
using Ahsoka.Services.System;

namespace Ahsoka.CS.Browser;

internal class AppModel
{
    static SystemServiceClient systemClient = null;
    static BrightnessInfoRaw lastBrightness = new BrightnessInfoRaw() { Value = 10 };

    internal static void StartModel(WebApplication app)
    {
        // Send Release Notification
        ApplicationContext.ReleaseStartup();
        systemClient = new SystemServiceClient();
        systemClient.ServiceConfig.Behaviors = EndPointBehaviors.AutoStart;
        systemClient.Start();
    }

    internal static GetHardwareInformationReply GetHardwareInformation()
    {
        return systemClient.GetHardwareInformation();
    }

    internal static int GetBrightness()
    {
        lastBrightness = systemClient.GetBrightnessRaw();
        return lastBrightness.Value;
    }

    internal static void BrightnessUp()
    {
        lastBrightness = systemClient.GetBrightnessRaw();
        if (lastBrightness.Value < lastBrightness.MaxValue)
            lastBrightness.Value += 1;

        lastBrightness = systemClient.SetBrightnessRaw(lastBrightness);
    }

    internal static void BrightnessDown()
    {
        lastBrightness = systemClient.GetBrightnessRaw();
        if (lastBrightness.Value > 0)
            lastBrightness.Value -=1;


        lastBrightness = systemClient.SetBrightnessRaw(lastBrightness);
    }

}
