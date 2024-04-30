using Ahsoka.Dispatch;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Bluetooth.LE;
using Ahsoka.Services.Cygnus;
using Ahsoka.Services.Data;
using Ahsoka.Services.IO;
using Ahsoka.Services.System;
using Ahsoka.Storyboard;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using Cygnus.Reach;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Ahsoka.Application.Storyboard;

internal class ApplicationViewModelRoot
{
    #region fields
    SystemServiceClient systemClient;
    IOServiceClient ioClient;

    //View Models
    //Brightness
    private BEEBrightnessStatus brightnessModel;
    //Buzzer
    private BEEBuzzerStatus buzzerModel;
    //System Info
    private BEETouchHardwareVersion touchHardwareVersionModel;
    private BEETouchFirmwareVersion touchFirmwareVersionModel;
    private BEEBSPVersion bspVersionModel;
    private BEEApplicationVersion applicationVersionModel;
    private BEESDKVersion sdkVersionModel;
    private BEEInstallerVersion installerVersionModel;
    private BEEPartNumber partNumberModel;
    private BEESerialNumber serialNumberModel;
    private BEEDeviceId deviceIdModel;
    private BEEApplicationName applicationNameModel;
    //Digital Outputs
    private BEEDigitalOutput1Status digitalOutput1;
    private BEEDigitalOutput2Status digitalOutput2;
    //Analog Inputs
    private BEEAnalogInput1Status analogInput1;
    private BEEAnalogInput2Status analogInput2;
    //Digital Inputs
    private BEEDigitalInput1Status digitalInput1;
    private BEEDigitalInput2Status digitalInput2;
    #endregion

    #region properties
    private bool stopCompleted = false;
    public static ApplicationViewModelRoot Default { get; } = new();

    public SystemServiceClient SystemClient { get { return LazyLoad(ref systemClient); } }
    public IOServiceClient IOClient { get { return LazyLoad(ref ioClient); } }

    //View Models
    //Brightness
    public BEEBrightnessStatus BrightnessModel { get { return LazyLoad(ref brightnessModel); } }
    //Buzzer
    public BEEBuzzerStatus BuzzerModel { get { return LazyLoad(ref buzzerModel); } }
    //System Info
    public BEETouchHardwareVersion TouchHardwareVersionModel { get { return LazyLoad(ref touchHardwareVersionModel); } }
    public BEETouchFirmwareVersion TouchFirmwareVersionModel { get { return LazyLoad(ref touchFirmwareVersionModel); } }
    public BEEBSPVersion BSPVersionModel { get { return LazyLoad(ref bspVersionModel); } }
    public BEEApplicationVersion ApplicationVersionModel { get { return LazyLoad(ref applicationVersionModel); } }
    public BEESDKVersion SDKVersionModel { get { return LazyLoad(ref sdkVersionModel); } }
    public BEEInstallerVersion InstallerVersionModel { get { return LazyLoad(ref installerVersionModel); } }
    public BEEPartNumber PartNumberModel { get { return LazyLoad(ref partNumberModel); } }
    public BEESerialNumber SerialNumberModel { get { return LazyLoad(ref serialNumberModel); } }
    public BEEDeviceId DeviceIdModel { get { return LazyLoad(ref deviceIdModel); } }
    public BEEApplicationName ApplicationNameModel { get { return LazyLoad(ref applicationNameModel); } }
    //Digital Outputs
    public BEEDigitalOutput1Status DigitalOutput1 { get { return LazyLoad(ref digitalOutput1); } }
    public BEEDigitalOutput2Status DigitalOutput2 { get { return LazyLoad(ref digitalOutput2); } }
    //Analog Inputs
    public BEEAnalogInput1Status AnalogInput1 { get { return LazyLoad(ref analogInput1); } }
    public BEEAnalogInput2Status AnalogInput2 { get { return LazyLoad(ref analogInput2); } }
    //Digital Inputs
    public BEEDigitalInput1Status DigitalInput1 { get { return LazyLoad(ref digitalInput1); } }
    public BEEDigitalInput2Status DigitalInput2 { get { return LazyLoad(ref digitalInput2); } }

