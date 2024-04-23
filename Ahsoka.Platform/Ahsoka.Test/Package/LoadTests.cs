using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.System;
using Ahsoka.System;
using Ahsoka.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;

namespace Ahsoka.Test;

[TestClass]
public class LoadTests : LinearTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void TestLoad()
    {
        // Start Client for Install System
        var client = new SystemServiceClient();
        var clientRuntime = AhsokaRuntime.CreateBuilder()
                     .AddClients(client)
                     .StartWithInternalServices();

        var harwareInfo = client.GetHardwareInformation();

        // Create a Test Package
        PackageInformation package = PackagingTests.CreatePackage(out string tempFilePath);
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Load Package
        using (FileStream stream = new(package.GetOutputFullPath(), FileMode.Open, FileAccess.Read))
        {
            var progressString = new Progress<FileTransmitProgressInfo>(info =>
            {
                Console.WriteLine($"{info.PercentComplete}% - {info.Message}");
            });

            var result = client.LoadApplicationAsync(stream, true, progressString);

            // Wait and Check Result
            Assert.IsTrue(result.GetAwaiter().GetResult() == MessageReplyStatus.Success);
        }

        stopwatch.Stop();
        Debug.WriteLine($"File Upload Completed in {stopwatch.ElapsedMilliseconds}ms");

        // Delete Work Area - Now deleted at end of assembly shutdown
        //Directory.Delete(new DirectoryInfo(package.PackageOutputDirectory).Parent.FullName, true);

        // Shutdown and Wait for Exit
        AhsokaRuntime.ShutdownAll();
    }
}