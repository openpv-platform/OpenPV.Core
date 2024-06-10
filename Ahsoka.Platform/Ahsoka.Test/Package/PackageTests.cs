using Ahsoka.Installer;
using Ahsoka.Installer.InstallEngine;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Install;
using Ahsoka.System;
using Ahsoka.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Ahsoka.Test;

[TestClass]
public class PackagingTests
{
    public TestContext TestContext { get; set; }
    static readonly Dictionary<string, PackageInformation> packageInfo = new();

    [AssemblyCleanup]
    public static void Cleanup()
    {
        string appOutputs = Path.Combine(Path.GetTempPath(), "AhsokaTestFolder");

        int tries = 0;
        while (tries < 4 && Directory.Exists(appOutputs))
        {
            try
            {
                Directory.Delete(appOutputs, true);
            }
            catch { Thread.Sleep(1000); } // Wait files to be released.

            tries++;
        }
    }

    [TestMethod]
    public void TestPackagesStoryboard()
    {
        RunPackageTest(true, "AhsokaDemo");
    }

    [TestMethod]
    public void TestPackagesInProc()
    {
        RunPackageTest(true, "AhsokaCAN", "S70", PlatformFamily.OpenViewLinux);
    }

    [TestMethod]
    public void TestPackagesOutProc()
    {
        RunPackageTest(false);
    }

    private static void RunPackageTest(bool runInProc, string packageToTest = "", string qualifier = "Desktop", PlatformFamily family = PlatformFamily.Windows64)
    {
        PackageInformation package = packageToTest == "" ?
                    CreatePackage(out string tempFilePath) :
                    CreatePackage(out tempFilePath, family, qualifier, packageToTest);

        // Open the File to Read Data
        using (FileStream stream = new(tempFilePath, FileMode.Open))
        {
            InstallEngineBase installEngine = InstallEngineBase.GetInstallEngine(stream);

            PackageContents contents = installEngine.PackageContents;

            Assert.AreEqual(package.PackageType, contents.PackageType);
            Assert.AreEqual(package.PlatformFamily, contents.PlatformFamily);
            Assert.AreEqual(package.SecurityMode, contents.SecurityMode);
            Assert.AreEqual(package.PlatformQualifier, contents.PlatformQualifier);
            if (!String.IsNullOrEmpty(package.SecurityModelNumber))
                Assert.AreEqual(package.SecurityModelNumber, contents.SecurityModelNumber);
            if (!String.IsNullOrEmpty(package.SecurityPassKey))
                Assert.AreEqual(package.SecurityPassKey, contents.SecurityPassKey);
            if (!String.IsNullOrEmpty(package.SecuritySerialNumber))
                Assert.AreEqual(package.SecuritySerialNumber, contents.SecuritySerialNumber);

            Assert.IsTrue(contents.IsSignatureValid);

            // Validate All Packages
            var isValid = installEngine.ValidatePackages();
            Assert.IsTrue(isValid == InstallResult.Success); // GUID is in the Stream

            // Extract a Single Package
            var packageId = installEngine.Components.First().ComponentType; // Fetch First Item

            using MemoryStream outputStream = new();

            installEngine.ExtractPackage(packageId, outputStream);

            string output = UTF8Encoding.UTF8.GetString(outputStream.ToArray());

            Assert.AreEqual(output, packageId.ToString());
        }

        // Start Client for Install System
        InstallServiceClient client = new();
        client.SetInstallerUI(new InstallerUIMock());

        // Create a Client Only Runtime
        AhsokaRuntime.CreateBuilder()
                     .AddClients(client)
                     .StartLocalRuntime();

        // Launch Install Service and Execute Install
        // This will block until its done.
        // Create a copy of the file since we use the same file multiple times
        string newTempFile = Path.GetTempFileName();
        File.Copy(tempFilePath, newTempFile, true);

        client.StartServiceAndInstall(newTempFile, "PassCode", runInProc);

        File.Delete(newTempFile);
    }


    public static PackageInformation CreatePackage(out string path, PlatformFamily family = PlatformFamily.Windows64, string qualifier = "Desktop", string packageToTest = "AhsokaCAN")
    {
        string key = family.ToString() + qualifier + packageToTest;

        if (packageInfo.TryGetValue(key, out PackageInformation info))
        {
            path = info.GetOutputFullPath();
            return info;
        }

        Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);

        string tempPath = Path.Combine(Path.GetTempPath(), "AhsokaTestFolder");
        string appOutputs = Path.Combine(tempPath, "DemoRoot", Guid.NewGuid().ToString());
        string originPath = Path.Combine(packageToTest);

