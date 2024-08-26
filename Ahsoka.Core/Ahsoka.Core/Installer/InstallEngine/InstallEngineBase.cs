#pragma warning disable CS1591
using Ahsoka.Installer.Components;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Install;
using Ahsoka.Services.System;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static NetMQ.NetMQSelector;

namespace Ahsoka.Installer.InstallEngine;


[PlatformFamily(PlatformFamily.Any, "")]
public class InstallEngineBase
{
    #region Fields
    readonly PackageHeader packageInfo;
    readonly List<InstallEngineComponent> components = new();

    private static string PrivateSigningkey => Properties.Resources.UntrustedPrivateKeyPEM;
    private static string PublicSigningkey => Properties.Resources.UntrustedPublicKeyPEM;

    readonly CancellationTokenSource cancelToken = new();

    #endregion

    #region Properties
    public PackageContents PackageContents => packageInfo as PackageContents;
    public PackageInformation PackageInformation => packageInfo as PackageInformation;
    internal List<InstallEngineComponent> Components => components;
    #endregion

    #region Factory Methods
    public static InstallEngineBase GetPackagingEngine(PackageInformation packageInfo)
    {
        var type = SelectInstallEngine(packageInfo.PlatformFamily, packageInfo.PlatformQualifier);
        return  Activator.CreateInstance(type, packageInfo) as InstallEngineBase;
    }

    public static InstallEngineBase GetInstallEngine(Stream fileStream, string privateKey = null)
    {
        string appKey = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.ApplicationSigningKeyPath);
        if (File.Exists(appKey))
        {
            DeviceKeyInfo info = JsonUtility.Deserialize<DeviceKeyInfo>(File.ReadAllText(appKey));
            privateKey = info.PrivateKey;
            Console.WriteLine($"Using Private Device Key ({Path.GetFileName(appKey)}) to Read Package");
        }

        privateKey ??= PrivateSigningkey;

