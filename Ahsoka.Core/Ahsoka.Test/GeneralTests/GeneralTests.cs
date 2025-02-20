using Ahsoka.Core;
using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility;
using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.Spark;
using Ahsoka.DeveloperTools.ViewModel;
using Ahsoka.Installer;
using Ahsoka.Installer.Components;
using Ahsoka.Services.Network;
using Ahsoka.Services.System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Ahsoka.Test;

[TestClass]
public class GeneralTests : LinearTestBase
{
    private object myValue = null;
    public object MyValue { get { return this.LazyLoad(ref myValue); } }

    public TestContext TestContext { get; set; }

    [ClassCleanup]
    public static void StopLogging()
    {
        AhsokaLogging.StopMemoryLog();
    }

    [TestMethod]
    public void GetParametersTests()
    {
        var knownValues = AhsokaMessagesBase.GetAllSystemParameters();

        Assert.IsTrue(knownValues.Any(x => x.Key == SystemService.Name));

        var systemKeys = knownValues.FirstOrDefault(x => x.Key == SystemService.Name);
        Assert.IsTrue(systemKeys.Value.Any(x => x.Name == "Available"));

        Assert.IsTrue(knownValues.Any(x => x.Key == WiFiClientService.Name));

        var networkKeys = knownValues.FirstOrDefault(x => x.Key == WiFiClientService.Name);
        Assert.IsTrue(networkKeys.Value.Any(x => x.Name == "ClientNetworkName"));
    }


    [TestMethod]
    public void NugetFetchTest()
    {
        string packageName = "Ahsoka.Core";
        string minVersionString = "3.0.0";
        string maxVersionString = "4.0.0";

        var source = new PackageSource("https://api.nuget.org/v3/index.json");
        var providers = Repository.Provider.GetCoreV3();
        var repository = new SourceRepository(source, providers);

        var search = repository.GetResourceAsync<PackageSearchResource>().GetAwaiter().GetResult();
        var filter = new SearchFilter(false);

        IEnumerable<IPackageSearchMetadata> response = search.SearchAsync($"packageid:{packageName}", filter, skip: 0, take: 20, NullLogger.Instance, CancellationToken.None).GetAwaiter().GetResult();
        var versions = response.First().GetVersionsAsync().GetAwaiter().GetResult();

        var results = versions.Select(x => x.Version);
        var minVersion = new NuGetVersion(minVersionString);
        var maxVersion = new NuGetVersion(maxVersionString);

        var thisVersion = new VersionRange(minVersion, true, maxVersion, false, new FloatRange(NuGetVersionFloatBehavior.Major, minVersion, "")).FindBestMatch(results)?.ToFullString();
        Console.WriteLine($"Minor Finds {thisVersion}");

        thisVersion = new VersionRange(minVersion, true, maxVersion, false, new FloatRange(NuGetVersionFloatBehavior.PrereleaseMinor, minVersion, "")).FindBestMatch(results)?.ToFullString();
        Console.WriteLine($"PrereleaseMinor Finds {thisVersion}");
    }

    [TestMethod]
    public void LoadHardwareInfo()
    {
        AhsokaLogging.StartMemoryLog(100, AhsokaVerbosity.Low);

        //Pull Sample Hardware Def
        string solution_dir = Path.GetDirectoryName(Environment.ProcessPath);
        while (!solution_dir.EndsWith("Ahsoka.Core"))
            solution_dir = Path.GetDirectoryName(solution_dir);

        var fileName = Path.Combine(solution_dir, @"Ahsoka.PublishOpenView\OpenViewS50HardwareConfiguration.json");

        string jsonText = File.ReadAllText(fileName);

        // Serialize a Hardware Info from both Manual and Auto and compare 
        // should find if someone missed a property
        var info1 = JsonSerializer.Deserialize<HardwareInfo>(jsonText, JsonUtility.Options);
        //info1.CANInfo = null; // No longer supported
        //info1.IOInfo = null;// No longer supported
        //info1.BluetoothInfo = null;// No longer supported
        var info2 = JsonUtility.Deserialize<HardwareInfo>(jsonText);

        string newText1 = JsonSerializer.Serialize(info1, JsonUtility.Options);
        string newText2 = JsonUtility.Serialize(info2);

        //File.WriteAllText(@"C:\Users\Jake\Desktop\HardwareConfiguration.test1", newText1);
        //File.WriteAllText(@"C:\Users\Jake\Desktop\HardwareConfiguration.test2", newText2);

        AhsokaLogging.StopMemoryLog();

        Assert.AreEqual(newText1, newText2);
    }

    [TestMethod]
    public void ClassLoaderTest()
    {
        var subClasses = ClassLoader.GetSubclasses(typeof(FileSystemComponentBase));
        Assert.IsTrue(subClasses.Any());

        var interfaces = ClassLoader.GetItemsWithInterface(typeof(ICanCancel));
        Assert.IsTrue(interfaces.Any());
    }