    #endregion


    private T LazyLoad<T>(ref T fieldValue) where T : class, new()
    {
        lock (this)
            fieldValue ??= new T();

        return fieldValue;

    }
    #region constructor and startup
    private ApplicationViewModelRoot() { }

    private BLEServiceClient bleClient = new();
    private CygnusServiceClient cygnusClient = new();
    private DataServiceClient dataClient = new();

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
        Dispatcher.Default.RegisterCallback((o, e) => { }, dataClient);
        Dispatcher.Default.RegisterCallback(HandleSystemMessage, SystemClient);
        Dispatcher.Default.RegisterCallback((o, e) => { }, bleClient);
        Dispatcher.Default.RegisterCallback((o, e) => { }, cygnusClient);

        // Register Model Dispatch for Storyboard, this will send events to us
        // when they are recieved from the Storyboard Client on our Main Thread
        Dispatcher.Default.RegisterCallback(HandleStoryboardMessage, ApplicationViewModelBase.StorybardClient);

        Dispatcher.Default.RegisterCallback<BLEServiceClient.AhsokaNotificationArgs>((o, e) =>
        {
            if (e.TransportId == BLEMessageTypes.Ids.AdapterReady)
            {
                cygnusClient.EnableTransport(CygnusInterface.BLECygnus);
            }
            else if (e.TransportId == BLEMessageTypes.Ids.PairingRequested)
            {
                bleClient.ConfirmPairingRequests(true, "Allways allowing pairs.");
            }
        }, bleClient);

        // This call will Initialize our models as the first Dispatched Message
        // after the DispatchQueue is started.   This demonstrates how to run code after
        // the clients and services are started but BEFORE other messages are added
        // to the dispatch queue.
        Dispatcher.Default.InvokeDispatcher(InitModels);

        // This will read the analog and digital inputs every 1000ms
        // It takes ~70ms to read each input, so polling faster than 300ms can flood the background queue
        Dispatcher.Default.RegisterTimerCallback((o, e) =>
            {
                Dispatcher.Default.InvokeDispatcherAsync(AnalogInput1.Update, AnalogInput1.UpdateStoryboard);
                Dispatcher.Default.InvokeDispatcherAsync(AnalogInput2.Update, AnalogInput2.UpdateStoryboard);
                Dispatcher.Default.InvokeDispatcherAsync(DigitalInput1.Update, DigitalInput1.UpdateStoryboard);
                Dispatcher.Default.InvokeDispatcherAsync(DigitalInput2.Update, DigitalInput2.UpdateStoryboard);
            },
            TimeSpan.FromMilliseconds(1000)
        );

        stopwatch.ResetTiming("Dispatch and Model Setup Complete");

        Dispatcher.Default.InvokeDispatcher(() =>  bleClient.EnableAdapter("ReachSBIO"));

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
            StoryboardProjectPath = "StoryboardExport/Demo-s70.gapp",
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

