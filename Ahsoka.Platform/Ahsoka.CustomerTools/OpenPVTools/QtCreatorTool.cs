using Ahsoka.Utility;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ahsoka.CustomerTools;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
internal class QtCreatorTool : OpenPVToolBase
{
    public QtCreatorTool()
    {
        this.IncludedProfiles = PluginsViewModel.profileWSL;
        this.PackageType = "Software Install";
        this.CanLaunch = true;
        this.CanInstall = true;
        this.InstallText = "This process will install QT Creator on your local OS to enable QT Development.";
    }

    public override async Task InstallTool(IToolViewModel toolVM)
    {
        string pathToScript = Path.Combine(LocalPath, "InstallQtCreator.sh");
        string executable, args;

        if (OperatingSystem.IsLinux())
        {
            executable = "/bin/bash";
            args = $"-c \"xterm -fs 14 -fa DejaVuSansMono -e 'chmod 711 {pathToScript} && sudo {pathToScript}'\"";
        }
        else
        {
            _ = toolVM.MainViewModel.Dialog.ShowDialog("Platform Error", "QT Native not supported on Windows", "Ok");
            return;
        }
        ProcessUtility.RunProcessInWindow(executable, args, true);
    }

    public override async Task LaunchTool(IToolViewModel toolVM)
    {
        string pathToScript = Path.Combine(LocalPath, "LaunchQtCreator.sh");
        string executable, args;

        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            executable = "/bin/bash";
            args = $"-c \"chmod 711 {pathToScript} && {pathToScript}\"";
        }
        else
        {
            _ = toolVM.MainViewModel.Dialog.ShowDialog("Platform Error", "QT Native not supported on Windows", "Ok");
            return;
        }
        ProcessUtility.RunProcessInWindow(executable, args);
    }
}
