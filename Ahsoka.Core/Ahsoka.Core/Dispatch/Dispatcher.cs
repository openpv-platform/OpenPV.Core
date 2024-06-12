using Ahsoka.ServiceFramework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Dispatch;

/// <summary>
/// Dispatch Engine for managing Dispatch Queues Ahsoka Applications
/// </summary>
public class Dispatcher
{
    #region Default Singleton
    static Dispatcher defaultDispatcher;

    /// <summary>
    /// Default Instance of Dispatcher
    /// </summary>
    public static Dispatcher Default
    {
        get
        {
            if (defaultDispatcher == null)
                return defaultDispatcher = new Dispatcher();
            else
                return defaultDispatcher;
        }
    }

    /// <summary>
    /// Allows passing a custom dispatcher as the "Default"
    /// </summary>
    /// <param name="dispatcher"></param>
    public void InitDefaultDispatcher(Dispatcher dispatcher)
    {
        if (defaultDispatcher != null)
            throw new InvalidOperationException("Custom Dispatcher must be set prior to accessing the default Dispatcher");

        defaultDispatcher = dispatcher;
    }
    #endregion

    #region Fields 
    bool isStarted = false;
    CancellationTokenSource tokenSource;
    readonly Dictionary<double, TimerTracker> timers = new();
    readonly List<ICanAutoStart> startupObjects = new();
    readonly List<IAhsokaClientEndPoint> endpoints = new();

    // Foreground Worker and Alternative Invoker if needed
    Action<EventHandler> alternativeInvoker;
    readonly BlockingCollection<EventItem> eventQueue = new();

    // Background Scheduler
    Task backgroundWorkLoop;
    readonly BlockingCollection<BackgroundWorkItem> backgroundQueue = new();
    #endregion

    #region Constructor and Start / Stop Methods
    /// <summary>
    /// Create a Dispatcher Engine for managing Services and Data Providers in a single thread
    /// </summary>
    /// <param name="runWarmup"></param>
    public Dispatcher(bool runWarmup = true)
    {
        // Async Pre-Load of Critical Libraries 
        // This call is not required but allows libraries to load while 
        // waiting for the UI to start.
        if (runWarmup) AhsokaRuntime.WarmupRuntime();
    }

    /// <summary>
    /// Starts and Runs the a Single RunLoop
    /// if an alternative invoker is specified then the Dispatcher will run its own thread and all
    /// all callbacks will occur on that the alternative invoker.  Otherise the RunAndStart blocks 
    /// executing all callbacks on the calling thread.
    /// 
    /// Usage: Use as a the Main Loop for a Non-Visual application or Message Based UI (Storyboard for example)
    ///        Specify an alternative Invoker if running in another Application Architecture (GTK for example)
    /// 
    /// </summary>
    /// <param name="alternativeInvoker">Alternative Thread to Invoke Callbacks. </param>
    /// <param name="autoStartDispatcherSources">Starts Any Dispatcher Sources Before Entering RunLoop</param>
    /// <param name="autoStopDispatcherSources">Stops Any Dispatcher Sources Before Entering RunLoop</param>
    public void StartAndRun(Action<EventHandler> alternativeInvoker = null,
        bool autoStartDispatcherSources = true,
        bool autoStopDispatcherSources = true)
    {
        isStarted = true;
        tokenSource = new CancellationTokenSource();

        // Start AutoStart Objects
        if (autoStartDispatcherSources)
            AutoStartSources();

        // start any items that are 
        if (backgroundQueue.Count > 0)
            StartBackgroundIfNotRunning();

        // Call Ahsoka Release Startup 
        AhsokaRuntime.ReleaseStartup();
        
        if (alternativeInvoker == null)
            RunInvokerCore(autoStopDispatcherSources);
        else
        {
            this.alternativeInvoker = alternativeInvoker;
            Task.Run(() => { RunInvokerCore(autoStopDispatcherSources); });
        }
    }

    /// <summary>
    /// Stop the Dispatcher
    /// </summary>
    public void Stop()
    {
        // Cancel All Tokens
        lock (timers)
            foreach (var item in timers)
                item.Value.Timer.Dispose(tokenSource.Token.WaitHandle);

        tokenSource?.Cancel();

        // If we have a background work queue...wait for it to end.
        backgroundWorkLoop?.Wait();
    }
    #endregion

