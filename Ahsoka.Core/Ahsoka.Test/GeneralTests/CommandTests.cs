using Ahsoka.Core;
using Ahsoka.Installer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace Ahsoka.Test;

[TestClass]
public class CommandTests : LinearTestBase
{

    public TestContext TestContext { get; set; }

    [TestMethod]
    public void TestClientCodeGenerator()
    {

        PackageInformation package = PackagingTests.CreatePackage(out string fileName);
        package.ApplicationType = ApplicationType.Cpp;

        string path = Path.GetTempPath() + Guid.NewGuid().ToString();
        Directory.CreateDirectory(path);

        ClientCodeGenerator.GenerateClients(package, path);

        Directory.Delete(path, true);
    }
}
