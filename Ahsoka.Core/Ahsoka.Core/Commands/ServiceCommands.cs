using Ahsoka.Core.Utility;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;

namespace Ahsoka.Core.Commands;

[ExcludeFromCodeCoverage]
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
            using (FileStream stream = new FileStream(configurationFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                configuration = ProtoBuf.Serializer.Deserialize<RuntimeConfiguration>(stream);
        }
        else
        {
            string config = File.ReadAllText(configurationFile);
            configuration = JsonUtility.Deserialize<RuntimeConfiguration>(config);
        }

        // Run and Block Executable Until Shutdown
        // Used for Running Ahsoka Services in a Separate Process
        var services = AhsokaRuntime.CreateServicesFromConfiguration(configuration);
        AhsokaRuntime.Default.StartEndPoints(services);

        // Get a list of Running Services.  We will wait for the whole list to stop.
        var list = AhsokaRuntime.Default.ServiceEndPoints.Select(x => x.GetWaitHandle()).ToArray();

        // Blocks Until Services Shutdown
        WaitHandle.WaitAll(list);

        Console.WriteLine("Service Shutdown Completed");
    }
}