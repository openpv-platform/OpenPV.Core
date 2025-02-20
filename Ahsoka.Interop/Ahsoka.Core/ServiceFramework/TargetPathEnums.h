#pragma once

#include <iostream>
#include <string>
#include <unistd.h>

namespace AhsokaCore
{
    
    enum ServiceDataPaths
    {
        /// <summary>
        /// Path to Networking Data.
        /// </summary>
        NetworkProfileFolder,
    };

    /// <summary>
    /// Paths used for the Installer and Installer Components
    /// </summary>
    enum InstallerPaths
    {
        /// <summary>
        /// Folder which is watched for installs to be installed
        /// </summary>
        WatchFolder,

        /// <summary>
        /// Folder where the Installer Application is Stored
        /// </summary>
        InstallerUIRoot,

        /// <summary>
        /// Path to the Installer UX Executable 
        /// </summary>
        InstallerUIExecutablePath,

        /// <summary>
        /// Directory where the System Command Line Tools are stored.
        /// </summary>
        CommandLineRoot,

        /// <summary>
        /// Path to the Command Line Executable.
        /// </summary>
        CommandLineExecutablePath,

        /// <summary>
        /// Path to the Boot Logo Image File.
        /// </summary>
        BootLogoFileName,

        /// <summary>
        /// Path to the Application Version File
        /// </summary>
        ApplicationVersion,
    
        /// <summary>
        /// Path to the Application ID
        /// </summary>
        ApplicationId,

        /// <summary>
        /// Path to the Application Description File
        /// </summary>
        ApplicationDescription,

        /// <summary>
        ///  Path to the Installer Version File.
        /// </summary>
        InstallerVersion,

        /// <summary>
        /// Path to the Application Name File
        /// </summary>
        ApplicationName,

        /// <summary>
        /// Path to the Application Type File
        /// </summary>
        ApplicationType,

        /// <summary>
        /// Path to AutoStart Service Configuration if enabled.
        /// </summary>
        StartConfig,

        /// <summary>
        /// Path to the CoProcessor Customer Application
        /// </summary>
        CoProcessorApplicationPath,

        /// <summary>
        /// Signing Key used for Decrypting Applications
        /// </summary>
        ApplicationSigningKeyPath,

        /// <summary>
        /// Service Security Info
        /// </summary>
        ServiceSecurityFile,
    };

    /// <summary>
    /// Path to the Root Ahsoka Folders
    /// </summary>
    enum RootPaths
    {
        /// <summary>
        /// Path to the Customer Application
        /// </summary>
        ApplicationRoot,

        /// <summary>
        ///  Path to the last Installed Customer Application Backup
        /// </summary>
        BackupRoot,

        /// <summary>
        ///  Path to any Dependencies installed for a Development Platform.
        /// </summary>
        DependencyRoot,

        /// <summary>
        ///  Path to the Temp Files used by the System.
        /// </summary>
        TempRoot,

        /// <summary>
        /// Path to the Factory Information Folder
        /// </summary>
        FactoryRoot,

        /// <summary>
        /// Path to the Field Information Folder
        /// </summary>
        FieldRoot,

        /// <summary>
        /// Path where System Logs are stored.
        /// </summary>
        LogRoot,

        /// <summary>
        /// Path where Cloud Data is Stored.
        /// </summary>
        CloudRoot,

        /// <summary>
        /// Path to non-volatile Data is store for Services
        /// </summary>
        ServiceDataRoot,
    };


    /// <summary>
    /// Path to the Factory Data Items
    /// </summary>
    enum FactoryPaths
    {
        /// <summary>
        /// Path to the Hardware Info Folder
        /// </summary>
        HardwareRoot,

        /// <summary>
        /// Path to the Hardware Information Folder.
        /// </summary>
        HardwareInformationFile,

        /// <summary>
        /// Path to the Hardware Device Signing Key
        /// </summary>
        HardwareDeviceSigningKeyFile,

        /// <summary>
        /// Path to the Hardware Device Signing Key
        /// </summary>
        HardwareDeviceSigningKeyPEMFile,

        /// <summary>
        /// Path to the Hardware Folder Signature Validation File.
        /// </summary>
        HardwarePackageValidationKeyPEMFile,

        /// <summary>
        /// Path to the Hardware Folder Manifest File
        /// </summary>
        HardwareManifestFile,

        /// <summary>
        /// Path to the Manifest Signature File which validates the Hardware Folder
        /// </summary>
        HardwareManifestSignatureFile,

        /// <summary>
        /// Path to the Application Startup File.
        /// </summary>
        ApplicationScriptFile,

        /// <summary>
        /// Path to the Hardware Public Key file used for encrypting packages.
        /// </summary>
        HardwareDeviceSigningPublicKeyFile,

        /// <summary>
        /// Path to the Hardware Public Key file used for encrypting packages.
        /// </summary>
        HardwareDeviceSigningPublicKeyPEMFile,

        /// <summary>
        /// Path to License Info for this Target
        /// </summary>
        ModuleLicenseFile
    };


}
