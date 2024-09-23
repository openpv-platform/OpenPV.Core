using Ahsoka.Spark;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using EnovationCloud.Core.Client;
using EnovationCloud.Core.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ahsoka.Test;

[TestClass]
public class RemoteLinuxTests : LinearTestBase
{

    readonly string containerName = "Ahsoka.Test";
    readonly string imageName = "openpv/openview_test";

    public TestContext TestContext { get; set; }


    [TestMethod]
    public void TestRemoteLinuxToolsInContainer()
    {
        if (Environment.GetEnvironmentVariable("TF_BUILD") != null)
            return;

        ProcessUtility.RunProcess("docker", $"pull {imageName}", null, out string result, out string error);
        Console.WriteLine(result);

        // docker kill Ahsoka.Test - In case left running.
        ProcessUtility.RunProcess("docker", $"kill {containerName}", null, out result, out error);
        Console.WriteLine(result);
        ProcessUtility.RunProcess("docker", $"remove {containerName}", null, out result, out error);
        Console.WriteLine(result);

        // Start Container and Wait to Cancel
        var dockerProcess = Task.Run(() =>
        {
            // docker run -p 22:22 -it --name Ahsoka.Test openpv/openview_test
            ProcessUtility.RunProcess("docker", $"run -p 127.0.0.1:2022:22 --name {containerName} {imageName}", null, out string result, out string error);
            Console.WriteLine(result);
            Console.WriteLine(error);
        });

        // Wait for Container to Start.
        int tries = 10;
        while ((tries--) > 0)
        {
            ProcessUtility.RunProcess("docker", $"ps --filter \"name={containerName}\" --filter \"status=running\" --quiet", null, out string runningResult, out string runningError, 10000);
            if (!string.IsNullOrEmpty(runningResult))
                break;
        }

        try
        {
            // Login to Spark to get Token
            SparkTestsHelpers.GetSparkLogin(out string identity, out string password, out SparkTests.DefaultTestSparkServer);

            var loginDto = SparkAccountClient.SharedSparkClient.LoginToSpark(SparkTests.DefaultTestSparkServer,
                                     SparkEndPointType.Teams,
                                     SecurityGroopType.Account,
                                     identity, SecureLocalStrings.ToSecureString(password),
                                     null);

            var targetInfo = new TargetConnectionInfo() { Port = 2022, PlatformFamily = PlatformFamily.Ubuntu64, PlatformQualifier = "Desktop", HostName = "127.0.0.1", UserName = "root" };
            var sparkLogin = new SparkConnectionInfo() { Login = identity, Password = password, Url = SparkTests.DefaultTestSparkServer };
            var progress = new Progress<string>(Console.WriteLine);

            var mfgInfo = new ManufacturingInfo() { PartNumber = "12345678", SerialNumber = "87654321" };

            var tools = RemoteTargetToolFactory.GetToolsForPlatform(PlatformFamily.Ubuntu64);

            var hardwareInfo = HardwareInfo.GetHardwareInfo(PlatformFamily.Ubuntu64, "Desktop");
            tools.EnableRDP(hardwareInfo, targetInfo, 5999, progress);
            tools.EnableRDP(hardwareInfo, targetInfo, 5999, progress);
            tools.DisableRDP(hardwareInfo, targetInfo, progress);


            // Clear Journals
            result = tools.ClearJournal(targetInfo);
            Console.WriteLine(result);

            // Fetch Journals
            result = tools.GetInstallerJournal(targetInfo);
            Console.WriteLine(result);

            result = tools.GetApplicationJournal(targetInfo);
            Console.WriteLine(result);

            result = tools.StopApplication(targetInfo, false);
            Console.WriteLine(result);

            // Test Standard Prep
            tools.Prep(targetInfo, sparkLogin, progress);

            // Test Factory Prep
            tools.Prep(targetInfo, sparkLogin, progress, mfgInfo);

            // Load from Machine or Create and Add Configuration
            string keyName = "OpenPVContainerTestKeyFile";

            var keyFileInfo = SshUtility.CreateKeyFile(keyName);
            targetInfo.PrivateKeyFile = keyFileInfo.PrivateKeyPath;

            var targetSigningKey = hardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.ApplicationSigningKeyPath);

            SshUtility.AddApplicationKey(targetInfo, keyFileInfo.PublicKeyPath, targetSigningKey);
            SshUtility.AddSshPublicKey(targetInfo, keyFileInfo);
            SshUtility.AddToSshConfiguration(keyFileInfo, keyName, targetInfo.HostName, targetInfo.UserName);
            SshUtility.UnlockDevice(targetInfo, string.Empty);

            result = tools.Reboot(targetInfo);
            Console.WriteLine(result);

            File.Delete(keyFileInfo.PublicKeyPath);
            File.Delete(keyFileInfo.PrivateKeyPath);

            // Stop Container and Wait for process to end.
            ProcessUtility.RunProcess("docker", $"stop {containerName}", null, out result, out error);
            dockerProcess.Wait(5000);

            ProcessUtility.RunProcess("docker", $"kill {containerName}", null, out result, out error);

        }
        catch { }  // We run on the server but we don't fail a build 
        finally
        {
            // Stop Containers and Remove.
            ProcessUtility.RunProcess("docker", $"stop {containerName}", null, out result, out error);
            ProcessUtility.RunProcess("docker", $"kill {containerName}", null, out result, out error);
            ProcessUtility.RunProcess("docker", $"remove {containerName}", null, out result, out error);
        }
    }
}
