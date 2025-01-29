using Ahsoka.ServiceFramework;
using Ahsoka.Services.Security;
using Ahsoka.Services.System;
using Ahsoka.System;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Base;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Starting Ahsoka Blank Application");

        // Create Security Service Client and Start the Services
        SecurityServiceClient securityClient = new();

        // Create a Service Client for our Ahsoka Runtimes.  The System Service can access
        // information about the system such as versions and cpu temp as well as other features
        // such as the LCD Brightness and Buzzer States.
        // by default this client will run in our current process and use TCPIP
        // Note that we start the service here.
        SystemServiceClient systemClient = new();

        // If using Security, Request a Token (The Example Key is included in this project which you can import into your project).
        // Once you recieve the token, you must call waitfortoken in case it was configured for "Challenge"
        // in this example, we simply rely on the API key to validate the clients
        var request = securityClient.RequestToken(new TokenRequestInfo()
        {
            // Replace with your own key or import cert from project
            ApiKey = "VC+rr/2YIuUAkUpR3aJiajPEpNt6FL14e8oodWSc/X1Jy3fU6M9m610p4gBM8t38CUkKYtVI7ZF4D9sJWyJrJw==",
            ClientIds = { ((IAhsokaClientEndPoint)systemClient).GetClientId() },
            ClientDescription = "TestClient"
        });

        // Waits for Accepted Token to be Recieved
        var systemServiceToken = securityClient.WaitForToken(request.RequestId);
        if (!systemServiceToken.TokenIsValid)
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "The token was not valid");
        else
            systemClient.SetToken(systemServiceToken);


        CancellationTokenSource source = new();
        var backgroundWorkThread = Task.Run(() =>
        {
            while (!source.IsCancellationRequested)
            {
                // Print the Status of our Services every second.  Service Clients
                // are safe to access in background threads HOWEVER many times the results
                // may need to be protected using locks or mutexs.
                //
                // NOTE here we only consume the single call (and no other threads access Client) 
                // Print the Current CPU Temperature
                var returnValue = systemClient.GetHardwareInformation();
                Console.WriteLine($"\r\n   CPU Temperature:{returnValue.CpuTempInC}");

                AhsokaLogging.LogMessage(AhsokaVerbosity.High, "\r\nPress <enter> to Stop Application");

                // Wait 5s (timer) this will cancel if the token source is sent
                source.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
            }
        }, source.Token);


        // Before we start our work, we need to let the system we are ready to allow other system components to start
        // This Notifies the  SystemD Architecture and releses other low priority services.  If you do not call this in your 
        // startup, the system will automatically release after around 60s.   Until that time you can't access services such as
        // ssh and scp.
        // Note that this is handled for you is using Dispatcher
        ApplicationContext.ReleaseStartup();


        // Block our main thread. There are numerous ways to do this but we are simply waiting for an Enter Key.
        // We could of course also call ...
        // - ApplicationContext.WaitForExitSignal();   // This is released with ApplicationContext.Exit():
        // - Dispatcher.Default.StartAndRun();
        // - while (true) {   // Blocking Work } 
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


        // This alerts the Cancellation Source to let any threads know we want to stop.
        // we then wait for it to finish before we continue.
        source.Cancel();
        backgroundWorkThread.Wait();

        // Here we will do the final cleanup of our runtime(s).   
        // Note that we could also call AhsokaRuntime.Default.ShutdownAll(); which
        // will request and wait for a any runtimes that were started to shutdown and
        systemClient.Stop();
        securityClient.Stop();

        // Finally lets call the Application Context Exit.  This ensures that any other processes running in our service "Ahsoka.Application"
        // are shutdown as well.  This will typically always be the last line of code in your app but is handled for you when using Dispatcher.
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Application Shutting Down");
        ApplicationContext.Exit();
    }


    /// <summary>
    /// Simple function to wait for Enter to be pressed
    /// </summary>
    /// <returns></returns>
    private static bool WaitForKeyboardEntry()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Press <enter> to Stop Application");
        Console.ReadLine();
        return true;
    }

    private static void TimerElapsed(object sender, EventArgs args)
    {
        SystemServiceClient client = (SystemServiceClient)sender;

        // Print the Status of our Services every second
        Console.WriteLine($"\r\n    Get ServiceInfo ----");

        var returnValue = client.GetRunningServices();
        foreach (var item in returnValue.ServiceStats)
            Console.WriteLine($"    {item.ServiceName} Listening on {item.ServiceAddress}\r\n\t MessagesReceived:{item.ReceiveMessageCount}\r\n\t MessagesSent:{item.SendMessageCount}");


        AhsokaLogging.LogMessage(AhsokaVerbosity.High, "\r\nPress <enter> to Stop Application");

    }
}

