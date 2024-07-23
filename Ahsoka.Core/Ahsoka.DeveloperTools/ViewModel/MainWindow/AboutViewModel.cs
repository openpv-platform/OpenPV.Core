using Ahsoka.System;
using Ahsoka.Utility;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Ahsoka.DeveloperTools;

internal class AboutViewModel : ChildViewModelBase<MainViewModel>
{
    readonly MainViewModel parentViewModel;

    public string Version { get; init; }
    public string FullVersion { get; private set; }
    public string Copyright { get; init; }

    public List<LicenseInfo> Licenses { get; init; }

    public LicenseInfo SelectedLicense { get; set; }

    public AboutViewModel(MainViewModel parentViewModel):base(parentViewModel)
    {
        var version = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);

        this.parentViewModel = parentViewModel;
        this.Licenses = SystemInfo.LicenseInfos;
        this.Copyright = version.LegalCopyright;
        this.Version = version.FileVersion;
        this.FullVersion = version.ProductVersion;
    }

    internal void ShowLicense()
    {
        try
        {
            if (SelectedLicense == null) { _ = parentViewModel.Dialog.ShowDialog("Selection Error", "No License Selected", "Ok"); return; }
            if (String.IsNullOrEmpty(SelectedLicense.LicenseUrl)) { _ = parentViewModel.Dialog.ShowDialog("Selection Error", "No Link for Selected License", "Ok"); return; }
            ProcessUtility.RunProcessInWindow(SelectedLicense.LicenseUrl);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    internal void ToggleDebugCLI()
    {
        parentViewModel.DebugCommandLine = !parentViewModel.DebugCommandLine;
    }
    internal void ToggleInternalCLI()
    {
        parentViewModel.UseInternalCommandLine = !parentViewModel.UseInternalCommandLine;
    }
}


