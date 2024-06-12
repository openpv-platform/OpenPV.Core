using System;
using System.Reflection;

namespace Ahsoka.Utility;

/// <summary>
/// Gets version for the Running Application
/// </summary>
public class VersionUtility
{
    /// <summary>
    /// Get Version as a String
    /// </summary>
    /// <returns></returns>
    public static string GetAppVersionString()
    {
        return GetAppVersion().ToString();
    }

    /// <summary>
    /// Get Version as an Object
    /// </summary>
    /// <returns></returns>
    public static Version GetAppVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version;
    }
}
