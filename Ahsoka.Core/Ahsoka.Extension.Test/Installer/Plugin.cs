using Ahsoka.Services.Install;
using System;
using System.Threading;

namespace Ahsoka.Installer.InstallEngine;

internal class SampleInstallerPlugin : InstallerPlugin
{
    public override void CheckPrerequisites(PackageInformation packageInformation, IProgress<PackageProgressInfo> progress, CancellationToken token)
    {
       
    }

}
