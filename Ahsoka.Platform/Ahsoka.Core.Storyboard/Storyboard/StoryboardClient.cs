using Ahsoka.Dispatch;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Data;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Storyboard;

/// <summary>
/// Client used to connect to Storyboard over SBIO
/// Defines an Input and Output Channel pair.
/// </summary>
public class StoryboardClient : IDispatchSource, ICanAutoStart, IDisposable
{
    #region Fields
    Process sbProcess;
    readonly StoryboardOutputChannel outputChannel;
    readonly StoryboardInputChannel inputChannel;
    readonly CancellationTokenSource cancelToken = new();
    readonly AutoResetEvent shutDownEvent = new(false);
    readonly StartupConfiguration configuration;
    readonly IStoryboardAPI sbioAPI;
    readonly DataServiceClient dataClient;
    bool uiStarted = false;
    bool channelsOpen = false;
    private BlockingCollection<StoryboardMessage> MessageQueue = new();
    private Task StoryboardInputChannelTask;
    private Task MessageQueueConsumerTask;
    #endregion

    #region Service Notification Handler 
    /// <summary>
    /// Delegate for receiving messages which originate from Storyboard
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void StoryboardMessageReceived(object sender, StoryboardEventArgs e);

    // Declare the event.
    /// <summary>
    /// Event which is raised when a Storyboard (SBIO) Event is recieved.
    /// use the Message Object to construct a strongly typed object for use 
    /// in your model / business logic.
    /// </summary>
    public event StoryboardMessageReceived MessageReceived;

    /// <summary>
    /// Handles Dispatch Messages and Translates to Storyboard messages
    /// </summary>
    event EventHandler<EventArgs> IDispatchSource.HandleDispatchMessage
    {
        add
        {
            MessageReceived += value.Invoke;
        }

        remove
        {
            MessageReceived -= value.Invoke;
        }
    }

    #endregion

    #region Methods


    /// <summary>
    /// Constructs a new SB Client
    /// </summary>
    /// <param name="sbConfig">Configures how the SB Application will be started</param>
    /// <param name="api"></param>
    /// <param name="dataClient"></param>
    public StoryboardClient(StartupConfiguration sbConfig = null, IStoryboardAPI api = null, DataServiceClient dataClient = null)
    {
        api ??= GetInterop();

        sbioAPI = api;

        this.dataClient = dataClient;

        sbConfig ??= new StartupConfiguration() { StoryboardInputChannel = "backend", StoryboardOutputChannel = "frontend", UseEarlyStart = false };

        configuration = sbConfig;

        // Same Input and Output Channel Name used for testing purposes.
        outputChannel = new StoryboardOutputChannel(sbConfig.StoryboardOutputChannel, sbioAPI);
        inputChannel = new StoryboardInputChannel(sbConfig.StoryboardInputChannel, sbioAPI);
    }

    /// <summary>
    /// IDisposable Dispose function
    /// </summary>
    public void Dispose()
    {
        StopEventListener();
    }

    internal static IStoryboardAPI GetInterop()
    {
        return SystemInfo.CurrentPlatform switch
        {
            PlatformFamily.Windows64 => new WinStoryboardInterop(),
            PlatformFamily.OpenViewLinux => new STStoryboardInterop(),
            PlatformFamily.MacosArm64 => new MacOStoryboardInterop(),
            _ => throw new PlatformNotSupportedException(),
        };
    }

