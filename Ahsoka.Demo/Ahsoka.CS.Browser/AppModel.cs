using Ahsoka.Services.System;
using Ahsoka.System;

namespace Ahsoka.CS.Browser;

internal class AppModel
{
    static SystemServiceClient systemClient = null;
    static int lastBrightness = 50;

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
        lastBrightness = systemClient.GetBrightness().Percentage;
        return lastBrightness;
    }

    internal static void BrightnessUp()
    {
        lastBrightness = systemClient.GetBrightness().Percentage;
        if (lastBrightness <= 90)
            lastBrightness += 10;

        systemClient.SetBrightness(new BrightnessInfo() { Percentage = lastBrightness });
    }

    internal static void BrightnessDown()
    {
        lastBrightness = systemClient.GetBrightness().Percentage;
        if (lastBrightness >= 10)
            lastBrightness -= 10;

        systemClient.SetBrightness(new BrightnessInfo() { Percentage = lastBrightness });
    }

}
