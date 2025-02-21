using Ahsoka.Core;
using Ahsoka.Services.System;

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
        if (lastBrightness <= 95)
            lastBrightness += 5;

        lastBrightness = systemClient.SetBrightness(new BrightnessInfo() { Percentage = lastBrightness }).Percentage;
    }

    internal static void BrightnessDown()
    {
        lastBrightness = systemClient.GetBrightness().Percentage;
        if (lastBrightness >= 5)
            lastBrightness -= 5;

        lastBrightness =  systemClient.SetBrightness(new BrightnessInfo() { Percentage = lastBrightness }).Percentage;
    }

}