    [TestMethod]
    public void GeneralCoverageTests()
    {
        // Random small accesses to increase code coverage

        var info = FirmwarePreRequisites.GetBSPInfo(PlatformFamily.OpenViewLinux);
        Console.WriteLine($"{info.RequiredCodeName}:{info.MinVersion}:{info.FirmwareFilter}");
        info = FirmwarePreRequisites.GetBSPInfo(PlatformFamily.OpenViewLinuxPro);
        Console.WriteLine($"{info.RequiredCodeName}:{info.MinVersion}:{info.FirmwareFilter}");
        info = FirmwarePreRequisites.GetBSPInfo(PlatformFamily.Windows64);
        Console.WriteLine($"{info.RequiredCodeName}:{info.MinVersion}:{info.FirmwareFilter}");

        PlatformSupportPathInfo.GetPlatformResourcePath(PlatformFamily.Windows64);

        foreach (var item in Enum.GetValues<InstallerPaths>())
            SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(item);

        Assert.IsNull(HardwareInfo.GetHardwareInfoByDescription("BadName"));
        Console.WriteLine(SystemInfo.HardwareInfo.PlatformDescription);

        ClassLoader.AddAssembly(Assembly.GetExecutingAssembly());

        Assert.IsNotNull(this.MyValue); // Test Lazy Loader
    }

    [TestMethod]
    public void ProcessUtilityTests()
    {
        // Random small accesses to increase code coverage
        ProcessUtility.RunProcessScript("cmd /c dir", ".", out string _, out string _);
        ProcessUtility.RunProcessInWindow("cmd", "/c dir", false, ".");
        var _ = ProcessUtility.CreateProcessInWindow("cmd", "/c dir", "."); // Don't start.
    }

    [TestMethod]
    public void FilePathTests()
    {
        string permData = ApplicationDataPaths.GetPermanentDataPath();

        string permAppData = Path.Combine(permData, "MyTestFile");
        File.WriteAllText(permAppData, "MyTestFile");
        Assert.IsTrue(File.Exists(permAppData));

        string localData = ApplicationDataPaths.GetLocalDataPath();

        string localAppData = Path.Combine(localData, "MyTestFile");
        File.WriteAllText(localAppData, "MyTestFile");
        Assert.IsTrue(File.Exists(localAppData));

        string path = ApplicationDataPaths.GetPersistentDataPath();

        string appData = Path.Combine(path, "MyTestFile");
        File.WriteAllText(appData, "MyTestFile");
        Assert.IsTrue(File.Exists(appData));

        string serviceDataPath = SystemInfo.HardwareInfo.TargetPathInfo.GetServiceDataPath("MyService");
        string serviceData = Path.Combine(serviceDataPath, "MyTestFile");
        File.WriteAllText(serviceData, "MyTestServiceFile");
        Assert.IsTrue(File.Exists(serviceData));

        // Clear App Folder...Service Should Still Exist
        ApplicationDataPaths.ClearAppData();
        Assert.IsTrue(!File.Exists(appData));
        Assert.IsTrue(!File.Exists(localAppData));

        Assert.IsTrue(File.Exists(serviceData));

        // Service Data should Go.
        ApplicationDataPaths.ClearAppData(true, true);
        Assert.IsTrue(!File.Exists(serviceData));

        /// Verify Permanent
        Assert.IsTrue(File.Exists(permAppData));
        File.Delete(permAppData);
    }

    [TestMethod]
    public void FileTests()
    {

        string testValue = "TestValue";

        string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string tempFolder2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string tempFile = Path.Combine(tempFolder, Guid.NewGuid().ToString());
        string tempFile2 = Path.Combine(tempFolder2, Path.GetFileName(tempFile));

        Directory.CreateDirectory(tempFolder);
        File.WriteAllText(tempFile, testValue);

        // Copy Folder and Verify the Copied Result
        FileUtilities.CopyDirectory(tempFolder, tempFolder2, true, new Progress<String>());

        string text = File.ReadAllText(tempFile2);

        Assert.AreEqual(testValue, text);

        Directory.Delete(tempFolder, true);
        Directory.Delete(tempFolder2, true);

    }

    [TestMethod]
    public void ProcessUtilityTest()
    {
        string commandLine = Path.Combine(PlatformSupportPathInfo.GetPlatformSupportPath(PlatformFamily.Windows64), "CommandLine", "Ahsoka.CommandLine");

        StringBuilder builder = new();
        CancellationTokenSource source = new();
        var progress = new LocalProgress<CommandLineProgressInfo>(info =>
        {
            builder.AppendLine(info.Message);
        });

        LocalCommandLine.RunWithLocalConsole("InvalidArgs", null, progress);

        string results = builder.ToString();

        // Check that Critical Plugins Loaded.
        Assert.IsTrue(results.Contains("--StartInstallService"), results);
        Assert.IsTrue(results.Contains("--LoadApplication"), results);
        Assert.IsTrue(results.Contains("--Package"), results);
        Assert.IsTrue(results.Contains("--FactoryPrep"), results);
        Assert.IsTrue(results.Contains("--Prep"), results);
        Assert.IsTrue(results.Contains("--GetHardwareModel"), results);
        Assert.IsTrue(results.Contains("--GetHardwareSerialNumber"), results);
        Assert.IsTrue(results.Contains("--StartServices"), results);

    }