    #region Register Dispatcher Methods
    /// <summary>
    /// Register a callback on a Time Interval
    /// </summary>
    /// <param name="timeElapsedCallback"></param>
    /// <param name="interval"></param>
    /// <param name="source"></param>
    public void RegisterTimerCallback(Action<object, EventArgs> timeElapsedCallback, TimeSpan interval, object source = null)
    {
        RegisterTimerCallback<EventArgs>(timeElapsedCallback, interval, source);
    }

    /// <summary>
    /// Register a callback on a Time Interval
    /// </summary>
    /// <param name="timeElapsedCallback"></param>
    /// <param name="interval"></param>
    /// <param name="source"></param>
    public void RegisterTimerCallback<T>(Action<object, T> timeElapsedCallback, TimeSpan interval, object source = null) where T : class
    {
         AddAutoStartInternal(source as IDispatchSource);

        // If Timer Exists, Add Dispatcher, otherwise create a new timer / tracker
        if (this.timers.TryGetValue(interval.TotalMilliseconds, out TimerTracker tracker))
        {
            tracker.Callbacks.Add(new TimerTrackerCallback<T>()
            {
                Callback = timeElapsedCallback,
                Sender = source
            });
        }
        else
        {
            // Enqueue Work for Each Dispatcher
            // Creating a new Timer so Create a Callback for it.
            var timerCallback = new TimerCallback((object o) =>
            {
                lock (timers)
                {
                    if (o is List<TimerTrackerCallback> list)
                        foreach (var item in list)
                            eventQueue.Add(new EventItem<T>()
                            {
                                Callback = (o, e) => { item.Execute(this); },
                                Source = item.Sender
                            });
                }
            });

            List<TimerTrackerCallback> callbacks = new() { new TimerTrackerCallback<T>() { Callback = timeElapsedCallback, Sender = source } };

            this.timers[interval.TotalMilliseconds] = new TimerTracker()
            {
                Timer = new Timer(timerCallback, callbacks, (int)interval.TotalMilliseconds, (int)interval.TotalMilliseconds),
                Callbacks = callbacks
            };
        }
    }

    /// <summary>
    /// Register a callback from an Input Source
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="dispatchSource"></param>
    public void RegisterCallback(Action<object, EventArgs> callback, IDispatchSource dispatchSource)
    {
        RegisterCallback<EventArgs>(callback, dispatchSource);
    }

    /// <summary>
    /// Register a callback from an Input Source
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="dispatchSource"></param>
    public void RegisterCallback<T>(Action<object, T> callback, IDispatchSource dispatchSource) where T : class
    {
        AddAutoStartInternal(dispatchSource);

        dispatchSource.HandleDispatchMessage += (o, e) =>
        {
            eventQueue.Add(new EventItem<T>()
            {
                Callback = callback,
                Source = dispatchSource,
                EventArgs = e as T
            });
        };
    }
    #endregion

    #region Standalone Invokers
    /// <summary>
    /// Schedules an Action on the Dispatcher Main Loop
    /// </summary>
    /// <param name="value"></param>
    public void InvokeDispatcher(Action value)
    {
        eventQueue.Add(new EventItem<Task>()
        {
            Callback = (o, e) =>
            {
                value.Invoke();
            }
        });
    }

    /// <summary>
    /// Schedules an Event on the Dispatcher Main Loop
    /// </summary>
    /// <param name="callback"></param>
    public void InvokeDispatcher(Action<object, EventArgs> callback)
    {
        eventQueue.Add(new EventItem<EventArgs>()
        {
            Callback = callback
        });
    }

    /// <summary>
    /// Schedules an Action on the Dispatcher Main Loop with Event Args and Optional Source Object
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="args"></param>
    /// <param name="source"></param>
    public void InvokeDispatcher(Action<object, EventArgs> callback, EventArgs args, IDispatchSource source = null)
    {
        eventQueue.Add(new EventItem<EventArgs>()
        {
            Source = source,
            EventArgs = args,
            Callback = callback
        });
    }

