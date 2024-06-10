using Ahsoka.Dispatch;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.System;
using Ahsoka.Storyboard;
using Ahsoka.System;
using Ahsoka.Utility;
using System;

namespace Ahsoka.Application.Storyboard;
internal class ApplicationViewModelRoot
{
    #region fields
    SystemServiceClient systemClient;
    #endregion

    #region properties
    private bool stopCompleted = false;
    public static ApplicationViewModelRoot Default { get; } = new();
    public SystemServiceClient SystemClient { get { return LazyLoad(ref systemClient); } }
    #endregion


    private T LazyLoad<T>(ref T fieldValue) where T : class, new()
    {
        lock (this)
            fieldValue ??= new T();

        return fieldValue;

    }
    #region constructor and startup
    private ApplicationViewModelRoot()
    {

    }

    internal void StartAndRun()
    {
        using var stopwatch = new AhsokaStopwatch("Start And Run System");

        // First Start the Storyboard UI
        StartStoryboardUI();
        stopwatch.ResetTiming("UI Ready");

        // Register Model Dispatch for Storyboard, this will send events to us 
        // when they are recieved from the Storyboard Client on our Main Thread
        Dispatcher.Default.RegisterCallback(HandleStoryboardMessage, ApplicationViewModelBase.StorybardClient);

        // Register Model Dispatch for Models
        // this will send notifications to us when they occur in the System Client (Ignition)
        // on the main thread.
        Dispatcher.Default.RegisterCallback(HandleSystemMessage, SystemClient);

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

    internal bool StartStoryboardUI()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "Starting Storyboard Thermostat Demo");

        // Create a Storyboard Configuration Object Passing it the configuration info
        // such as the Project, Arguements and Channel Names
        var sbConfig = new StartupConfiguration()
        {
            StoryboardProjectPath = "StoryboardExport/Template.gapp",
            StartupArguements = " ",
            StoryboardOutputChannel = "ToFrontend_channel",
            StoryboardInputChannel = "ToBackend_channel"
        };

        // Creates a Client Object to handle Messages Into and Out of Storyboard
        ApplicationViewModelBase.StorybardClient = new StoryboardClient(sbConfig);

        // Start the UI and Channels
        ApplicationViewModelBase.StorybardClient.StartStoryboardUI();

        return true;
    }

    /// <summary>
    /// Used to stop the Dispatcher and exit the StartAndRun method
    /// </summary>
    internal void StopModel()
    {
        if (!stopCompleted)
        {
            // Stop Runtimes, Dispatchers and Request SystemD to Shutdown Ahsoka.Application.
            ApplicationContext.Exit();

            stopCompleted = true;
        }

    }

    public void HandleStoryboardMessage(object model, EventArgs args)
    {
        if (args is StoryboardEventArgs sbArg)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Receiving message {sbArg.Message.Name}");

            // This Call Constructs a Model Object from a Storyboard message in order to 
            // update our model objects.   Typically these are in response to User Inputs such as 
            // Touch or Other Triggers coming from Storyboard
            try
            {
                var viewModel = sbArg.Message.GetObject<ApplicationViewModelBase>();
                if (viewModel != null)
                {
                    viewModel.UpdateModel(sbArg.Message.Target);
                }
                else
                {
                    throw new ApplicationException($"No object for Event {sbArg.Message.Name}");
                }
            }
            catch (ApplicationException ex)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Encountered Application Exception Parsing Message {sbArg.Message.Name}: {ex.Message}");
            }
            catch (Exception ex)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Encountered Exception: {ex.Message}");
            }

        }
    }

    public void HandleSystemMessage(object model, EventArgs args)
    {
        if (args is SystemServiceClient.AhsokaNotificationArgs e)
        {
            if (e.TransportId == SystemMessageTypes.Ids.IgnitionOnNotification)
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Ignition On");
            else if (e.TransportId == SystemMessageTypes.Ids.IgnitionOffNotification)
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Ignition Off");
        }
    }

    private void InitModels()
    {
        //Send Event to Frontend
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Backend Sending: DEVBackendToFrontend");
        DEVBackendToFrontend devBackend = new()
        {
            VarString = "test"
        };
        devBackend.UpdateStoryboard();
    }
    #endregion
}
