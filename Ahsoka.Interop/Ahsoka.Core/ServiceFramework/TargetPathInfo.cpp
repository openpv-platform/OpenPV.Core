#include <iostream>
#include <string>
#include <unistd.h>
#include <cstdio>
#include <cstring>
#include "TargetPathInfo.h"

using namespace AhsokaCore;

namespace AhsokaCore
{
    const string AppInfoFolder = "/.appinfo";
    const string CoProcessorFolder = ".coprocessor/";
    const string AppNameFile = "/AppName.txt";
    const string AppVersionFile = "/AppVersion.txt";
    const string AppTypeFile = "/AppType.txt";
    const string AppDescriptionFile = "/AppDescription.txt";

        TargetPathInfo::TargetPathInfo(string platformRootPath)
        {
            rootPath = platformRootPath;
        }

        string TargetPathInfo::GetRootPaths(RootPaths path)
        {
            switch (path) 
            {
                case CloudRoot:
                    return GetAhsokaPath("cloud");
                case ApplicationRoot:
                    return GetAhsokaPath("current");
                case BackupRoot:
                    return GetAhsokaPath("backup");
                case DependencyRoot:
                    return GetAhsokaPath("dependency");
                case TempRoot:
                    return GetAhsokaPath("tmp");
                case FactoryRoot:
                    return GetAhsokaPath("factory");
                case FieldRoot:
                    return GetAhsokaPath("field");
                case LogRoot:
                    return GetAhsokaPath("log");
                case ServiceDataRoot:
                    return GetAhsokaPath("service");
                default:
                    return "";
            }
            
        }

        string TargetPathInfo::GetInstallerPath(InstallerPaths path, bool factory)
        {
            
            string factoryRoot = GetRootPaths(FactoryRoot);
            string fieldRoot = GetRootPaths(FieldRoot);
            string appRoot = GetRootPaths(ApplicationRoot);

            switch (path)
            {
                case ApplicationSigningKeyPath:
                    return fieldRoot.append("/signing_key.appcert");

                case WatchFolder:
                    return GetAhsokaPath("watcher");

                case InstallerUIRoot:
                    if (factory)
                        return factoryRoot.append("/installerui");
                    else
                        return fieldRoot.append("/installerui");

                case InstallerUIExecutablePath:
                    if (factory)
                        return factoryRoot.append("/installerui/Ahsoka.Installer.UI");
                    else
                        return fieldRoot.append("/installerui/Ahsoka.Installer.UI");

                case CommandLineRoot:
                    return factoryRoot.append("/commandline");

                case CommandLineExecutablePath:
                    return factoryRoot.append("/commandline/Ahsoka.CommandLine");

                case BootLogoFileName:
                    return "/usr/share/psplash/splash.png";

                case ApplicationVersion:
                    return appRoot.append(AppInfoFolder).append(AppVersionFile);
                
                case ApplicationDescription:
                        return appRoot.append(AppInfoFolder).append(AppDescriptionFile);
                
                case ApplicationName:
                    return appRoot.append(AppInfoFolder).append(AppNameFile);
                
                case ApplicationType:
                    return appRoot.append(AppInfoFolder).append(AppTypeFile);
                
                case InstallerVersion:
                    if (factory)
                        return factoryRoot.append("/installerui/InstallerVersion.txt");
                    else
                        return fieldRoot.append("/installerui/InstallerVersion.txt");
            }

            return factoryRoot;
        }


        string TargetPathInfo::GetFactoryPath(FactoryPaths path)
        {
            string factoryRoot = GetRootPaths(FactoryRoot);
            string factoryHardwareRoot = factoryRoot + "/hardware";

            switch (path)
            {
                case HardwareRoot:
                    return factoryHardwareRoot;
                case HardwareInformationFile:
                    return factoryHardwareRoot.append("/HardwareConfiguration.json");
                case HardwareDeviceSigningKeyFile:
                    return factoryHardwareRoot.append("/DeviceKey.cert");
                case HardwareDeviceSigningKeyPEMFile:
                    return factoryHardwareRoot.append("/DeviceKeyPEM.cert");
                case HardwareDeviceSigningPublicKeyFile:
                    return factoryHardwareRoot.append("/DevicePublicKey.cert");
                case HardwareDeviceSigningPublicKeyPEMFile:
                    return factoryHardwareRoot.append("/DevicePublicKeyPEM.cert");
                case HardwareManifestFile:
                    return factoryHardwareRoot.append("/Manifest.info");
                case HardwareManifestSignatureFile:
                    return factoryHardwareRoot.append("/Manifest.sig");
                case ModuleLicenseFile:
                    return factoryHardwareRoot.append("/ModuleLicenseInfo.pbuff");
                default:
                    return factoryRoot;
            };
        }

        string TargetPathInfo::GetServiceDataPath(ServiceDataPaths path)
        {
            string serviceDataRoot = GetAhsokaPath("service");

            switch (path)
            {
                case NetworkProfileFolder:
                    return serviceDataRoot.append("/network/profiles");
                default:
                    return serviceDataRoot;
            };
        }

        string TargetPathInfo::GetServiceDataPath(string serviceName)
        {
            string serviceDataRoot = GetAhsokaPath("service");
            return  serviceDataRoot.append("/{serviceName}");
        }


        string TargetPathInfo::GetAhsokaPath(string path)
        {
            auto newPath = rootPath;
            auto folder = newPath.append("/").append(path);
            return folder;
        }

}