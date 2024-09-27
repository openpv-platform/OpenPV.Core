using Ahsoka.Extension.Test;
using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.System;
using Ahsoka.System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Ahsoka.Test;

[TestClass]
public class ExtensionTests : LinearTestBase
{
   
    public TestContext TestContext { get; set; }


    [TestMethod]
    public void TestExtensions()
    {
        LoadExtensions();

        Extensions.AddPrivateExtension(typeof(VisibleTestInternals).Assembly);

        string folder = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
        string file = Path.Combine(folder, "BuildFolder", "OpenLinuxST.PackageInfo.json");
        var packageInfo = PackageInformation.LoadPackageInformation(file);

        packageInfo.ServiceInfo.RuntimeConfiguration.ExtensionInfo = [new ExtensionInfo() { ExtensionName = "Test Extension" }];
        var commands = Extensions.GetCommands(packageInfo, "Test Extension", CommandTypes.BuildCommands);

        var extensions = Extensions.GetExtensionLibraries(false);

        Dictionary<string, byte[]> additionalFiles = new Dictionary<string, byte[]>();
        Extensions.InstallExtension(additionalFiles, "Test Extension");
    }


    [TestMethod]
    public void TestSystemEventData()
    {
       var events = AhsokaMessagesBase.GetAllServiceEvents();
    }

    internal static string LoadExtensions()
    {
        string folder = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
        folder = Path.Combine(folder, "ExtensionTests");

        Extensions.LoadExtensions(folder);
        return folder;
    }
}
