# Developing OpenPV Extensions

Extensions in OpenPV enable 3rd Party (Internal and External) developers to extend the functionality of OpenPV by adding their own functionality which can run alongside the OpenPV based systems.  There are a number of tasks that can be accomplished with Extensions including adding services, code generators and behaviors to other systems.   We will go over each of the functionalities that can be extended later in this document.  


**Table of Contents**

[Registering OpenPV Extension](#registering-openpv-extension)

[Developing Service Extensions](#developing-service-extensions)

[Adding Commands to Ahsoka.CommandLine](#adding-commands-to-ahsoka-commandline)

[Running Commands Automatically](#running-commands-automatically)

[Modifying the Application Install](#modifying-the-application-install)


# Registering OpenPV Extension

Before an extension can be referenced by OpenPV, it must be registered with the core by creating a  \*.extensionInfo.json file in the Core's Publish Extensions project.  This file contains the following information that must be known when "registering".

- Extension Name - This is the name users will see in the extension list
- Extension Package Name - This is the name of the nuget package for the extensions main library.
- UX Extension Package Name - This is the name of the nuget package for an optional UX Library for use in configuring this extension.  Note that currently extensions only support AvaloniaUI for developing UI's 
- Minimum Compatible Version - This is the minimum version that the core requires of the extension.
- Maximum Compatible Version - This is the max version of the extension that will work with the core.
- Service Configurations - If the extension provides services for the Ahsoka Framework, the default service configuration is created here.   When adding an extension the core team will assign the extension a default port (data channel) to ensure the default ports do not conflict between extensions.

 **Currently registering or updating does require rebuilding the core libraries however this process may move to a cloud based system in later 3.x or 4.x builds.**
 
# Developing Service Extensions

Services are the backbone of OpenPV Applications.  Services enable cross platform development by allowing their "clients" to be created in many languages as well as enabling functionality to be accessed from both local as well as remote (via TCP, BT, etc.) platforms.

For detailed information on developing services for OpenPV, go here [Ahsoka Service Framework](Understanding%20Ahsoka%20Service%20Architectures.md)

In addition to the normal work of developing a services, some services and extensions have the need to get information about other services that may be running in the system.   To prevent tight coupling between systems, a series of interfaces are used to allow extensions to supply behaviors, parameters and other key artifiacts to their peer extensions.

For example, the default central caching system (DataService) may need to know what Key / Value pairs are available for each service that is running.    To support this, developer of extensions can add an optional override of the method OnGetParameters on their ServiceMessage class.   From here, you can provide the list of data values in your service to other services that may wish to extend.  In this method, the extension can parse its internal configurations or provide static values which allow other systems to inspect the available values at runtime (or at generation time).

# Adding Commands to Ahsoka Commandline

One of the first extension capabilities is the ability to add new commands to the Ahsoka.CommandLine.   This enables the extension developer to support a number of scenarios including...

- Adding a custom code generator or other data transformations that may be useful during Generation Time or SDK Generation Type.  For example, of a code generator should be run when creating a .OPV file.
- To enhance a developers workflow.  For example, a command line extension might take a native file format and convert it to a known format such as a can DBC file being converted to the CAN Extensions native .json.

To create a command the developer should do the following.

1. Create a static class with a static method.
2. Adorn the method with a "CommandLineMethod" attribute describing the command line syntax as well documenting the arguments accepted by the CommandLine.    

When the command line application launches, if the extension provides commands these will be available to the user.  The example below shows the Control Extensions GeneratCalibrationFromDBC command.

```
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
}
```


# Running Commands Automatically

Often times, you want to automate the commands your extension provides.  The core provides the ability for the extension to add commands to be run in response to the following events.

- SdkGenerators - These commands will be run when the SDK is Generated / Injected into the project
- ModelGenerators - These commands are run when models are generated (typically when the SDK is Injected) to create View Models.
- Build Commands - These commands are run when the application is generated / packaged in an OPV File.

To add commands to the listed events...

1. Create a class that implements the "IExtensionGenerator" interface.
2. Implement the GetCommands() method.   Here you receive the package info (usefull for accessing paths and configurations) and can add commands to the "commandsToExecute" dictionary.   
   
These commands will be executed at when the event occurs in the OpenPV Developer Toolkit.  
***Note that commands do NOT have to call Ahsoka.CommandLine but rather can be used to add your own scripts or other actions to the various events such as copying files, packaging files and executing external applications.**


# Modifying the Application Install

Most applications and extensions are packaged along with the application when being installed on the target, however in some cases your extension may need to provide its own configuration files, binaries or even more complex firmware.   Tos support this two types of Installer extension mechanisms are available.

- InstallerPlugins - In most cases an installer plugin can handle what an application needs.  Installer Plugins can...
  - Check the PackageInformation for required licenses or service configurations. This is usefull for validating required files or licenses at compile time.
  - Add Additional files to the application Package.  This is usefull for injecting static values / files or configurations into the applicaition folder
  - Modify the Application Output Area prior to packaging. This is useful if you need to modify or remove items such as developer only artifacts.
  - Modify the Target as part of the install of the package.  This is useful if the plugin requires additional modifications or platform specific artifacts be installed which can only be determined on the actual target system.
  
- InstallerComponents - For more advanced extensions, an extension can provide an Installer Component.   Unlike a plugin, the installer component generates a complete standalone slot in the OpenPV File Format.  While in most cases the plugin is preferred, the Installer Components is useful when you need to... 
     - Package self contained applications separate from the core application.
     - Install software on to an optional peripheral or external device
     - Need to bundle larger binaries that need their own separate folder / file system structure. 

To build Installer Plugins

- Create a class that inherits the "InstallerPlugin" base class.
- Override the required methods for CheckPrerequisites, PrepareApplicationArea, AddAdditionalApplicationFiles and / or ModifyTargetInstall.  All methods are optional so only override what is needed for your extension.
- If any cleanup is needed, you can override the CleanUpApplication function to remove temp files etc.

To build Installer Components

- Create a class that inherits the "InstallerComponent" base class.
- Generate a GUID for your Component and return the GUID value in the "Component Type" property.  The GUID globally identifies the component and can not be changed.
- Override IsRequired to indicate if the Component is Required for a successful package generation
- Override the Component Name to return a human readable value to show in the Installer UI and Command Line during generation.
- Override the OnCreatePackageStream function to return the package information (typically a binary file format such as zip) to include in the .OPV File.
- Override the OnInstallComponent function to unpack your package on the target and execute any required system setup.  In this function, you will be passed a reference to the data stream you packed on OnCreatePackageStream.

**Note that the streams you generate will be encrypted as part of the .OPV file format so can only be read during the OnInstallComponent after being decoded by the Installer with the appropriate decryption system.


# Code Generation and OpenPV Models

One of the most powerful ways you can help your developers and customers to quickly consume your extensoin is to provide facilities for automatically generating code to allow their application to interact with "View Models" in the application rather of relying on flat service interfaces. The most common method of doing this is by using a custom configuration file (which describes the developers expectations) to generate code in the form of "View Models" for their application to interact with.

OpenPV makes use of view models any time the physical model (such as a message or data structure) doesn't match the real world application model.  Take a simple example of an Discrete IO value available in an OpenView Display.   Rather than interact with "Analog 1" an extension developer may choose to capture that Analog 1 is used for a Light Sensor.  By capturing this as a simple configuration (typically with an extension UI) the developer could then generate code that allows the developer to interact with an object called Vehicle with a property called "LightInLumens".  If at a later time, the application must be reworked to move the light sensor from Analog 1 to Analog 2 (or even a different network such as CAN). The underlying Application would not need to change...only the relevant configuration would change.

OpenPV uses this combination of Configuration, Models and Code Generation to allow abstraction between services such as CAN, IO and even UI Systems such as Crank Storyboard.   In addition, all of the technology necessary to implement your own models is available to extensions.

# Creating Code Generators

Most of the tools you need to generate code you have learned about already.    Code Generators are simply OpenPV Commands which you run as part of Generation.  To create a Code Generator, you simply create a command that takes the relevant information needed to generate the code and write the relevant files (in the passed in language) to the developers project folder.  Currently the core system does not provide any DOM or extended support for generating code however, in C#, it is relatively easy to generate complex strings using StringBuilder and many examples are availble in other extensions to get you started such as the CANModelGenerator, StoryboardModelGenerator and others.

** Note that while most generators are implmented in C#, Extension developers can of course use other languages and simply use the IExtensionGenerator to execute their external command line processes as part of ModelGeneration.

# Behaviors and Other Collaboration Features

While generating code and generating your own models is a common paradigm for extending OpenPV Services into easier to use API's, many times extensions must interact with data that is produced or consumed by other part of a system often implemented by other Extensions.   To assist in enabling extensions to collaborate with each other while minimizing the coupling between these extensions, OpenPV provides Behavior Interfaces which you can implement classes (as simple decorators) which enable you to hook into the various parts of a running OpenPV system.

- IClientEndPointBehavior - Implemnting the IClientEndPointBehavior allows you to extend the Clients of other systems.  To accomplish this, implement the IClientBehavior interface and add your class to a Clients behavior list.  These IClientEndPointBehavior objects are notified any time a message is sent (OnSendMessage) or recieved (OnNotificationReceived) by the client allowing you extend the functionality or provide alternative behaviors based on the client.  
- IViewModelBehavior - Probably the most powerful behavior you can create is a IViewModelBehavior.  The ViewModelBehavior object can be added to any View Model (inherited from the build in ViewModelBase) to listen to changes in the ViewModel directly.   From here you can extend functionality which occures when changes are made to models at runtime as well as forward values onto your own services or system components.  This behavior is often used for global caching systems (such as DataService) or when triggers need to be added for hooking into external application such as a Mobile Application connected over Bluetooth.    These behaviors can be added by developers as optional feature on their models OR by extending the code generators (using IViewModelGenerator) to add the behaviors when the code is generated.

# Injecting Behaviors and Other Functionality

While many of the behaviors and capabilities you have learned can be added by the developers as part of their development process, you have one more mechanism you can use to add functionality to an existing system.  The IViewModelGenerator interface allows you to add your own code when OTHER systems are generating their code and data models.  To accomplish this, you can...

- Create a class in your extension that implements the IViewModelGenerator interface.
- Override the SupportedTypes property to return the language(s) your extension is designed to handle.
- Override the ExtendHeader Method to add code to the ModelObject header.  This is typically used if you need to add "Using" or "Import" statements to an object.
- Override the ExtendConstructor Method to add code to the Constructor of the object.  This can be used to initialize your own memory or to notify other components of the instanciation.  One powerfull example of this method is to add your own behavior objects (IViewModelBehavior) to the view model when created.
- Override the ExtendMethods Method to add your own methods to the object.  This can be useful for adding your own methods specific to the instance.
- Override the ExtendSetter Method to add your logic that should occur after a property (or set call) has been completed.  This can be useful for sending your own notifications, forwarding values or decomposing values into other values.
- Override the ExtendAfterClassOutput Method to add code after all models have been generated.  This is usefully for creating your own objects in the generated code such as Utility and Singleton types which your extension may need.