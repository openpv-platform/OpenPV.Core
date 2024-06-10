using Ahsoka.Services.Install;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace Ahsoka.Installer.InstallEngine;

internal class StoryboardInstallerPlugin : InstallerPlugin
{
    internal override void CheckPrerequisites(PackageInformation packageInformation, IProgress<PackageProgressInfo> progress, CancellationToken token)
    {
        if (!packageInformation.Licenses.Any(x => x.LicenseName == "Storyboard Runtime") 
            && packageInformation.PlatformFamily.ToString().StartsWith("OpenView"))
        {
            string error = $"Invalid Project Licenses.  In order to generate this package, you must select a Project and 'Include a Storyboard Runtime.'";
            throw new Exception(error);
        }

        if (packageInformation.StoryboardInfo == null)
        {
            progress?.Report(new PackageProgressInfo() { Message = $"Invalid Project Information.  Storyboard Package Information cannot be null with a Storyboard project type." });
            return;
        }
        
        if (packageInformation.DotNetInfo == null)
        {
            progress?.Report(new PackageProgressInfo() { Message = $"Invalid Project Information.  DotNet Package Information  cannot be null with a DotNet project type." });
            return;
        }

        // Validate DotNet Project
        if (packageInformation.DotNetInfo.ProjectPath == null || !File.Exists(packageInformation.DotNetInfo.ProjectPath))
        {
            progress?.Report(new PackageProgressInfo() { Message = $"DotNet Project Path Not Found at  {packageInformation.DotNetInfo.ProjectPath} - Skipping Storyboard Validation" });
            return;
        }

        // Validate Storyboard Project
        string pathToProjectInfo = Path.Combine(Path.GetDirectoryName(packageInformation.GetPackageInfoPath()), Path.GetDirectoryName(packageInformation.DotNetInfo.ProjectPath), "Storyboard.Build.targets");
        if (pathToProjectInfo == null || !File.Exists(pathToProjectInfo))
        {
            progress?.Report(new PackageProgressInfo() { Message = $"Storyboard Project Path (.gde) Not Found at  {packageInformation.DotNetInfo.ProjectPath} - Skipping Storyboard Validation" });
            return;
        }

        string projectFile = File.ReadAllText(pathToProjectInfo);
        XDocument doc = XDocument.Parse(projectFile);

        string storyboardProgramFolder = doc.Descendants("StoryboardProgramFolder").FirstOrDefault()?.Value;
        string storyboardApplicationName = doc.Descendants("StoryboardAppName").FirstOrDefault()?.Value;
        string storyboardExportFolder = doc.Descendants("StoryboardExportFolder").FirstOrDefault()?.Value;
        string storyboardVersion = doc.Descendants("StoryboardVersion").FirstOrDefault()?.Value;

        string storyboardVersionDirectory = Path.Combine(storyboardProgramFolder, "Storyboard_Engine", storyboardVersion);
        if (!Directory.Exists(storyboardVersionDirectory))
            progress?.Report(new PackageProgressInfo() { Message = $"Warning - StoryboardEngine Folder was not found at {storyboardVersionDirectory}" });

        string outputBaseDirectory = packageInformation.GetApplicationFullPath();
        string storyboardEngineOutputDirectory = Path.Combine(outputBaseDirectory, "Storyboard", packageInformation.PlatformFamily.ToString(), "sbengine");
        if (!Directory.Exists(storyboardEngineOutputDirectory))
            progress?.Report(new PackageProgressInfo() { Message = $"Warning - StoryboardEngine Output Folder was not found at {storyboardEngineOutputDirectory}. UI may not start." });

        string storyboardAppOutput = Path.Combine(outputBaseDirectory, storyboardExportFolder, storyboardApplicationName + ".gapp");
        if (!File.Exists(storyboardAppOutput))
            progress?.Report(new PackageProgressInfo() { Message = $"Warning - Storyboard Application (.gapp)  was not found at {storyboardAppOutput}. UI may not start." });

    }

}