        PackageContents packageContents = PackageContents.ReadPackageContents(fileStream, privateKey);
        var type = SelectInstallEngine(packageContents.PlatformFamily, packageContents.PlatformQualifier);
        return Activator.CreateInstance(type, packageContents) as InstallEngineBase;
    }
    #endregion

    #region Methods
    public static void PrintPackageManifest(string packageFile, string privateKey = null, IProgress<string> progress = null)
    {
        using FileStream stream = new(packageFile, FileMode.Open);

        InstallEngineBase installEngine = InstallEngineBase.GetInstallEngine(stream, privateKey);

        PackageContents contents = installEngine.PackageContents;

        StringBuilder builder = new();
        builder.AppendLine("");
        builder.AppendLine($"Contents of {Path.GetFileName(packageFile)}");
        builder.AppendLine("");
        builder.AppendLine($"  ApplicationID:         {contents.ApplicationId}");
        builder.AppendLine($"  Package Type:          {contents.PackageType}");
        builder.AppendLine($"  Platform Family:       {contents.PlatformFamily}");
        builder.AppendLine($"  Platform Qualifier:    {contents.PlatformQualifier}");
        builder.AppendLine($"  Header CRC:            {contents.HeaderCRC}");
        builder.AppendLine($"  Signature Is Valid:    {contents.IsSignatureValid}");
        builder.AppendLine($"  Contents Count:        {contents.Components.Count}");
        builder.AppendLine("");
        builder.AppendLine(String.Format("{0,-20} {1,-10} {2, 10} {3,20}", "Package", "Size", "CRC", "Version"));

        foreach (var item in contents.Components)
        {
            string output = String.Format("{0,-20} {1,-10} {2, 10} {3,20}",
                installEngine.Components.FirstOrDefault(x => x.ComponentType == item.ComponentType).ComponentName,
                StringUtilities.GetByteFormattedSize(item.DataSize),
                item.DataCrc,
                item.Version);

            builder.AppendLine(output);
        }

        builder.AppendLine("");
        builder.AppendLine($"End of PackageOutput");
        builder.AppendLine("");

        progress.Report(builder.ToString());
    }

    public InstallEngineBase(PackageHeader packageInfo)
    {
        // Capure PackageInfo
        this.packageInfo = packageInfo;

        // Init Security Components
        LoadComponents(packageInfo, components);
    }

    public string CreateInstallPackage(string pemKey = null,
        IProgress<PackageProgressInfo> progress = null,
        bool isCustomInstall = false)
    {
          bool appUpdate = true;

        if (!isCustomInstall)
        {
            // Enable All Exceipt Firmware and Installer UI
            foreach (var item in Components)
                item.IsEnabled = !(item is FirmwareUpdateComponent or InstallerUIComponent);

            appUpdate = true;
        }
        else
        {
            foreach (var item in Components)
            {
                if (item is BeginInstallEngineComponent or EndInstallEngineComponent or CommandLineComponent) // Required
                    item.IsEnabled = true;
                else if (item is FirmwareUpdateComponent)
                    item.IsEnabled = this.PackageInformation.IncludeFirmwareUpdate;
                else if (item is StartLogoComponent)
                    item.IsEnabled = this.PackageInformation.IncludeBootLogo;
                else if (item is InstallerUIComponent)
                    item.IsEnabled = this.PackageInformation.IncludeInstallerUI;
                else
                    item.IsEnabled = this.PackageInformation.IncludeApplication;
            }

            appUpdate = this.PackageInformation.IncludeApplication;
        }


        List<InstallerPlugin> installedPlugins = new();
        if (appUpdate)
        {
            RunGeneratorCommands(progress);

            // Create Installer Items
            installedPlugins = RunInstallerPreReqs(progress);
        }

        string applicationPath = PackageInformation.GetApplicationFullPath();
        string outputPath = PackageInformation.GetOutputFullPath(isCustomInstall);

        if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        string outputPathRelative = PackageInformation.GetOutputRelativePath(isCustomInstall);
        progress?.Report(new PackageProgressInfo() { Message = $"Begin Package Creation at {outputPathRelative}" });

        // Check that Output Contains Command
        string pathToExecutable = Path.Combine(applicationPath, PackageInformation.ExecCommand);

        // Check we generated 
        if (!Directory.Exists(applicationPath) || !Directory.EnumerateFiles(applicationPath).Any())
        {
            progress?.Report(new PackageProgressInfo() { Message = $"An Application Folder was not found at {applicationPath}." });
            return "Application Folder Missing or Empty";
        }

        // If Not a Script (look for spaces?, check exe.
        if (!PackageInformation.ExecCommand.Contains(' ') && !File.Exists(pathToExecutable))
        {
            progress?.Report(new PackageProgressInfo() { Message = $"An Application Executable was not found at {pathToExecutable}." });
            return "Application Executable Missing";
        }

        // Select Key
        pemKey ??= PublicSigningkey;

        // Create Components to include in the package
        Dictionary<Guid, PackageComponent> data = new();

        try
        {
            // Execute Installer Pack Methods which add components if needed.
            foreach (var component in Components)
            {
                if (!component.IsEnabled)
                    continue;

                if (cancelToken.IsCancellationRequested)
                    return "Process Cancelled";

                var componentData = component.Pack(applicationPath, installedPlugins, PackageInformation, progress);
                if (componentData != null)
                    data.Add(component.ComponentType, componentData);
            }

            // Open the File
            using FileStream fs = new(outputPath, FileMode.Create);

            // Write File
            WriteOutputPackage(PackageInformation, fs, data.Values.ToList(), pemKey);
        }
        catch { throw; }
        finally
        {
            // Run Cleanup Processes.
            foreach (var component in Components)
                if (data.TryGetValue(component.ComponentType, out PackageComponent componentData))
                    component.Cleanup(applicationPath, componentData, PackageInformation, progress);
        }

        progress?.Report(new PackageProgressInfo() { Message = $"Finished Package Creation at {outputPathRelative}" });

        return outputPath;
    }

    private void RunGeneratorCommands(IProgress<PackageProgressInfo> progress)
    {
        // Execute Commands 
        progress.Report(new PackageProgressInfo() { Message = "Running Code Generators..." });

        foreach (var info in PackageInformation.ServiceInfo.RuntimeConfiguration.ExtensionInfo)
        {
            var list = Extensions.GetCommands(PackageInformation, info.ExtensionName, CommandTypes.ModelGenerators | CommandTypes.BuildCommands);
            foreach (var command in list)
                if (command.Value == GeneratorCommandType.AhsokaCommandLine)
                {
                    // Split Arguments
                    var args = new List<string>();
                    args.AddRange(Regex.Matches(command.Key, @"[\""].+?[\""]|[^ ]+")
                                    .Cast<Match>()
                                    .Select(m => m.Value.Trim('"'))
                                    .ToList());

                    CommandLine.Execute(args.ToArray());
                }
                else
                {
                    string pathToCommand = command.Key.Substring(0, command.Key.IndexOf('"', 2) + 1).Trim('"');
                    string processArgs = command.Key.Substring(command.Key.IndexOf('"', 2) + 2);

                    progress.Report(new PackageProgressInfo() { Message = $"Executing Shell Command from..." });
                    progress.Report(new PackageProgressInfo() { Message = $"   {pathToCommand}" });
                    progress.Report(new PackageProgressInfo() { Message = $" " });
                    progress.Report(new PackageProgressInfo() { Message = $"With Arguments..."});
                    progress.Report(new PackageProgressInfo() { Message = $"   {processArgs}" });
                    progress.Report(new PackageProgressInfo() { Message = $" " });

                    IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
                    {
                        progress.Report(new PackageProgressInfo() { Message = info.Data});
                    });

                    if (OperatingSystem.IsLinux())
                        ProcessUtility.RunProcessAsync("chmod", "+x " + pathToCommand, Environment.CurrentDirectory, progressText, CancellationToken.None);

                    ProcessUtility.RunProcessAsync(pathToCommand, processArgs, Environment.CurrentDirectory, progressText, CancellationToken.None);
                }
        }
    }

    private List<InstallerPlugin> RunInstallerPreReqs(IProgress<PackageProgressInfo> progress)
    {
        List<InstallerPlugin> installedPlugins = new List<InstallerPlugin>();

        // Add App Plugins Last
        installedPlugins.Add(InstallerPlugin.GetApplicationPlugin(PackageInformation.ApplicationType));
        installedPlugins.Add(new StartupInstallerPlugin()); // Generates Start Scripts.

        foreach (var item in Extensions.GetExtensions<InstallerPlugin>())
            installedPlugins.Add(item);

        // Remove Nulls
        installedPlugins = installedPlugins.Where(x => x != null).ToList();

        // Run Prerequisite Checks
        foreach (var item in installedPlugins)
            item.CheckPrerequisites(PackageInformation, progress, cancelToken.Token);
        return installedPlugins;
    }

    public InstallResult ValidatePackages(string passKey = null,
        IProgress<InstallProgressInfo> progress = null)
    {
        if (PackageContents == null)
            throw new InvalidOperationException("The install engine must be opened with PackageContents or Stream to validate packages  ");

        if (PackageContents.SecurityMode is SecurityModes.PassKeyOnInstall or SecurityModes.PassKeyOnOverwrite)
        {
            if (PackageContents.SecurityPassKey != passKey)
                return InstallResult.PassKeyIncorrect;
        }

        foreach (var item in PackageContents.Components)
            if (ValidateComponent(PackageContents.ContentsStream, item) == false)
                return InstallResult.InvalidComponentFile;

        return InstallResult.Success;
    }

    public InstallResult ExtractPackage(Guid packageId,
        Stream outputStream,
        IProgress<InstallProgressInfo> progress = null)
    {
        if (PackageContents == null)
            throw new InvalidOperationException("The install engine must be opened with PackageContents or Stream to validate packages  ");

        var decryptKey = CreateDecryptKey(PackageContents, PackageContents.PrivateKey);

        // Get Content Info
        var contentItem = PackageContents.Components.Where(x => x.ComponentType == packageId).FirstOrDefault();
        if (contentItem != null)
        {
            if (ValidateComponent(PackageContents.ContentsStream, contentItem) == false)
                return InstallResult.InvalidComponentFile;

            // Seek to beginning of Package Data
            PackageContents.ContentsStream.Seek(contentItem.DataFileOffset, SeekOrigin.Begin);

            // Decrypt the data
            long bytesRemaining = contentItem.DataSize;
            using (var cryptoStream = new CryptoStream(PackageContents.ContentsStream, decryptKey.CreateDecryptor(), CryptoStreamMode.Read, true))
            {
                var bytes = new byte[64 * 1024];

                while (bytesRemaining > 0)
                {
                    int bytesToRead = Math.Min((int)bytesRemaining, bytes.Length);
                    int count = cryptoStream.Read(bytes, 0, bytesToRead);

                    if (count > 0)
                    {
                        outputStream.Write(bytes, 0, count);
                        bytesRemaining -= count;
                    }
                    else
                    {
                        // If we are not at the end of the file, this is a failure....otherwise cryptostream took care of padding for us.
                        if (PackageContents.ContentsStream.Position < PackageContents.ContentsStream.Length)
                            throw new ApplicationException("Error Reading Encrypted Package Data");
                        else
                            bytesRemaining = 0;
                    }
                }
            }

            // Because we use "substreams" in this file format, we must trim the AES Padding at the end
            // except when the package is the last package then the CryptoStream takes care of it in the last read
            if (PackageContents.ContentsStream.Position < PackageContents.ContentsStream.Length)
            {
                outputStream.Position -= 1; // Back up 1
                int trimLength = outputStream.ReadByte();
                outputStream.SetLength(outputStream.Length - trimLength);
            }
        }

        return InstallResult.Success;
    }

    internal Dictionary<string, string> GetComponentAttributes(Guid componentId)
    {
        var contentItem = PackageContents.Components.Where(x => x.ComponentType == componentId).FirstOrDefault();
        return contentItem.Attributes;
    }

    internal InstallCompleteInfo RunInstall(InstallService service, string passKey = null)
    {
        InstallResult finalResult = InstallResult.Success;
        InstallCompleteInfo installCompleteInfo;

        var context = new InstallContext(this, service, passKey);
        context.OpenLog();

        try
        {
            // Validate the Package
            service.SendLog(new InstallLogInfo() { LogMessage = "Validating Package", LogMessageType = LogMessageType.Information });

            finalResult = ValidatePackages(passKey);

            if (finalResult == InstallResult.Success)
            {
                context.MoveNext();

                // Execute the Package
                foreach (var item in PackageContents.Components)
                {
                    // We are running this component so Skip it.
                    if (item.ComponentType == CommandLineComponent.CommandLineComponentType)
                        continue;

                    var component = Components.Where(x => x.ComponentType == item.ComponentType).FirstOrDefault();
                    var result = component.InstallComponent(context, item);

                    if (result != InstallResult.Success)
                    {
                        finalResult = result;
                        break;
                    }

                    // Add Complete Step Here.
                    context.MoveNext();
                }

                service.SendInstallProgress(new InstallProgressInfo() { Message = $"Install Completed", PercentComplete = 100 });

                // Close the Stream
                using (PackageContents.ContentsStream) { }
            }
        }
        catch (Exception ex)
        {
            finalResult = InstallResult.ExceptionThrownDuringInstall;
            service.SendLog(new InstallLogInfo() { LogMessage = ex.Message, LogMessageType = LogMessageType.Error });
        }
        finally
        {
            // Send Results
            installCompleteInfo = CreateFinalResultMessage(finalResult);

            // Cleanup Previous Install Data
            service.SendLog(new InstallLogInfo() { LogMessage = "Cleaning Previous Install Data", LogMessageType = LogMessageType.Information });
            CleanupPreviousInstalls(service);

            context.CloseLog();
        }

        return installCompleteInfo;
    }

    public void Cancel()
    {
        cancelToken.Cancel();
    }
    #endregion

    #region Protected Interface
    internal virtual void LoadComponents(PackageHeader packedComponentData, List<InstallEngineComponent> components)
    {
        components.Clear();
        components.Add(new BeginInstallEngineComponent());
        components.Add(new CommandLineComponent());
        components.Add(new ApplicationOutputComponent());

        // Optional Components.
        components.Add(new StartLogoComponent());
        components.Add(new InstallerUIComponent());
        components.Add(new NetworkFirmwareComponent());

        // Add any Extensions
        var results = System.Extensions.GetExtensions<InstallEngineComponent>();
        foreach(var item in results)
        {
            components.Add(item);
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Loading {item.ComponentName} Installer Extension");
        }

        components.Add(new FirmwareUpdateComponent());
        components.Add(new EndInstallEngineComponent());
    }
    #endregion

    #region Internal Methods
    private bool ValidateComponent(Stream packageStream,
        PackageComponent contentItem)
    {
        // Seek to beginning of Package Data
        packageStream.Seek(contentItem.DataFileOffset, SeekOrigin.Begin);

        var crcResult = Crc32.CalculateCrc32(packageStream, false, (int)contentItem.DataSize);

        return crcResult == contentItem.DataCrc;
    }

    private void WriteOutputPackage(PackageInformation installInfo,
        Stream outputStream,
        List<PackageComponent> installs,
        string signingKey)
    {
        // Generate Header 
        PackageHeader header = installInfo.CopyAsPackageHeader(signingKey);

        // Get component streams into byte arrays
        var componentDataBlocks = new Dictionary<PackageComponent, Stream>();
        foreach (var component in installs)
        {
            componentDataBlocks.Add(component, component.Data);
        }

        // Encrypt component data blocks
        byte[] aesIv = null;
        byte[] aesKey = null;
        if (installInfo.PackageType == PackageType.opvEncrypted)
        {
            // Generate AES key and IV, then use it to encrypt the entire package
            using var aes = Aes.Create();

            // This might be redundant. Do it here just to force it
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();
            aes.Padding = PaddingMode.ANSIX923;

            // Capture AES-IV and AES-Key so they can be added to the signature block later
            aesIv = aes.IV;
            aesKey = aes.Key;


            // Replace unencrypted data blocks with encrypted data blocks
            foreach (var component in componentDataBlocks.ToArray())
            {
                if (cancelToken.IsCancellationRequested)
                    return;

                var memoryStream = new MemoryStream();
                using var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write, true);
                component.Value.CopyTo(cryptoStream);
                cryptoStream.FlushFinalBlock();
                componentDataBlocks[component.Key] = memoryStream;
            }
        }

        foreach (var component in componentDataBlocks)
        {
            // Update the data size and CRC
            PackageComponent comp = new()
            {
                ComponentTypeId = component.Key.ComponentTypeId,
                DataSize = component.Value.Length,
                DataCrc = Crc32.CalculateCrc32(component.Value),
                Version = component.Key.Version,
                Attributes = component.Key.Attributes
            };
            header.Components.Add(comp);
        }

        // Pack header
        using MemoryStream headerStream = new();
        ProtoBuf.Serializer.Serialize(headerStream, header);
        var headerData = headerStream.ToArray();

        // Create signature block, which is the CRC (4 bytes), AES IV (16 bytes), and AES Key (32 bytes)
        // encrypted with the PV public key or the customer's private key from their certificate
        List<byte> signatureData = new();
        signatureData.AddRange(BitConverter.GetBytes(Crc32.CalculateCrc32(headerStream)));
        signatureData.AddRange(aesIv);
        signatureData.AddRange(aesKey);
        var signatureDataEncrypted = RSAEncryption.EncryptWithPEMKey(signatureData.ToArray(), signingKey);

        // Write Header
        outputStream.Write(BitConverter.GetBytes(headerData.Length));
        outputStream.Write(headerData);

        // Add Signature Data
        outputStream.Write(signatureDataEncrypted);

        // Now write the component data blocks to the stream
        foreach (var component in componentDataBlocks)
        {
            using (component.Value) // Clearn the Data here
            {
                component.Value.Seek(0, SeekOrigin.Begin);
                component.Value.CopyTo(outputStream);
            }
        }
    }

    private Aes CreateDecryptKey(PackageContents contents, string xmlPrivatekey)
    {
        MemoryStream decryptedBytes = new(RSAEncryption.DecryptWithPEMKey(contents.SignatureData, xmlPrivatekey));
        BinaryReader reader = new(decryptedBytes, Encoding.UTF8, true);
        _ = reader.ReadInt32();
        var id = reader.ReadBytes(16);
        var key = reader.ReadBytes(32);

        var aes = Aes.Create();
        aes.IV = id;
        aes.Key = key;
        aes.Padding = PaddingMode.ANSIX923;

        return aes;
    }

    private static Type SelectInstallEngine(PlatformFamily family, string platformQualifier)
    {
        List<Type> candidateInstallers = new();

        // Load Message Objects
        foreach (var assmwf in ClassLoader.GetSystemAssemblies())
            if (assmwf.TryGetTarget(out var assembly))
                foreach (var type in assembly.GetTypes())
                {
                    var attr = type.GetCustomAttribute<PlatformFamilyAttribute>();
                    if (attr != null)
                    {
                        if (attr.PlatformFamily.HasFlag(family) || attr.PlatformFamily == PlatformFamily.Any)
                            candidateInstallers.Add(type);
                    }
                }

        if (candidateInstallers.Count == 1)
            return candidateInstallers[0];
        else if (candidateInstallers.Count > 1)
            throw new NotImplementedException("Multiple Installers Found");

        return null;
    }

    private static InstallCompleteInfo CreateFinalResultMessage(InstallResult finalResult)
    {
        string message;

        message = finalResult switch
        {
            InstallResult.Success => "Install Completed Successfully",
            InstallResult.PreviousInstallInProgress => "Previous Install is in Progress",
            _ => "Unknown Result",
        };
        return new InstallCompleteInfo() { Message = message, Result = finalResult };
    }

    private void CleanupPreviousInstalls(InstallService service)
    {
        // Search for Temp Files from Previous Installs
        string path = SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.TempRoot);
        service.SendLog(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Temp Path {path}" });

        if (Directory.Exists(path))
            foreach (var file in Directory.GetFiles(path))
                if (File.Exists(file))
                {
                    service.SendLog(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Deleted Temp File {file}" });
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception) { }
                }

        if (Directory.Exists(path))
        {
            var list = new DirectoryInfo(path).GetDirectories().OrderBy(x=>x.CreationTime);
            foreach (var dir in list)
                if (dir == list.Last())
                {
                    service.SendLog(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Skip Delete for Current Temp Files" });
                }
                else
                {
                    service.SendLog(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Deleted Temp Directory {dir}" });

                    try
                    {
                        Directory.Delete(dir.FullName, true);
                    }
                    catch (Exception) { } // Fail Silently.
                }
        }


        // Clear the Logs Folder of all but last 5 logs.
        var logPath = Path.Combine(SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.LogRoot), "installer");
        var logList = Directory.GetFiles(logPath).Order().ToList();
        while (logList.Count > 5)
        {
            string log = logList.First();
            File.Delete(log);
            logList.Remove(log);

            service.SendLog(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Deleted Log File {Path.GetFileName(log)}" });
        }

    }

    #endregion
}


#pragma warning restore CS1591