    /// <summary>
    /// Invoke an Function (action) in the Backgroud and 
    /// Executes the Action (trueCallback) when complete if result of 'action' is true
    /// Executes the Action (falseCallback) when complete if result of 'action' is false - Optional
    /// </summary>
    /// <param name="action"></param>
    /// <param name="successCallback"></param>
    /// <param name="failCallback"></param>
    public void InvokeDispatcherAsync(Func<bool> action, Action successCallback, Action failCallback = null)
    {
        StartBackgroundIfNotRunning();

        // Enqueue Background Work
        backgroundQueue.Add(new BoolBackgroundWorkItem() { Action = action, DefaultCallback = successCallback, ErrorCallback = failCallback });
    }

    /// <summary>
    /// Invoke an Function (action) which create result (T)
    /// Executes the Callback(T Result) when complete 
    /// </summary>
    /// <param name="action"></param>
    /// <param name="resultCallback"></param>
    public void InvokeDispatcherAsync<T>(Func<T> action, Action<T> resultCallback)
    {
        StartBackgroundIfNotRunning();

        // Enqueue Background Work
        backgroundQueue.Add(new BackgroundWorkItem<T>() { Action = action, Callback = resultCallback }); ;
    }

    /// <summary>
    /// Invoke an Function (action) which create result (T)
    /// Executes the Callback(T Result) when complete 
    /// </summary>
    /// <param name="action"></param>
    /// <param name="resultCallback"></param>
    /// <param name="context"></param>
    public void InvokeDispatcherAsync<T, Context>(Func<T> action, Action<T, Context> resultCallback, Context context)
    {
        StartBackgroundIfNotRunning();

        // Enqueue Background Work
        backgroundQueue.Add(new BackgroundWorkItem<T, Context>() { Action = action, Callback = resultCallback, Context = context }); ;
    }

    #endregion

    #region  Internal Methods
    private void AutoStartSources()
    {
        // Pull Clients that Need a Runtime
        var autoStartList = endpoints.Where(x => x.ServiceConfig.Behaviors.HasFlag(System.EndPointBehaviors.AutoStart)); // Get Auto Start Services
        var clientStartList = endpoints.Where(x => !x.ServiceConfig.Behaviors.HasFlag(System.EndPointBehaviors.AutoStart)); // Get Non Auto Start Services

        if (autoStartList.Any())
        {
            // Start  Auto Start Runtime(s)
            var builder = AhsokaRuntime.CreateBuilder()
                .AddClients(endpoints.ToArray())
                .StartWithInternalServices();
        }

        if (clientStartList.Any())
        {
            // Start Auto Start Runtime for Local CLients (Non AutoStart)
            var builder = AhsokaRuntime.CreateBuilder()
                .AddClients(endpoints.ToArray())
                .StartLocalRuntime();
        }


        var nonRuntimes = startupObjects.Where(x => x is not IAutoStartServiceClient).ToList();
        foreach (var item in nonRuntimes)
            item.AutoStart(this);

    }
    
