using Ahsoka.Installer;
using Ahsoka.Installer.InstallEngine;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Install;
using Ahsoka.Services.System;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
#pragma warning disable IDE0051 // Remove unused private members

namespace Ahsoka.Commands;

[ExcludeFromCodeCoverage]
[CommandLinePlugin]
internal static class PackageCommands
{
    [CommandLineMethod(@"     --LoadApplication: Loads and Installs an .opv File
            [TargetConnectionInfoFile] - Connection Info File(JSON)
            [ApplicationFile] Path to an opv file")]

    private static void LoadApplication(string connectionInfoFile, string applicationFile)
    {
        var connectionInfo = JsonUtility.Deserialize<TargetConnectionInfo>(File.ReadAllText(connectionInfoFile));

        var config = new System.ServiceConfiguration()
        {
            ServiceName = SystemService.Name,
            TcpConnectionAddress = $"{connectionInfo.HostName ?? "192.168.7.1"}",
            DataChannel = 5001,
        };

        var client = new SystemServiceClient(config);
        AhsokaRuntime.Default.StartEndPoints(client);

       Console.WriteLine($"Connecting to {config.DataURI}...");

        int exitCode = 0;
        if (!WaitForConnection(client))
        {
            Console.WriteLine("Not connected.");
            exitCode = 1;
        }
        else
        {
            var progress = new LocalProgress<FileTransmitProgressInfo>(info =>
            {
                // Messages are of the form "..... 25% Uploaded", so printing info.Percentage is redundant
                Console.WriteLine(info.Message);
            });

            MessageReplyStatus status;
            using (var stream = new FileStream(applicationFile, FileMode.Open, FileAccess.Read))
            {
                status = client.LoadApplicationAsync(stream, true, progress).Result;  // blocks
            }

            if (status != MessageReplyStatus.Success)
            {
                Console.WriteLine($"LoadApplication failed: {status}");
                exitCode = 1;
            }
        }

        AhsokaRuntime.Default.StopAllEndPoints();

        if (exitCode != 0)
            Environment.Exit(exitCode);
    }

    private static bool WaitForConnection(SystemServiceClient client)
    {
        const int timeoutMs = 2000;
        GetHardwareInformationReply hardwareInfo = null;
        for (int elapsedMs = 0; elapsedMs < 60000; elapsedMs += timeoutMs)
        {
            hardwareInfo = client.GetHardwareInformation(timeoutMs);
            if (hardwareInfo != null)
                return true;
            client.UpdateConfiguration(client.ServiceConfig);  // Forces socket reconnection
        }
        return false;
    }

    [CommandLineMethod(@"     --Package: Package as an opv file.  Will use info from 'PackageInfo.json
            [WorkingDirectory] Optional - Base Path to use when Executing Packaging
            [PathToInstallFile] Optional - Specify an alternative 'PackageInfo.json")]
    private static void Package(string workingDirectory, string packageInfoPath)
    {
        if (workingDirectory != null)
            Environment.CurrentDirectory = workingDirectory;

        string path = Environment.CurrentDirectory;
        Console.WriteLine($"Packaging Outputs relative to {path}");
        Console.WriteLine();

        if (packageInfoPath == null)
        {
            foreach (var item in new DirectoryInfo(Environment.CurrentDirectory).GetFiles("*.PackageInfo.json"))
                ProcessPackage(item.Name, false);
        }
        else
        {
            ProcessPackage(packageInfoPath, false);
        }
    }

    [CommandLineMethod(@"     --PackageCustom: Package as an opv file.  Will use info from 'PackageInfo.json
            [WorkingDirectory] - Base Path to use when Executing Packaging
            [PathToInstallFile] - Specify an alternative 'PackageInfo.json'
            [CustomPackagePath] - Path to Customer 'CustomInfo.json'")]
    private static void PackageCustom(string workingDirectory, string packageInfoPath, string customPackagePath)
    {
        if (workingDirectory != null)
            Environment.CurrentDirectory = workingDirectory;

        string path = Environment.CurrentDirectory;
        Console.WriteLine($"Packaging Outputs relative to {path}");
        Console.WriteLine();

        if (packageInfoPath == null)
        {
            foreach (var item in new DirectoryInfo(Environment.CurrentDirectory).GetFiles("*.PackageInfo.json"))
                ProcessPackage(item.Name, true, customPackagePath);
        }
        else
        {
            ProcessPackage(packageInfoPath, true, customPackagePath);
        }
    }

    private static void ProcessPackage(string packageInfoPath, bool customPackage, string customPackageInfo = "")
    {
        CustomPackageInfo customInfo = null;
        if (customPackage)
        {
            if (!File.Exists(customPackageInfo))
            {
                Console.WriteLine($"Path to Custom Package Info was not found at {customPackageInfo}");
                return;
            }

            customInfo = JsonUtility.Deserialize<CustomPackageInfo>(File.ReadAllText(customPackageInfo));
        }


        if (!File.Exists(packageInfoPath))
        {
            Console.WriteLine($"Path to PackageInfo was not found {packageInfoPath} in {Environment.CurrentDirectory}");
            return;
        }

        string signingSignature = null;
        PackageInformation package = PackageInformation.LoadPackageInformation(packageInfoPath);
        if (!string.IsNullOrEmpty(package.PackageSigningCertFile))
        {
            string packageCert = package.GetPackageCertPath();
            if (File.Exists(packageCert))
            {
                DeviceKeyInfo info = JsonUtility.Deserialize<DeviceKeyInfo>(File.ReadAllText(packageCert));
                signingSignature = info.PublicKey;
                Console.WriteLine($"Creating Package with Private Key {package.PackageSigningCertFile}");
                Console.WriteLine($"  Note: Use of a private key requires the device be provisioned with this public key to install.");
                Console.WriteLine($"");
            }
        }

        LocalProgress<PackageProgressInfo> progress = new(info =>
        {
            Console.WriteLine(info.Message);
        });

        Task<PackageCompleteInfo> result = Task.Run(() =>
        {
            // Load InstallComponents
            InstallEngineBase createPackageEngine = InstallEngineBase.GetPackagingEngine(package);

            // Package the file.
            string fileName = createPackageEngine.CreateInstallPackage(signingSignature, progress, customInfo);

            if (!File.Exists(fileName))
            {
                string extraHelp = package.DotNetInfo != null ? "\r\nFor DotNet Applications, check that the project and publish files are specified in the DotNetSetup Tab." : string.Empty;
                return new PackageCompleteInfo()
                {
                    Message = $"\r\nPackage Creation Failed - {fileName}.\r\nPlease verify that the 'Application Output' matches the output of your application compile in your PackageInfoFile.  {extraHelp}"
                };
            }

            PrintManifest(fileName, package.GetPackageCertPath());

            return new PackageCompleteInfo()
            {
                Message = $"Package Creation at {fileName} completed."
            };
        });

        try
        {
            result.Wait();
        }
        catch (Exception)
        {
        }

        Console.WriteLine();
        if (result.IsFaulted)
            Console.WriteLine(result.Exception.Message.ToString());
        else
            Console.WriteLine(result.Result.Message.ToString());
    }


    [CommandLineMethod(@"     --PrintManifest: Print Contents of opv package file
            [PathToPackageFile] Optional = File to print manifest for
            [PathToKeyFile] Optional = Private Key File used to Sign / Encrypt this Package")]
    private static void PrintManifest(string packageFile, string pathToKeyFile)
    {
        IProgress<string> progress = new Progress<string>(info =>
        {
            Console.WriteLine(info);
        });

        string key = null;
        if (File.Exists(pathToKeyFile))
        {
            DeviceKeyInfo info = JsonUtility.Deserialize<DeviceKeyInfo>(File.ReadAllText(pathToKeyFile));
            key = info.PrivateKey;
            Console.WriteLine($"Using Private Device Key ({Path.GetFileName(pathToKeyFile)}) to Read Package");
        }

        InstallEngineBase.PrintPackageManifest(packageFile, key, progress);
    }

    [CommandLineMethod(@"     --Verify: Verify that Factory Folder is Complete and Valid")]
    private static void Verify()
    {
        bool result = SystemInfo.ValidatePlatform();
        if (result)
            Console.WriteLine($"Platform Folder {SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.FactoryRoot)} is Valid");
        else
            Console.WriteLine($"Platform Folder {SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.FactoryRoot)} is Not Valid");
    }

}