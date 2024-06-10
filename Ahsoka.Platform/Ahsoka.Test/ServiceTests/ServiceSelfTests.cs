using Ahsoka.Services.System;
using System;
using System.Diagnostics;
using System.Threading;

namespace Ahsoka.Services;

public class ServiceSelfTests
{
    static readonly AutoResetEvent resetEvent = new(false);

    public static void PrintServerMetrics(SystemServiceClient client)
    {
        // Test Service Connection
        string echoString = client.EchoValue("Hello World");
        Console.WriteLine($"EchoValue Responsed with \"{echoString}\"");

        Console.WriteLine($"\r\nGet ServiceInfo ----");

        var returnValue = client.GetRunningServices();
        foreach (var item in returnValue.ServiceStats)
            Console.WriteLine($"{item.ServiceName} Listening on {item.ServiceAddress}\r\n\t MessagesReceived:{item.ReceiveMessageCount}\r\n\t MessagesSent:{item.SendMessageCount}");
    }

    public static void RunEchoPerfTest(int count, SystemServiceClient client)
    {
        // Run Perf Test.
        Stopwatch watch = new();
        watch.Start();

        for (int i = 0; i < count; i++)
        {
            // Test Services Endpoint
            _ = client.EchoValue("Test Value");
        }
        watch.Stop();

        Console.WriteLine("");
        Console.WriteLine($"Performance Test: {count} Request/Response Calls completed in {watch.ElapsedMilliseconds}ms");
        Console.WriteLine($"                : {((float)count / watch.ElapsedMilliseconds) * 1000} Calls Per Second");
        Console.WriteLine("");
    }
}
