using Ahsoka.Installer.InstallEngine;
using System;

namespace Ahsoka.Core.Hardware;

/// <summary>
/// Screen Info
/// </summary>
public class ScreenInfo
{
    /// <summary>
    /// Adapter Index
    /// </summary>
    public int ScreenWidth { get; set; } = 800;

    /// <summary>
    /// Screen Height for this Display
    /// </summary>
    public int ScreenHeight { get; set; } = 480;

    /// <summary>
    /// Screen Dpi for this Display
    /// </summary>
    public int ScreenDpi { get; set; } = 96;

    /// <summary>
    /// Returns the current Application Orientation (also available in environment APP_ENV_ORIENTATION)
    /// </summary>
    public Orientation CurrentOrientation
    {
        get
        {
            string value = Environment.GetEnvironmentVariable(StartupInstallerPlugin.OrientationKeyAttribute.ToUpperInvariant());
            if (value == "180")
                return Orientation.Rotate180;
            else if (value == "90")
                return Orientation.Rotate90;
            else if (value == "270")
                return Orientation.Rotate270;

            return Orientation.DefaultOrientation;
        }
    }
}

/// <summary>
/// Screen Orientation
/// </summary>
public enum Orientation
{
    /// <summary>
    /// Default Orientation
    /// </summary>
    DefaultOrientation = 0,

    /// <summary>
    /// Rotate 90 Degrees
    /// </summary>
    Rotate90 = 90,

    /// <summary>
    /// Rotate 180 Degrees
    /// </summary>
    Rotate180 = 180,

    /// <summary>
    /// Rotate 270 Degress
    /// </summary>
    Rotate270 = 270,
}
