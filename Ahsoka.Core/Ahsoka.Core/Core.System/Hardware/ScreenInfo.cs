using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.LayerManager;
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