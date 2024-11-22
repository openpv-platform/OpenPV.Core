using Ahsoka.Installer.InstallEngine;
using Ahsoka.Utility;
using System;
using Ahsoka.Services.LayerManager;

namespace Ahsoka.System.Hardware;

/// <summary>
/// Screen Info
/// </summary>
public class ScreenInfo : ICanDeserializeBase
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

    internal override void SetProperty(string propertyName, string value)
    {
        if (propertyName == "screenWidth")
            ScreenWidth = int.Parse(value);
        else if (propertyName == "screenHeight")
            ScreenHeight = int.Parse(value);
        else if (propertyName == "screenDpi")
            ScreenDpi = int.Parse(value);
    }

}