    /// <summary>
    /// Explicitly starts the Storyboard Engine passing it the Project File specified in the StartupConfiguration.
    /// </summary>
    /// <exception cref="ApplicationException"></exception>
    public void StartStoryboardUI()
    {
        using var stopwatchChannels = new AhsokaStopwatch("SBEngine");

        // Don't Start UI Twice
        if (uiStarted)
            return;

        string sbEnginePath = GetSBEnginePath();
        string pathToSBProject = GetSBProjectPath();

        if (!File.Exists(sbEnginePath) && !Directory.Exists(sbEnginePath))
            throw new ApplicationException("Could not find sbengine at " + sbEnginePath);

        if (!File.Exists(pathToSBProject))
            throw new ApplicationException("Could not find storyboard project at " + pathToSBProject);

        // Prepare Environment
        var greionameVar = new KeyValuePair<string, string>("GREIONAME", this.outputChannel.ChannelName);
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Storyboard GREIONAME={greionameVar.Value}");

        string envSeparater = OperatingSystem.IsWindows() ? ";" : ":";

        string existingPath = Environment.GetEnvironmentVariable("DYLD_LIBRARY_PATH");
        if (existingPath != null) existingPath += envSeparater;
        var dyLibraryPathVar = new KeyValuePair<string, string>("DYLD_LIBRARY_PATH", $"{existingPath}{Path.Combine(GetSBLibraryRootPath(), "lib")}");
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Storyboard DYLD_LIBRARY_PATH={dyLibraryPathVar.Value}");

        string existingLibPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
        if (existingLibPath != null)  existingLibPath += envSeparater;
        var libraryPathVar = new KeyValuePair<string, string>("LD_LIBRARY_PATH", $"{existingLibPath}{Path.Combine(GetSBLibraryRootPath(), "lib")}");
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Storyboard LD_LIBRARY_PATH={libraryPathVar.Value}");


        var pluginsVar = new KeyValuePair<string, string>("SB_PLUGINS", $"{Path.Combine(GetSBLibraryRootPath(), "plugins")}");
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Storyboard SB_PLUGINS={pluginsVar.Value}");

        // Start SBEngine
        if (OperatingSystem.IsWindows() || !configuration.UseEarlyStart)
        {
            Environment.SetEnvironmentVariable(dyLibraryPathVar.Key, dyLibraryPathVar.Value);
            Environment.SetEnvironmentVariable(greionameVar.Key, greionameVar.Value);
            Environment.SetEnvironmentVariable(libraryPathVar.Key, libraryPathVar.Value);
            Environment.SetEnvironmentVariable(pluginsVar.Key, pluginsVar.Value);
                     
           sbProcess = sbioAPI.StartProcess(sbEnginePath, pathToSBProject, configuration.StartupArguements);
        }
        else if (!File.Exists(GetStartUIHelperPath()))
        {
            // Create script to start UI early in Linux
            string[] lines =
            {
                "#!/bin/sh",
                $"export {greionameVar.Key}={greionameVar.Value}",
                $"export {libraryPathVar.Key}={libraryPathVar.Value}",
                $"export {pluginsVar.Key}={pluginsVar.Value}",
                $"chmod +x {sbEnginePath}",
                $"{sbEnginePath} {configuration.StartupArguements} {pathToSBProject} &"
            };

            File.WriteAllLines(GetStartUIHelperPath(), lines, Encoding.ASCII);
            Process.Start("chmod", "+x " + GetStartUIHelperPath()).WaitForExit();
            Process.Start(GetStartUIHelperPath());
        }

        uiStarted = true;

        // Open the Channels
        OpenChannels();
        stopwatchChannels.StopTiming("ChannelsOpen");
    }


    /// <summary>
    /// Send an existing Storyboard Message over the SBIO Interface
    /// </summary>
    /// <param name="message">Message to send to Storyboard</param>
    public void SendEvent(StoryboardMessage message)
    {
        outputChannel.SendEvent(message);
    }

