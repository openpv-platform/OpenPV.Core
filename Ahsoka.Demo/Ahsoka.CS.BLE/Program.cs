using Ahsoka.CS.BLE.Models;
using Ahsoka.Dispatch;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Bluetooth.LE;
using Ahsoka.Services.System;
using Ahsoka.System;
using System;
using System.ComponentModel;

namespace Ahsoka.CS.BLE;

public class Program
{
    static readonly BLEServiceClient bleClient = new();
    static readonly SystemServiceClient systemClient = new();
    static readonly ExampleService serviceExample = new();
    static readonly ExampleService2 serviceExample2 = new();
    static readonly MainUI mainUI = new();

    public static void Main()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Starting Ahsoka Bluetooth LE Demo Application");

        // Start our UI and Setup the Various Buttons and Text Areas
        var window = mainUI.StartUI();
        mainUI.AddButton(UpdateExample1, "Update Example 1");
        mainUI.AddButton(UpdateExample2, "Update Example 2");

        // These Events will listen for our Model Changes and Refresh Our UI.
        // we are also calling the RefreshUI once to start to init the status values
        serviceExample.PropertyChanged += RefreshUI;
        serviceExample2.PropertyChanged += RefreshUI;
        RefreshUI(serviceExample, new PropertyChangedEventArgs(""));

        // This callback handles the notifications from the BLE Client for Enabled / Disabled
        // Once we are enabled we ask the Service (ExampleService) to Intialize (SetupService)
        // Note that we are handling Pairing Async from the Dispatcher since its managing our main thread.
        bleClient.NotificationReceived += (o, e) =>
        {
            if (e.TransportId == BLEMessageTypes.Ids.AdapterEnabled && e.NotificationObject is BLEConnectionNotification notif)
            {
                // Our Adapter is Ready so Start the Setup Logic (in a Thread)
                // when its ready we will Let the UI know (SystemReady)
                if (notif.State == BLEConnectionState.ClientReady)
                    Dispatcher.Default.InvokeDispatcherAsync(SetupServices, SystemReady, SystemReady);
                else
                    Dispatcher.Default.InvokeDispatcher(Shutdown); // Handle failed Startup.
            }
            else if (e.TransportId == BLEMessageTypes.Ids.PairingRequested && e.NotificationObject is BLEPairingRequest pairRequest)
            {
                // A Pairing Request was Received.  With BLE Peripherals this occurs when we have a 
                // Characteristic that requires encrypted data.  Our job here is to Prompt the User with the Pass Key
                // so they can Accept or Reject it.
                if (pairRequest.RequestType is BLEPairingRequestType.ConfirmValue or
                    BLEPairingRequestType.ConfirmNoValue)
                {
                    mainUI.ShowPairingRequest(pairRequest.Passcode, PairAccepted, PairRejected);
                    Console.WriteLine($"APP: Pairing Request Received with PassKey:{pairRequest.Passcode} Type:{pairRequest.RequestType}");
                }
            }
            else if (e.TransportId == BLEMessageTypes.Ids.PairingCancelled)
            {
                // Hide any showing Pairing Requests
                mainUI.HidePairingRequest();
            }
        };

        // Init the Adapter. Since this is the only message queued to our Dispatcher
        // it will run first and start Adapter
        Dispatcher.Default.InvokeDispatcher(InitAdapter);

        // Wireup System Service (Mostly to handle listening to the Desktop Tools
        Dispatcher.Default.RegisterCallback(SystemServiceNofications, systemClient);

        // Start Dispatcher for System with Our Window Invoker
        Dispatcher.Default.StartAndRun(window.Invoke);

        // Now start the Main Drawing Loop 
        // we will block here until the app shuts down.
        window.ShowAndRun();

        // Handle Shutdown and Cleanup
        Shutdown();

        return;
    }

    private static void SystemServiceNofications(object sender, EventArgs args)
    {
        if (args is SystemServiceClient.AhsokaNotificationArgs e)
        {
            if (e.TransportId == SystemMessageTypes.Ids.IgnitionOnNotification)
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Ignition On");
            else if (e.TransportId == SystemMessageTypes.Ids.IgnitionOffNotification)
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Ignition Off");
        }
    }

    private static void PairRejected()
    {
        bleClient.ConfirmPairingRequests(false, "Pairing Rejected");
        mainUI.HidePairingRequest();
    }

    private static void PairAccepted()
    {
        bleClient.ConfirmPairingRequests(true, "Pairing Accepted");
        mainUI.HidePairingRequest();
    }

    private static void RefreshUI(object sender, PropertyChangedEventArgs e)
    {
        mainUI.UpdateStatusText("Example 1 (StringValue):", serviceExample.StringValue);
        mainUI.UpdateStatusText("Example 1 (ByteValue):", BitConverter.ToString(serviceExample.ByteValue));

        mainUI.UpdateStatusText("Example 2 (StringValue): ", serviceExample2.StringValue);
        mainUI.UpdateStatusText("Example 2 (ByteValue):", BitConverter.ToString(serviceExample2.ByteValue));
        mainUI.UpdateStatusText("Example 2 (DataValue): ", serviceExample2.DataValue != null ? BitConverter.ToString(serviceExample2.DataValue) : "<none>");

        mainUI.RedrawScreen();
    }

    /// <summary>
    /// Example Model Update
    /// </summary>
    private static void UpdateExample1()
    {
        serviceExample.StringValue = "Value 1";
    }

    /// <summary>
    /// Example Model Update
    /// </summary>
    private static void UpdateExample2()
    {
        serviceExample2.StringValue = "Value 1";
    }

    private static void SystemReady()
    {
        mainUI.InitStateChanged(true, "APP: Bluetooth Services Ready at OpenViewDisplay");
    }

    private static void InitAdapter()
    {
        mainUI.InitStateChanged(false, "APP: Enabling Bluetooth Adapter.");
        bleClient.EnableAdapter("OpenViewDisplay");
    }

    private static void Shutdown()
    {
        // UnRegister the Models with teh Service Manager
        Console.WriteLine("APP: Removing BLE Services.");
        bleClient.StopServiceModels(serviceExample, serviceExample2);

        Console.WriteLine("APP: Running Shutdown.");
        AhsokaRuntime.ShutdownAll();

        // Close App
        Console.WriteLine("APP: Application Context Exit");
        ApplicationContext.Exit();

    }

    private static bool SetupServices()
    {
        // Register any BLE Services and Begin Advertizing
        bleClient.StartServiceModels(serviceExample, serviceExample2);
        return true;
    }

}
