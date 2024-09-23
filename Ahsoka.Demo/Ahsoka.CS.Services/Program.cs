using Ahsoka.Dispatch;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.System;
using Ahsoka.System;
using System;

namespace Ahsoka.Services.Demo;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Starting Ahsoka Services (Time) Demo Application");

        // Create a Service Client for accessign system features.
        // this is useful as it allows the OpenVPV Customer Toolkit to talk 
        // to our application.  If this behavior is not wanted, the clients configuration
        // can be altered to only listen to internal url's OR not started at all.
        SystemServiceClient systemServiceClient = new();
        Dispatcher.Default.AddStartupItem(systemServiceClient);

        // Here we create a Time Service Client which we built in this demo.  By default this service will 
        // use its internal configuration and start when first called OR when started
        // by the dispatcher.
        TimeServiceClient timeServiceClient = new();

        // Here we register to recieve callbacks from the Time Client.
        // Note that in this example, we define a method on our Program Class to handle the callback.
        Dispatcher.Default.RegisterCallback<TimeServiceClient.AhsokaNotificationArgs>(HandleSetTimeResponse, timeServiceClient);

        //  The method below allows us to Invoke our method in a background thread (our main work) and
        //  call the "successCallback" if the method returns true or a failCallback if false.  There are other
        //  variations of this call but all are used to call work in a thread and notify us when complete.
        //  Here we use a lambda to capture our timeServiceClient
        Dispatcher.Default.InvokeDispatcherAsync(() =>
        {
            // Call our Work Method
            return DoWork(timeServiceClient);
        },
        Dispatcher.Default.Stop); // Stop the Dispatcher when Work is Complete.

        // Start the main Dispatcher and Block until it completes.
        // This method blocks (executes our RunLoop) until the service shutdown is requested
        // which currently happens in the WaitForKeyboardEntry() method.
        Dispatcher.Default.StartAndRun();

        // Once the Dispatcher is shutdown in the Dispatcher.Default.Stop() method,
        // we ask Linux / Windows to exit our process.
        ApplicationContext.Exit();
    }

    /// <summary>
    ///  Main Work Method for the Application.
    /// </summary>
    /// <param name="timeServiceClient"></param>
    /// <returns></returns>
    private static bool DoWork(TimeServiceClient timeServiceClient)
    {
        // The  'work' of our demo will be to execute the following...
        //
        //  1. Read the Time and Print it to the Coneole using AhsokaLogging.
        // 
        //  2. Request The Time Be Set forward one minute (Result will Print in the HandleSetTimeResponse Callback above)
        //
        //       Note that our service doesn't actually set clock just in case you have a meeting coming up :)
        //
        //  3. Finally we will wait for Keyboard Entry (or Service Shutdown on Linux via SIGTERM)
        //

        // Call the Time Service and Get the Time.
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Getting Current Time");
        var timeInfo = timeServiceClient.GetTime();
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"The Current time is: {timeInfo.CurrentTime} with TimeZone: {timeInfo.TimeZone}");

        // Set Time Forward 1 Minute. Result will come in the notification callback.
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Set the Time forward 1 minute");
        timeInfo.CurrentTime = timeInfo.CurrentTime.Value.AddMinutes(1);
        timeServiceClient.SetTime(timeInfo);

        // Wait for Input or Service Shutdown
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

    private static void HandleSetTimeResponse(object source, TimeServiceClient.AhsokaNotificationArgs result)
    {
        // Handle Async Notification from Service
        // Note that this will likely be printed AFTER the Waiting Message 
        if (result.NotificationObject is SetTimeResult setTimeResult)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"SetTime Result Received with Result :{setTimeResult.Success}");
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Time Set to: {setTimeResult.CurrentTime} with TimeZone: {setTimeResult.TimeZone}");
        }
    }

}