    /// <summary>
    /// Send a Raw Storyboard Message over the SBIO Interface
    /// </summary>
    /// <param name="message"></param>
    /// <param name="target"></param>
    public void SendEvent(object message, string target = null)
    {
        // Attempt to Send event. If Event can't be sent, abort application
        if (outputChannel.SendEvent(message, target) == -1)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Ahsoka Application Shut Down due to Error in Storyboard Event Channel");
            Environment.Exit(0);
        }

    }

    private void DataClientValueChangeNotificationHandler(object sender, DataServiceClient.AhsokaNotificationArgs notificationInfo)
    {
        if (notificationInfo.TransportId == DataMessageTypes.Ids.NotifyKeyValueChanged)
        {
            KeyValueNotification notif = notificationInfo.NotificationObject as KeyValueNotification;
            for (int i = 0; i < notif.KeyValues.Count; i++)
            {
                if (!StoryboardMessageSerializer.TryGetSBMessageObject(notif.KeyValues[i], out object sbMessageObject))
                    continue;

                MessageQueue.Add(new StoryboardMessage(sbMessageObject));
            }
        }
    }

    /// <summary>
    ///  Starts the Listener Thread to listen for Incoming SBIO Messages / Events.
    /// </summary>
    public void StartEventListenerAsync()
    {
        OpenChannels();

        StoryboardInputChannelTask = Task.Run(() =>
        {
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var resultObject = inputChannel.ReceiveMessage(10000);
                    // Queue the Storyboard Event to the message Queue to be processed.
                    if (resultObject != null)
                    {
                        MessageQueue.Add(resultObject);
                    }
                }
                catch (OperationCanceledException) { }
            }

            shutDownEvent.Set();

        }, cancelToken.Token);

        if (dataClient != null)
            dataClient.NotificationReceived += DataClientValueChangeNotificationHandler;

        MessageQueueConsumerTask = Task.Run(() =>
        {
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    StoryboardMessage message = MessageQueue.Take(cancelToken.Token);
                    MessageReceived?.Invoke(this, new StoryboardEventArgs()
                    {
                        Message = message,
                    });
                }
                catch(OperationCanceledException) { }
            }

            shutDownEvent.Set();

        }, cancelToken.Token);
    }

    /// <summary>
    /// Stops the Storyboard Listener Thread
    /// </summary>
    public void StopEventListener()
    {
        cancelToken.Cancel();
        if (OperatingSystem.IsWindows())
        {
            sbProcess?.Kill();
        }
        else
        {
            Process.Start("pkill", "-f sbengine").WaitForExit();
        }
        uiStarted = false;
        inputChannel.CloseChannel();

        if (dataClient != null)
            dataClient.NotificationReceived -= DataClientValueChangeNotificationHandler;

        Task.WaitAll(MessageQueueConsumerTask, StoryboardInputChannelTask);

        shutDownEvent.WaitOne(); // Wait For Shutdown
    }

    private string GetSBLibraryRootPath()
    {
        return Path.Combine(sbioAPI.EngineRootPath(), "sbengine");
    }

    private string GetSBEnginePath()
    {
        return sbioAPI.GetSBEnginePath();
    }
  
    private string GetSBProjectPath()
    {
        return Path.Combine(AppContext.BaseDirectory, configuration.StoryboardProjectPath);
    }

    private string GetStartUIHelperPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "start_ui_helper.sh");
    }

    private void OpenChannels()
    {
        if (!channelsOpen)
        {
            outputChannel.OpenChannel();
            inputChannel.OpenChannel();

            channelsOpen = true;
        }

    }

    void ICanAutoStart.AutoStart(object requester)
    {
        StartEventListenerAsync();
    }

    void ICanAutoStart.AutoStop(object requester)
    {
        StopEventListener();
    }

    #endregion
}

/// <summary>
/// Event Args used for handing Received Storyboard Data over SBIO
/// </summary>
public class StoryboardEventArgs : EventArgs
{
    /// <summary>
    ///  Message Object used to decode Storyboard Event Data into the applications native Message Objects.
    /// </summary>
    public StoryboardMessage Message { get; set; }
}

/// <summary>
/// Storyboard process startup arguments and info
/// </summary>
public partial class StartupConfiguration
{
    /// <summary>
    /// Flag to create a Start UI script for earlier start
    /// </summary>
    public StartupConfiguration()
    {
        this.UseEarlyStart = false;
    }
}