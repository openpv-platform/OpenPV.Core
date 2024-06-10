using Ahsoka.Services.Install;
using Ahsoka.Utility;
using System;
using System.IO;
using System.Threading;

namespace Ahsoka.Installer.InstallEngine;

internal class DotNetInstallerPlugin : InstallerPlugin
{
    internal override void CheckPrerequisites(PackageInformation packageInformation, IProgress<PackageProgressInfo> progress, CancellationToken token)
    {
        if (packageInformation.DotNetInfo == null)
        {
            progress?.Report(new PackageProgressInfo() { Message = $"Invalid Project Information.  DotNet Package Information  cannot be null with a DotNet project type. " });
            return;
        }

        if (packageInformation.DotNetInfo.PublishProfile == null || packageInformation.DotNetInfo.ProjectPath == null)
        {
            progress?.Report(new PackageProgressInfo() { Message = $"DotNet Application Not Found" });
            return;
        }

        string projectPath = Path.Combine(Path.GetDirectoryName(packageInformation.GetPackageInfoPath()), packageInformation.DotNetInfo.ProjectPath);
        string publishProfilePath = Path.Combine(Path.GetDirectoryName(packageInformation.GetPackageInfoPath()), packageInformation.DotNetInfo.PublishProfile);
        if (File.Exists(projectPath) && File.Exists(publishProfilePath))
        {
            progress?.Report(new() { Message = $"DotNet Application Found: {Path.GetFileNameWithoutExtension(projectPath)}" });
            progress?.Report(new() { Message = $"Executing Build / Publish" });

            IProgress<CommandOutput> progressText = new Progress<CommandOutput>(info =>
            {
                progress?.Report(new() { Message = info?.Data?.ToString() });
            });

            if (projectPath.Contains(' '))
                projectPath = $"\"{projectPath}\"";

            if (publishProfilePath.Contains(' '))
                publishProfilePath = $"\"{publishProfilePath}\"";

            if (OperatingSystem.IsWindows())
                ProcessUtility.RunProcessAsync(@"dotnet", $"publish {projectPath} /p:PublishProfile={publishProfilePath}", null, progressText, token);
            else
                ProcessUtility.RunProcessAsync("/usr/local/share/dotnet/dotnet", $"publish {projectPath} /p:PublishProfile={publishProfilePath}", "/usr/local/share/dotnet", progressText, token);

            progress?.Report(new PackageProgressInfo() { Message = $"Build / Publish Completed" });
        }
    }
}
