# Developing Ahsoka.Platform 
This guide will assist in understanding and setting up tools for developing internal components for Ahsoka / OpenPV.  While other setups are possible, the guide below is the default / recommended tools for development.

# Getting Started

1.	Prerequisites
2.	Contribute
3.  Pipelines & Public Builds


# Prerequisites

- C++ Prerequisites - C++ Development takes place in a docker container called openview/openview_sdk.  This docker image is maintained by the Ahsoka Platform  development team (Emerging Platforms) and includes the dependencies and latest toolchain which matches the latest Distribution Image.

    - Docker - You should be familiar with Docker and have a Docker Engine running on your desktop (Docker Desktop or other docker daemon) when developing C++ components (or compiling C# Components). The Windows Docker Desktop application requires Windows Subsystem for Linux (WSL) feature enabled.
    - Visual Studio Code - VS Code is the preferred development tool for Linux Development (Ubuntu or STM)
    - (Deprecated) Visual Studio C++ Compiler - To build the full Ahsoka System you should have Visual Studio with C++ installed.  When building on windows (CLI or VSCode) this compiler is used. (Note that After 1.247, this is not needed.)
    - (Deprecated) CMAKE - You should have CMAKE installed as the C++ Components build using CMAKE by default on Windows and Linux. (Note that After 1.247, this is not needed.)

- C# Prerequisites - C# Development takes place in Visual Studio 2022 for Windows.  Note that all C++ development prerequisites are needed here as well as the default compile requires the C++ components be built for Ahsoka.Core.

    - Visual Studio 2022 Release 17.4+ - C# is currently building with DotNetCore 7.0 which is supported in 17.4 or later.
    - WIX (WIX Toolset) - This installer framework is used to build the Windows Installer executables required when building the Release build of the main solution.    
      - Note that while you can build debug, you will get errors when opening the solution without this add in.   [Its located here....](https://wixtoolset.org)
      - Starting in 1.252, you can install WIX with the command "dotnet tool install --global wix"   You will also want to install the HeatWave Visual Studio Extension
	- Install the dotnet-project-licenses tool: "dotnet tool install --global dotnet-project-licenses"

# Contribute
 
**Ahoska.Interop (C++)** - To setup your system for C++ Development do the following...

1. Install Prerequisites for C++ (Note that After 1.247, this is not needed.)
2. Launch Docker Desktop or an Equivalent Daemon
3. Download Ahsoka.Platform (this repository)
4. Update the repo submodules if using git CLI
5. Building Binaries
   
    *To Build on Windows)*
     - Launch VS Code and Open the the root folder for Ahsoka.Platform.   VSCode will install a set of plugins.  If not you can install them by hand.   The recommended plugins are C++, CMAKE and RemoteExplorer.
     - Reconfigure the project by selecting your preset from the toolbar (WindowsDebug or WindowsRelease.... for example).  You can now build locally using the "Build" toolbar item.
     - Run and Debug toolbar items should also be available for running the various test executables (Ahsoka.Client.Test or Ahsoka.Storyboard.Test)


    *To Build on Linux*
     - Launch VS Code.
     - From the Remote Explorer select "Open Folder in Container".   Select the same root folder for Ahsoka.Platform.   VSCode launch (and possibly download) the Container for your image with the name Ahsoka.Platform.  VSCode will also install the relevant plugins for the container including C++ & CMAKE.
     - Reconfigure the project by selecting your preset from the toolbar. 4 Presets are included 
     - Linux Debug for building / debugging in Ubuntu
       - OpenViewST Debug for building debug binaries for the OpenViewDisplays.
       - OpenViewST Release for building release binaries for the OpenViewDisplays
     - Run and Debug toolbar items should also be available for Linux Debug for running the various test executables (Ahsoka.Client.Test or Ahsoka.Storyboard.Test)


**Ahsoka.Platform (C#)** - To setup your system for C# Development do the following...

1. Install Prerequisites for C++ and C#
2. Launch Docker Desktop or an Equivalent Daemon (Docker Desktop requires Windows Subsystem for Linux (WSL) feature enabled.)
3. Download Ahsoka.Platform (this repository)
4. Update the repo submodules if using git CLI (The esp-hosted repo may require esp-hosted/docker/build_firmware.sh to be made executable.)
5. Open Visual Studio 2022 and Open the Ahsoka.Platform solution found in the Ahsoka.Platform folder.
6. Right click on the project "Ahsoka.BuildPreReqs" to build all container based packages.  This now builds the following sub-projects located in the PreRequisites Folder.

  - Ahsoka.BuildCoProcessor - This build uses ST CubeIDE to build the CoProcessor Firmware for use with Real Time CAN.
  - Ahsoka.BuildESP32 - This build creates firmware or the ESP32 Module used for WiFi and Bluetooth.
  - Ahsoka.BuildInterop - This project builds the C++ Interop DLL's (so's) for use with Ahsoka.Core.

7. Select Debug or Release from the Configuration Manager with "Any CPU" selected and Build your solution.  Multiple targets are used when testing Ahsoka.Platform

   - Ahsoka.CustomerTools - Builds the Customer Tools UI.
   - Ahsoka.CommandLine - Builds the command line tools and used when debugging functionality
   - Ahsoka.InstallerUI - Builds the Installer Application.
   - Ahsoka.PublishOutputs - Builds the Platform Support Packages (OpenView, Ubuntu, Windows)

Note that the PreReqs project does NOT build automatically.  Since this project takes 5-10 minutes, its recommended you build it one time and as needed after that (Clean and Build Ahsoka.PreReqs).   The project Ahsoka.PublishOutputs will build automatically the first time you build the solution.  If you need to build these dependency packages again, you must clean Ahsoka.PreRequisites or Ahsoka.PublishOutputs.    



# Pipelines & Component Delivery
 
All components in Ahsoka.Platform use Pipelines to contribute to Public Delivery.  At the current time, there are 4 primary public deliveries made of the following components

**Ahsoka.CustomerTools for Windows / Ubuntu** - Customer Tools is the download that most users will recieve when they want to develop applications using OpenPV.  All of the artifacts that make up OpenPV are created as part of the Ahsoka.Services pipeline.   This pipeline executes the following high level steps

1. Build Ahsoka.Interop - This package of platform specific libraries is created using containers or local compilers to build libraries which are needed to access low level components in the Distribution or system libraries only avaialble in C++.  In addition, this build creates any target support packages needed for various platforms.
2. Build Ahsoka.Platform - This package produces following outputs

   - Ahsoka.CustomerTools Installer (.msi) for Windows which is uploaded to PowerVision Spark
   - Ahsoka.CustomerTools Installer (.deb) for Ubuntu which is uploaded to an APT repository in Azure Storage
   - TargetSupport Packages for each Platform (Windows, Ubuntu, OpenView).  Each package may include the following items
     - Ahsoka.CommandLine - Commandline tools used to install, package and execute various OpenPV Tasks.
     - Ahsoka.InstallerUI - The Installer UI used on the target to execute installs.
     - Ahsoka.SDK - C++ SDK's used for developing with native toolchains such as QT.
   - Platform Plugins are packages of support files that are downloadable in Customer Tools.  Items such as image libraries and demo projects are created in this step.
   - Ahsoka.Core Nuget Package(s) are packages that include the cross platform libraries used for customers that are using C# based Applications.  

   
# Ahsoka Services Framework
 
[Ahsoka Service Framework](Understanding%20Ahsoka%20Service%20Architectures.md)

