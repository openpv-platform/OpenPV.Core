# Developer Tools {#DeveloperTools}

## Plugins{#Plugins}

The Plugin Manager allows you to download optional packages for your development workflow.

**Types of Packages**

1. Software Installation - Optional Installs which assist your workflow.
2. Demo Project - Starter Projects which you can use to bootstrap a new application
3. Target Firmware - Flash Images that can be flashed to your display using the built in DFU mode.
4. SDK Installs - C++ SDK's needed to compile QT or C++ Applications.  Note that they match the coorsponding Target Firmware and should not be mixed / matched.
5. Platform Support - Support for Platforms including Development Platforms such as Windows, Ubuntu, Mac as well as Target Platforms such as OpenViewLinux.

**Download Plugin** - Downloads the Plugin to your Application Data Folder.

**Install Plugin** - If available, this will install the Plugin on your Local Computer.  Note that some Plugins will install on your local WSL Instance on Windows.  For example toolkits, qt and others that require linux will use your WSL instance to install.  

\image{inline} html /Images/PluginManager.png "Plugin Manager" width=750

## Debug Tools {#DebugTools}

Pulls Linux Logs related to each application (unit) that starts with your system.  This tool allows you to quickly access the journals for the installer (to check the install) as well as the running application.  This is useful for seeing console output as well as any other issues with your application.

**Get Application Log** - Shows the unit and console output information from your application once it has been installed.

**Get Installer Log** - Shows the unit and console output information from the visual installer.  This is useful to see the the application has installed successfully.

**Save Log** - Saves the output to a file.

**Clear Journals** - Clears all journals on the connected display.  Note that this clears ALL journals not just the Ahsoka Journals.   Since journals continue until journal space is full this is useful for cleaning up old logs.

\image{inline} html /Images/Debug.png "Debug" width=750


## Display Tools {#DisplayTools}

 Display Tool allows you configure connections to Ahsoka Displays.  This is the list of connections that show in the status bar.

**Add** - Add a new display to the connections list on the left.

**Remove** - Remove the selected display from the connections list.

**Prep** - Clears much of the file system, installs the Visual Software Installer and prepares the system for using Ahsoka Applications.  This is required after doing a Firmware Flash Update as it replaces any factory data that was removed during the flash operation.

**Secure** - Secures the display by turning off the root logins and requiring key based SSH, SCP and sFTP Logins

**Load** - Allows you to "side load" a OPV File directly to the display.

\image{inline} html /Images/Displays.png "Display" width=750
