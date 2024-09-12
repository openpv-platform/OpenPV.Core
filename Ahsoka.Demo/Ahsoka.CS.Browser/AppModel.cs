using Ahsoka.ServiceFramework;
using Ahsoka.Services.System;
using Ahsoka.System;

namespace Ahsoka.CS.Browser;

internal class AppModel
{
    static SystemServiceClient systemClient = null;
    static UsbInfo lastUsbInfo = null;
    static string lastResult = "No Result";

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

    internal static UsbInfo GetUsbInfo()
    {
        if (lastUsbInfo == null)
            lastUsbInfo = systemClient.GetUsbInfo();

        return lastUsbInfo;
    }

    internal static void ToggleUsbInfo()
    {
        if (lastUsbInfo.Mode == UsbMode.Device || lastUsbInfo.Mode == UsbMode.Unknown)
            lastUsbInfo.Mode = UsbMode.Host;
        else
            lastUsbInfo.Mode = UsbMode.Device;

        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Swapping USB Role to {lastUsbInfo.Mode}");
        systemClient.SetUsbInfo(lastUsbInfo);

        // Wait for New Value
        int max = 0;
        var usbtemp = systemClient.GetUsbInfo();
        while (usbtemp.Mode != lastUsbInfo.Mode && max < 10)
        {
            Thread.Sleep(100);
            max++;
            usbtemp = systemClient.GetUsbInfo();
        }

        lastResult = $"USB Role Swap Result = {lastUsbInfo.Mode == usbtemp.Mode}";
        lastUsbInfo = usbtemp;
    }

    internal static string GetLastResult()
    {
        return lastResult;
    }
}
