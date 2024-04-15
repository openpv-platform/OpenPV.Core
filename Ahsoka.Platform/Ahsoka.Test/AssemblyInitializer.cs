
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Ahsoka.Test;

[TestClass]
public class TestInitializer
{
   [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        Extensions.LoadExtensions();

        /// Prep Windows for Tests
        var progress = new Progress<string>(info =>
        {
            Console.WriteLine(info);
        });

        var prep = RemoteTargetToolFactory.GetToolsForPlatform(PlatformFamily.Windows64);

        // Create Hardware Info
        HardwareInfo hardware = HardwareInfo.GetHardwareInfo(PlatformFamily.Windows64, "Desktop");

        var info = new TargetConnectionInfo() { PlatformFamily = hardware.PlatformFamily, PlatformQualifier = hardware.PlatformQualifier, HostName = "localhost", UserName = "" };

        bool returnValue = prep.Prep(info, null, progress);
    }

}
