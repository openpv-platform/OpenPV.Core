using Ahsoka.Installer;
using Ahsoka.Storyboard;
using Ahsoka.System;
using Ahsoka.Utility;
using ELFSharp.MachO;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Ahsoka.Commands;

[CommandLinePlugin]
internal static class StoryboardCommands
{
    [CommandLineMethod(@"     --GenerateStoryboardClasses: Generate Classes for a Storyboard Project
            [PathToSource] Path to Storyboard CustomEvents.evt file
            [PathToDestinationFolder] Folder where Class Definitions will be created
            [NameSpace] NameSpace for Output Classes
            [BaseClass] Base Class (or View Model) for Output Classes")]
    private static void GenerateStoryboardClasses(string pathToSource, string pathToDestinationFolder, string nameSpace, string baseClass)
    {
        StoryboardTools.GenerateMessageClasses(pathToSource, pathToDestinationFolder, nameSpace, baseClass);
    }
}

internal class StoryboardCodeGenerator : IExtensionGenerator
{
    internal static readonly string ConfigFile = "StoryboardConfiguration.json";

    public void GetCommands(PackageInformation packageInfo, Dictionary<string, GeneratorCommandType> commandsToExecute, CommandTypes commandTypes)
    {
        string configFile = Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), ConfigFile);

        if (File.Exists(configFile) && packageInfo.UserInterfaceType == UserInterfaceType.Storyboard)
        {
            var configInfo = JsonUtility.Deserialize<StoryboardDevelopmentConfiguration>(File.ReadAllText(configFile));
            
            if (commandTypes.HasFlag(CommandTypes.ModelGenerators))
            {
                commandsToExecute.Add($"--GenerateStoryboardClasses  \"{configInfo.GeneratorProjectPath}\\events\\custom.evt\" \"{configInfo.GeneratorOutputFolder}\"  \"{configInfo.GeneratorNameSpace}\" \"{configInfo.GeneratorModelBaseClass}\"",
                                      GeneratorCommandType.AhsokaCommandLine);
            }

            if (commandTypes.HasFlag(CommandTypes.BuildCommands))
            {
                string findExecutable = FindExecutable(configInfo.StoryboardAppProgramFolder, "Crank Storyboard");
                commandsToExecute.Add($"\"{findExecutable}\"  -application com.crank.gdt.ui.gappexport -nosplash model={configInfo.GeneratorProjectPath}/{configInfo.StoryboardAppName}.gde output={configInfo.StoryboardAppExportFolder}/{configInfo.StoryboardAppName}.gapp",
                                      GeneratorCommandType.ShellCommand);
            }
        }
    }

    private string FindExecutable(string storyboardAppProgramFolder, string fileName)
    {
        var list =  Directory.EnumerateFiles(storyboardAppProgramFolder,fileName + "*", SearchOption.AllDirectories);
        return list?.FirstOrDefault();
    }
}