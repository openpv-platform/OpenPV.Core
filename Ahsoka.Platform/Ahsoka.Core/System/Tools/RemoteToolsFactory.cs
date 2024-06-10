namespace Ahsoka.System;

/// <summary>
/// Gets the Tools Object for a given Platform.
/// </summary>
internal class RemoteTargetToolFactory
{
    public const string HardwareModelPrefix = "HardwareModel:";
    public const string HardwareIdPrefix = "HardwareSerialNumber:";

    /// <summary>
    /// Factory Command to Get a TargetTools Object
    /// </summary>
    /// <param name="platformFamily"></param>
    /// <returns></returns>
    public static IRemoteTargetTool GetToolsForPlatform(PlatformFamily platformFamily)
    {
        if (platformFamily == PlatformFamily.Windows64)
            return new WindowsTargetTools();

        return new RemoteTargetTool();
    }

}
