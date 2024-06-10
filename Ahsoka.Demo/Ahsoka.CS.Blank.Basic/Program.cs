using Ahsoka.Dispatch;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.System;
using Ahsoka.System;
using System;

namespace Ahsoka.Base;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Starting Ahsoka Blank Application");

        // Create a Service Client for our Ahsoka Runtimes
        // by default this client will run in our current process and use TCPIP
        // for communcation.  Most developers can use this basic setup.
        SystemServiceClient client = new();

        // Our simple example uses the default Dispatcher to run some code every 1 second in a callback 
        // called Timer Elapsed.   This callback will occur on the main thread or thread used to 
        // start the runtime.
        Dispatcher.Default.RegisterTimerCallback(TimerElapsed, TimeSpan.FromSeconds(5), client);

        // Since sometimes you will want to do long running or async work, you can use the following
        // example to run work that will not block the main thread. In our example, we will simply be 
        // waiting for a keyboard value.   After the Async callback is completed, its result is passed
        // to either the successCallback or failCallback dependeding on the result of Action.
        Dispatcher.Default.InvokeDispatcherAsync(WaitForKeyboardEntry, Dispatcher.Default.Stop);

        // Start the main Dispatcher and Block until it completes.
        Dispatcher.Default.StartAndRun();

        // Request that the system shutdown any services, dispatchers or clients that are running
        // as well as ask linux to terminate Ahsoka.Application.
        ApplicationContext.Exit();
    }

    /// <summary>
    /// Simple function to wait for Enter to be pressed
    /// </summary>
    /// <returns></returns>
    private static bool WaitForKeyboardEntry()
    {
        if (OperatingSystem.IsWindows())
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Press <enter> to Stop Application");
            Console.ReadLine();
        }
        else
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Waiting for Signal");
            ApplicationContext.WaitForExitSignal();
        }

        return true;
    }

    private static void TimerElapsed(object sender, EventArgs args)
    {
        SystemServiceClient client = (SystemServiceClient)sender;

        // Print the Current CPU Temperature
        var returnValue = client.GetHardwareInformation();
        Console.WriteLine($"\r\n   CPU Temperature:{returnValue.CpuTempInC}");

        if (OperatingSystem.IsWindows())
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "\r\nPress <enter> to Stop Application");

    }
}
