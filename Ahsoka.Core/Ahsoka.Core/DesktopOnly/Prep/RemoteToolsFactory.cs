namespace Ahsoka.Core;

/// <summary>
/// Gets the Tools Object for a given Platform.
/// </summary>
public class RemoteTargetToolFactory
{
/// <summary>
    /// Factory Command to Get a TargetTools Object
    /// </summary>
    /// <param name="platformFamily"></param>
    /// <returns></returns>
    public static IRemoteTargetTool GetToolsForPlatform(PlatformFamily platformFamily)
    {
        if (platformFamily == PlatformFamily.Windows64)
            return new WindowsTargetTools();
        else if (platformFamily == PlatformFamily.OpenViewLinuxPro)
            return new OpenViewProRemoteTargetTool();

        return new OpenViewRemoteTargetTool();
    }

}