    private void StartBackgroundIfNotRunning()
    {
        // Wait for Start
        if (backgroundWorkLoop == null && isStarted)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "Dispatcher: Starting Background Loop");
            backgroundWorkLoop = Task.Run(() =>
            {
                // Set Started
                try
                {
                    var originalName = Thread.CurrentThread.Name;
                    Thread.CurrentThread.Name = $"Dispatcher Background Loop";

                    // Run background Work
                    while (backgroundQueue.TryTake(out BackgroundWorkItem workItem, -1, tokenSource.Token))
                    {
                        workItem.Execute(this);
                    }

                    Thread.CurrentThread.Name = originalName;
                }
                catch (OperationCanceledException) { }
            });
        }
    }

    private void RunInvokerCore(bool autoStopDispatcherSources)
    {
        // Set Started
        try
        {
            var originalName = Thread.CurrentThread.Name;
            Thread.CurrentThread.Name = $"Dispatcher Invoker Core";

            while (eventQueue.TryTake(out EventItem messageData, -1, tokenSource.Token))
            {
                if (alternativeInvoker != null)
                    alternativeInvoker.Invoke((o, e) => { InvokeEventItem(messageData); });
                else
                    InvokeEventItem(messageData);
            }

            Thread.CurrentThread.Name = originalName;
        }
        catch (OperationCanceledException) { }

        // Start AutoStart Objects
        if (autoStopDispatcherSources)
            foreach (var source in startupObjects) { source.AutoStop(this); }
    }

    private void InvokeEventItem(EventItem messageData)
    {
        // Handle Input Reactions
        messageData.Execute(this);
    }

    internal void AddAutoStartInternal(IDispatchSource autoStartClient)
    {
        if (autoStartClient is ICanAutoStart autoSource)
        {
            if (!startupObjects.Contains(autoSource))
                startupObjects.Add(autoSource);
        }
        else if (autoStartClient is IAhsokaClientEndPoint client)
        {
            if (!endpoints.Contains(autoStartClient))
                endpoints.Add(client);
        }
    }

    #endregion

    #region Internal Types
    /// <summary>
    /// Object to track Standard Action Based Callbacks
    /// </summary>
    abstract class EventItem
    {
        internal object Source;
        abstract internal void Execute(Dispatcher dispatcher);
    }

    class EventItem<T> : EventItem
    {
        internal Action<object, T> Callback;
        internal T EventArgs;

        internal override void Execute(Dispatcher dispatcher)
        {
            Callback?.Invoke(this.Source, EventArgs);
        }
    }

    /// <summary>
    ///  Object to track Timer based Callbacks
    /// </summary>
    class TimerTracker
    {
        internal Timer Timer;
        internal List<TimerTrackerCallback> Callbacks;
    }

    /// <summary>
    /// Object to track a single timer callback and its sender
    /// </summary>
    abstract class TimerTrackerCallback
    {
        internal object Sender;
        abstract internal void Execute(Dispatcher dispatcher);
    }

    /// <summary>
    /// Object to track a single timer callback and its sender
    /// </summary>
    class TimerTrackerCallback<T> : TimerTrackerCallback where T : class
    {
        internal Action<object, T> Callback;
        internal override void Execute(Dispatcher dispatcher)
        {
            dispatcher.eventQueue.Add(new EventItem<EventArgs>()
            {
                Callback = (o, e) =>
                {
                    Callback.Invoke(this.Sender, e as T);
                }
            });
        }
    }

    // Object use to handle Background Work Items
    abstract class BackgroundWorkItem
    {
        internal object EventArgs { get; set; }
        abstract internal void Execute(Dispatcher dispatcher);
    }

    class BoolBackgroundWorkItem : BackgroundWorkItem
    {
        internal Func<bool> Action;
        internal Action DefaultCallback;
        internal Action ErrorCallback;

        internal override void Execute(Dispatcher dispatcher)
        {
            if (Action.Invoke())
                dispatcher.eventQueue.Add(new EventItem<EventArgs>() { Callback = (o, e) => { DefaultCallback.Invoke(); } });
            else if (ErrorCallback != null)
                dispatcher.eventQueue.Add(new EventItem<EventArgs>() { Callback = (o, e) => { ErrorCallback.Invoke(); } });
        }
    }

    class BackgroundWorkItem<T> : BackgroundWorkItem
    {
        internal Func<T> Action;
        internal Action<T> Callback;

        internal override void Execute(Dispatcher dispatcher)
        {
            // Invoke Callback on This Thread
            T val = Action.Invoke();

            dispatcher.eventQueue.Add(new EventItem<EventArgs>() { Callback = (o, e) => { Callback.Invoke(val); } });
        }
    }

    class BackgroundWorkItem<T, ContextType> : BackgroundWorkItem
    {
        internal ContextType Context;
        internal Func<T> Action;
        internal Action<T, ContextType> Callback;

        internal override void Execute(Dispatcher dispatcher)
        {
            // Invoke Callback on This Thread
            T val = Action.Invoke();

            dispatcher.eventQueue.Add(new EventItem<EventArgs>() { Callback = (o, e) => { Callback.Invoke(val, Context); } });
        }
    }

    #endregion
}