    internal void StopModel()
    {
        if (!stopCompleted)
        {
            //Turn off the buzzer
            BuzzerConfig buzzerRequest = new()
            {
                IsEnabled = false,
                FrequencyInHz = 100,
                VolumePct = 100
            };
            Default.SystemClient.SetBuzzerConfig(buzzerRequest);

            //Turn off digital outputs
            //Digital Outputs
            FEEDigitalOutput1Request.digOut.State = PinState.Low;
            Default.IOClient.SetDigitalOut(FEEDigitalOutput1Request.digOut);
            FEEDigitalOutput2Request.digOut.State = PinState.Low;
            Default.IOClient.SetDigitalOut(FEEDigitalOutput2Request.digOut);

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
        var sbClient = ApplicationViewModelBase.StorybardClient;
        
        //Brightness
        BrightnessModel.AddBehavior(new DataServiceViewModelListener(dataClient, "DisplayBrightness"));
        BrightnessModel.UpdateStoryboard();

        //Buzzer
        BuzzerModel.AddBehavior(new DataServiceViewModelListener(dataClient,"Buzzer"));
        BuzzerModel.UpdateStoryboard();

        // IO's
        DigitalOutput1.AddBehavior(new DataServiceViewModelListener(dataClient,"DigitalOutput1"));
        DigitalOutput1.UpdateStoryboard();
        DigitalOutput2.AddBehavior(new DataServiceViewModelListener(dataClient, "DigitalOutput2"  ));
        DigitalOutput2.UpdateStoryboard();

        // Initialize Storyboard Notification Behaviors (Notifies SB if DataService Has Changes to a Model)
        dataClient.AddBehavior(new DataServiceListener(sbClient, typeof(FEEBrightnessRequest), "DisplayBrightness"));
        dataClient.AddBehavior(new DataServiceListener(sbClient, typeof(FEEDigitalOutput1Request), "DigitalOutput1"));
        dataClient.AddBehavior(new DataServiceListener(sbClient, typeof(FEEDigitalOutput2Request), "DigitalOutput2"));
        dataClient.AddBehavior(new DataServiceListener(sbClient, typeof(FEEBuzzerRequest), "Buzzer"));
       
        //System Info
        Default.TouchHardwareVersionModel?.UpdateStoryboard();
        Default.TouchFirmwareVersionModel?.UpdateStoryboard();
        Default.BSPVersionModel?.UpdateStoryboard();
        Default.ApplicationVersionModel?.UpdateStoryboard();
        Default.SDKVersionModel?.UpdateStoryboard();
        Default.InstallerVersionModel?.UpdateStoryboard();
        Default.PartNumberModel?.UpdateStoryboard();
        Default.SerialNumberModel?.UpdateStoryboard();
        Default.DeviceIdModel?.UpdateStoryboard();
        Default.ApplicationNameModel?.UpdateStoryboard();

        //Digital Outputs
        FEEDigitalOutput1Request DigitalOutput1Request = new();
        DigitalOutput1Request.Init();
        FEEDigitalOutput2Request DigitalOutput2Request = new();
        DigitalOutput2Request.Init();


        cygnusClient.AddFile(new()
        {
            Name = "Test File",
            Path =  "/usr/local/Ahsoka/start.sh",
            Permissions = AccessLevel.Read,
        });
        cygnusClient.AddFile(new()
        {
            Name = "Big 1MB File",
            Path =  "/usr/local/Ahsoka/current/Azure.Storage.Blobs.dll",
            Permissions = AccessLevel.Read,
        });

        cygnusClient.RegsiterCommandCallback(
            new() { Name = "Test Command" },
            dataClient,
            ExampleCommandCallback
        );

        cygnusClient.RegisterCLICallback(
            dataClient,
            ExampleCLICallback
        );
    }

    private int ExampleCommandCallback(out string message)
    {
        message = "This is a test.";
        return 0;
    }

    private string ExampleCLICallback(string input)
    {
        string oStr = "";
        switch (input)
        {
            case "one":
                oStr = "We have matched with the prompt of \"one\"\n";
                break;

            case "two":
                oStr = "This is going to be a very long string that we are returning to test out whether or not the " +
                       "system is properly splitting up these very long responses to multiple messages to keep " +
                       "within the buffer size of BLE and not cause any problems with the existing Cygnus " +
                       "infrastructure.\n";
                break;

            case "ls":
                string basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string[] files = Directory.GetFiles(basePath);
                foreach (var file in files)
                    oStr += $"{file.Replace(basePath, ".")}\n";
                break;

            case "status":
                ProcessUtility.RunProcess("systemctl", "status Ahsoka.Application", null, out oStr, out _);
                break;

            case "top":
                ProcessUtility.RunProcess("systemctl", "status Ahsoka.Application", null, out string pOut, out _);
                var matches = new Regex(@"- (\d{1,5}) ").Matches(pOut);
                List<string> pids = new();
                foreach (Match match in matches)
                    pids.Add(match.Groups[1].Value);
                ProcessUtility.RunProcess("top", $"-b -n 1 -p {String.Join(",", pids)}", null, out oStr, out _);
                break;

            default:
                oStr = "This is working.\n";
                break;
        }

        return oStr;
    }

    #endregion
}
