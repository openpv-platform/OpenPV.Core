using Ahsoka.Installer;
using Ahsoka.Services.Can;
using Ahsoka.System;
using System;

namespace Ahsoka.Commands;

[CommandLinePlugin]
internal static class CanCommands
{
    [CommandLineMethod(@"     --GenerateCalibrationFromDBC: Generate Configuration File from DBC
            [PathToDBC] Path to OpenPV CAN Calibration File (.cancalibration.json)
            [PathToDestinationFile] Path to Output OpenPV CAN Calibration File (.cancalibration.json)")]
    private static void GenerateCalibrationFromDBC(string pathToDBC, string pathToDestinationFile)
    {
        CanMetadataTools.GenerateCalibrationFromDBC(pathToDBC, pathToDestinationFile);
    }

    [CommandLineMethod(@"     --GenerateCANClasses: Generate Model Classes from Configuration File
            [PathToPackageFile] Path to PackageInfoFile for the projecte (.PackageInfo.json
            [PathToOutputFolder] Folder where Class Definitions will be created
            [NameSpace] NameSpace for Output Classes
            [BaseClass] Base Class (or View Model) for Output Classes
            [Language] Dotnet or Cpp Supported")]
    private static void GenerateCANClasses(string pathToCalibrationFile, string pathToOutputFolder, string nameSpace, string baseClass, string language)
    {
        CanMetadataTools.GenerateMessageClasses(pathToCalibrationFile, pathToOutputFolder, nameSpace, baseClass, Enum.Parse<ApplicationType>(language));
    }
}