        var progressString = new Progress<string>(Console.WriteLine);

        Directory.CreateDirectory(appOutputs);
        FileUtilities.CopyDirectory(originPath, appOutputs, true, progressString);

        Environment.CurrentDirectory = appOutputs;

        string packagefile = Path.Combine(appOutputs, "OpenLinuxST.PackageInfo.json");
        PackageInformation package = JsonUtility.Deserialize<PackageInformation>(File.ReadAllText(packagefile));

        package.SecurityMode = SecurityModes.None;
        package.SecurityModelNumber = "MyModelNumber";
        package.SecurityPassKey = "MyPasskey";
        package.SecuritySerialNumber = "1234567";
        package.PlatformFamily = family;
        package.PlatformQualifier = qualifier;
        package.PackageOutputDirectory = "OutputFolder";

        // Write Fake Exe so we don't have to build
        string appFolder = Path.Combine(appOutputs, package.PackageApplicationFolder);
        if (!Directory.Exists(appFolder))
            Directory.CreateDirectory(appFolder);

        File.WriteAllText(Path.Combine(appFolder, package.ExecCommand), "TestExe");
        package.DotNetInfo.PublishProfile = "";

        // Write our Package File
        File.WriteAllText(packagefile, JsonUtility.Serialize(package));

        // Load All Info...setup paths etc.
        package = PackageInformation.LoadPackageInformation(packagefile);

        // Capture the values to our cache.
        packageInfo.Add(key, package);

        // Get an Engine and Create the Package File
        InstallEngineBase createPackageEngine = InstallEngineBase.GetPackagingEngine(package);
        path = createPackageEngine.CreateInstallPackage(null, new Progress<PackageProgressInfo>(info =>
        {
            Console.WriteLine(info.Message);
        }));

        return package;
    }

    [TestMethod]
    public void TestSigningCertificate()
    {
        var result = RSAEncryption.GenerateNewKeyPair();

        Debug.WriteLine(result.Key); // Private Key
        Debug.WriteLine(result.Value); // Public Key

        string originalValue = "12345678";
        byte[] bytes = UTF8Encoding.UTF8.GetBytes(originalValue);

        var encryptedBytes = RSAEncryption.EncryptWithPEMKey(bytes, result.Value);
        byte[] decryptedBytes = RSAEncryption.DecryptWithPEMKey(encryptedBytes, result.Key);

        string resultString = Encoding.UTF8.GetString(decryptedBytes);

        Assert.AreEqual(originalValue, resultString);


        var resultXML = RSAEncryption.GenerateNewKeyPair(false);
        string resultPem1 = RSAEncryption.ConvertXMLToPrivatePEM(resultXML.Key);
        Assert.IsTrue(resultPem1.Length > 0);
        string resultPem2 = RSAEncryption.ConvertXMLToPublicPEM(resultXML.Key);
        Assert.IsTrue(resultPem2.Length > 0);
    }

    [TestMethod]
    public void TestInstallerCore()
    {
        PackageInformation package = CreatePackage(out string _);

        string pathToOutput = package.GetPackageInfoPath();
        string solDirectory = Path.GetDirectoryName(pathToOutput);

        string[] arr = new string[3];
        arr[0] = "test";

        CommandLine.Execute(arr);

        arr[0] = "--Package";
        arr[1] = solDirectory;
        arr[2] = pathToOutput;
        CommandLine.Execute(arr);

        string pathToPackage = package.GetOutputFullPath();

        arr = new string[2];
        arr[0] = "--PrintManifest";
        arr[1] = pathToPackage;

        CommandLine.Execute(arr);

        // Delete Work Area
        //Directory.Delete(Path.GetDirectoryName(package.GetPackageInfoPath()), true);
    }
}


public class InstallerUIMock : IInstallerUI
{
    readonly StringBuilder builder = new();

    public void InstallComplete(InstallCompleteInfo info)
    {
        Console.WriteLine($"\r\n{info.Message}: {info.Result}");
        Console.WriteLine();
        Console.WriteLine("Log Outputs....");
        Console.Write(builder.ToString());
    }

    public void Log(InstallLogInfo info)
    {
        builder.AppendLine($"Log [{info.LogMessageType}]: {info.LogMessage}");
    }

    public void InstallProgress(InstallProgressInfo info)
    {
        if (info.UpdatePercentOnly)
            Console.Write($"..{info.PercentComplete}");
        else
            Console.Write($"\r\nProgress Report:{info.Message} {info.PercentComplete}");
    }
}