    [TestMethod]
    public void TestSecurityFunctions()
    {
        using SshKeyGenerator gen = new(1024);
        string privateKey = gen.ToPrivateKey();
        Assert.IsTrue(privateKey.Contains("-----END RSA PRIVATE KEY-----"));

        string toPublicKey = gen.ToPublicKey();
        Assert.IsTrue(toPublicKey.Contains("-----END PUBLIC KEY-----"));

        string toRfcPublicKey = gen.ToRfcPublicKey("MyExampleKey");
        Assert.IsTrue(toRfcPublicKey.Contains("ssh-rsa"));

        string toXml = gen.ToXml();
        Assert.IsTrue(toXml.Contains("<RSAKeyValue>"));
    }

    [TestMethod]
    public void StringTests()
    {
        var result = "MyValueIsHigh".SplitCamelCase();
        Assert.AreEqual(result, "My Value Is High");

        StringBuilder builder = new();
        builder.Append("Line1\n");
        builder.Append("Line2");
        builder = builder.RemoveToLastLine();
        Assert.AreEqual("Line1", builder.ToString());
    }

    [TestMethod]
    public void EncryptionTest()
    {
        string testData = "TestData";
        string path = Path.GetTempFileName();
        StringEncryption.CreateSecureFile(path, testData);
        var result = StringEncryption.ReadSecureFile(path);
        string readableResult = StringEncryption.ToInsecureString(result);
        Assert.AreEqual(testData, readableResult);
    }

    [TestMethod]
    public void EncryptionAESTest()
    {
        string stringData = "Hello, World!";
        MemoryStream plaintext = new(UTF8Encoding.UTF8.GetBytes(stringData));
        Console.WriteLine(plaintext);
        // Generate a random key and IV

        var data = AesEncryption.KeyFromDeviceId(Guid.NewGuid());

        // Encrypt / Decrypt
        var ciphertext = AesEncryption.Encrypt(plaintext, data.Key, data.IV);
        var decryptedText = AesEncryption.Decrypt(ciphertext, data.Key, data.IV);

        var resultString = Encoding.UTF8.GetString(decryptedText.ToArray());

        Assert.AreEqual(stringData, resultString);

    }


    [TestMethod]
    public void TestUpgradeInfo()
    {
        FirmwareUpdateInfo info = JsonUtility.Deserialize<FirmwareUpdateInfo>(Properties.Resources.OpenView_upgradeinfo);
        Assert.IsNotNull(info);
        Assert.IsTrue(info.UpgradesTo != null);
        Assert.IsTrue(info.PlatformFamily == PlatformFamily.OpenViewLinux);
        Assert.IsTrue(info.UpgradesFrom.Count > 0);
    }

    [TestMethod]
    public void SimpleViewModelTest()
    {
        TestViewModelParent parent = new();

        TestViewModel model = new(parent)
        {
            ValueObject = this,
            Value = 1234
        };

        Assert.AreEqual(model.ParentViewModel, parent);
        Assert.AreEqual(model.ValueObject, this);
        Assert.AreEqual(model.Value, 1234);

        var parms = parent.GetPropertyMap();
        Assert.AreEqual(parms.SystemName, this.GetType().Namespace);

    }

    [TestMethod]
    public void TestProjectUpgrade()
    {
        string folder = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
        string file = Path.Combine(folder, "BuildFolder", "OpenLinuxST.PackageInfo.json");
        if (File.Exists(file))
            file = Path.GetFullPath(file);

        var packageInfo = PackageInformation.LoadPackageInformation(file);

        file = Path.Combine(folder, "BuildFolder", "STDemoPackage.1.cancalibration.json");

        string result = UpgradeEngine.ExecuteUpgrade(file);
    }

    public class TestViewModelParent : ViewModelBase
    {
        public TestViewModelParent()
        { }

        private int myValue;
        private GeneralTests myValueObject;

        public int Value
        {
            get { return myValue; }
            set { OnSetValue(ref myValue, value); }
        }

        public GeneralTests ValueObject
        {
            get { return myValueObject; }
            set { OnSetObject(ref myValueObject, value); }
        }
    }

    public class TestViewModel : ChildViewModelBase<TestViewModelParent>
    {
        public TestViewModel(TestViewModelParent tests) :
            base(tests)
        { }

        private int myValue;
        private GeneralTests myValueObject;

        public int Value
        {
            get { return myValue; }
            set { OnSetValue(ref myValue, value); }
        }

        public GeneralTests ValueObject
        {
            get { return myValueObject; }
            set { OnSetObject(ref myValueObject, value); }
        }
    }



}
