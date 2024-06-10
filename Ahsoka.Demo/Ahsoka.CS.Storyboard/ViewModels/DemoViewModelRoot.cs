using Ahsoka.Dispatch;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Network;
using Ahsoka.Services.System;
using Ahsoka.Storyboard;
using Ahsoka.System;
using Ahsoka.Utility;
using Ahsoka.Utility.WiFi;
using System;

namespace Ahsoka.Demo.Storyboard;

internal class DemoViewModelRoot
{
    #region Fields
    bool stopCompleted = false;
    SystemServiceClient systemClient;
    NetworkServiceClient networkClient;
    #endregion

    #region Singleton
    // Singleton Instance
    public static DemoViewModelRoot Default { get; } = new();

    /// <summary>
    ///  Private Constructor to Enforce Singleton Pattern
    /// </summary>
    private DemoViewModelRoot() { }
    #endregion

    #region Properties

    public SystemServiceClient SystemClient { get { return this.LazyLoad(ref systemClient); } }
    public NetworkServiceClient NetworkClient { get { return this.LazyLoad(ref networkClient); } }

    public BrightnessUpdate BrightnessModel { get; } = new();
    public WiFiNetworkManager WiFiNetworkManager { get; } = new();
    #endregion

    #region Methods
    internal void StartAndRun()
    {
        using var stopwatch = new AhsokaStopwatch("Start And Run System");

        // First Start the Storyboard UI
        StartStoryboardUI();
        stopwatch.ResetTiming("UI Ready");

        // Register Model Dispatch for Storyboard, this will send events to us 
        // when they are recieved from the Storyboard Client on our Main Thread
        Dispatcher.Default.RegisterCallback<StoryboardEventArgs>(HandleInputMessage, DemoViewModelBase.StorybardClient);

        // Register Model Dispatch for Models
        // this will send notifications to us when they occur in the System Client (Ignition)
        // on the main thread.
        Dispatcher.Default.RegisterCallback<SystemServiceClient.AhsokaNotificationArgs>(HandleDataMessage, SystemClient);

        // This call will Initialize our models as the first Dispatched Message
        // after the DispatchQueue is started.   This demonstrates how to run code after
        // the clients and services are started but BEFORE other messages are added
        // to the dispatch queue.
        Dispatcher.Default.InvokeDispatcher(InitModels);

        stopwatch.ResetTiming("Dispatch and Model Setup Complete");

        // We will block here until Dispatch is Shut Down
        // Start Dispatcher (Allow Auto Start of Dispatch Sources like Storyboard Runtime)
        // Using Auto Start will Open the Channels to Storyboard 
        Dispatcher.Default.StartAndRun();
    }

    private void InitModels()
    {
        // Initialize the Brightness to match the System Startup Brightness
        // In this example, we use a local instance of the ViewModel to manages the state of brightness
        BrightnessModel.Init();

        // Setup WiFi Manager, it will register itself with the Dispatch Engine. 
        // it will maintain the WiFi Models and Notify Storyboard of Changes 
        // Note that the WiFi manager can run with or without a Dispatcher so
        // we pass the default dispatcher to it to indicate where messages should be sent.
        WiFiNetworkManager.Init(NetworkClient, Dispatcher.Default);
        WiFiNetworkManager.ConnectionStateChanged += WifiConnectionStateChanged;
        WiFiNetworkManager.SelectedNetworkChanged += (o, e) => { new ViewNetwork().UpdateStoryboard(); };
        WiFiNetworkManager.NetworkListChanged += (o, e) => { new ViewNetwork().UpdateStoryboard(); };

        // Setup Simulation, it will register itself with the Dispatch Engine
        // demonstrating time based work. In this example, we use a Singleton
        Simulation.Default.Init();
    }

    internal bool StartStoryboardUI()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "Starting Storyboard Thermostat Demo");

        // Create a Storyboard Configuration Object Passing it the configuration info
        // such as the Project, Arguements and Channel Names
        var sbConfig = new StartupConfiguration()
        {
            StoryboardProjectPath = "StoryboardExport/ThermostatIO.gapp",
            StartupArguements = " ",
            StoryboardInputChannel = "thermostat_backend",
            StoryboardOutputChannel = "thermostat_frontend"
        };

        // Creates a Client Object to handle Messages Into and Out of Storyboard
        DemoViewModelBase.StorybardClient = new(sbConfig, StoryboardMetadata.EventMetadata);

        // Start the UI and Channels
        DemoViewModelBase.StorybardClient.StartStoryboardUI();

        return true;
    }

    internal void StopModel()
    {
        if (!stopCompleted)
        {
            // Stop Runtimes, Dispatchers and Request SystemD to Shutdown Ahsoka.Application.
            ApplicationContext.Exit();
            stopCompleted = true;
        }
    }
    #endregion

    #region Dispatch Event Handlers
    public void HandleInputMessage(object model, StoryboardEventArgs sbArg)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Receiving message {sbArg.Message.Name}");

        // This Call Constructs a Model Object from a Storyboard message in order to 
        // update our model objects.   Typically these are in response to User Inputs such as 
        // Touch or Other Triggers coming from Storyboard
        var viewModel = sbArg.Message.GetObject<DemoViewModelBase>();
        if (viewModel != null)
        {
            viewModel.UpdateModel();
        }
        else
        {
            throw new ApplicationException($"No object for Event {sbArg.Message.Name}");
        }
    }


    public void HandleDataMessage(object model, SystemServiceClient.AhsokaNotificationArgs args)
    {
        if (args.TransportId == SystemMessageTypes.Ids.IgnitionOnNotification)
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Ignition On");
        else if (args.TransportId == SystemMessageTypes.Ids.IgnitionOffNotification)
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Ignition Off");
    }

    void WifiConnectionStateChanged(object sender, WirelessConnectionState state)
    {
        ConnectionStatus status;

        if (WiFiNetworkManager.ConnectedNetwork != null)
        {
            status = new()
            {
                Ssid = WiFiNetworkManager.ConnectedNetwork.Ssid,
                SignalLevel = (uint)WiFiNetworkManager.ConnectedNetwork.SignalLevel
            };
        }
        else
        {
            status = new()
            {
                Ssid = "No Connection",
                SignalLevel = 0
            };
        }

        status.UpdateStoryboard();
    }

    #endregion
}
