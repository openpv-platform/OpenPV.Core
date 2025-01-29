using Ahsoka.Core.Drawing.Utility;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;

namespace Ahsoka.Installer.UI;

class Program
{
    static void Main()
    {
        // Show Warnings and Errors as well as Performance Metrics
        AhsokaLogging.LoggingVerbosity = AhsokaVerbosity.Medium | AhsokaVerbosity.Performance;
        using var stopwatch = new AhsokaStopwatch("Ahsoka.Installer.UI.Main()");
        AhsokaStopwatch.MaxDepth = 4;

        if (OperatingSystem.IsWindows())
            ConsoleExtension.Hide();

        ConfigurationLoader.SetRuntimeConfiguration(ConfigurationLoader.CreateDefaultConfiguration());

        // Start the Application passing it our drawing area
        new ApplicationController()
            .RunApplication(InstallerDrawingManager.Default);

        ApplicationContext.Exit();
    }
}
