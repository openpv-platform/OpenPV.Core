using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.IO;

namespace Ahsoka.Commands;

[CommandLinePlugin]
internal static class ServiceCommands
{
    [CommandLineMethod(@"     --StartServices: Start Services for the Passed in Configuration
            [ConfigurationFile] Path to a Configuration.json or .pbuff file")]
    private static void StartServices(string configurationFile)
    {
        Console.WriteLine("Starting Services with Configuration - " + configurationFile);

        RuntimeConfiguration configuration;
        if (configurationFile.EndsWith("pbuff"))
        {
            FileStream stream = new (configurationFile, FileMode.Open);
            configuration = ProtoBuf.Serializer.Deserialize<RuntimeConfiguration>(stream);
        }
        else
        {
            string config = File.ReadAllText(configurationFile);
            configuration = JsonUtility.Deserialize<RuntimeConfiguration>(config);
        }
         

        // Run and Block Executable Until Shutdown
        // Used for Running Ahsoka Services in a Separate Process
        AhsokaRuntime.CreateBuilder()
                    .AddServicesFromConfiguration(configuration)
                    .StartLocalRuntime()
                    .WaitForShutdown(); // Blocks Until Done

        Console.WriteLine("Service Shutdown Completed");
    }
}