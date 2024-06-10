using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Ahsoka.Storyboard;

class StoryboardAPIMock : IStoryboardAPI
{
    #region Fields
    readonly object syncRoot = new();
    int nextChannel = 0;
    readonly Dictionary<string, int> channelNames = new();
    readonly Dictionary<int, StoryboardQueue> messages = new();
    #endregion

    #region Properties
    internal static StoryboardAPIMock Default { get; set; } = new StoryboardAPIMock();
    #endregion

    #region IStoryboardAPI Methods
    int IStoryboardAPI.CreateInboundChannel(string channelName)
    {
        lock (syncRoot)
            return CreateChannel(channelName);
    }

    int IStoryboardAPI.CreateOutboundChannel(string channelName)
    {
        lock (syncRoot)
            return CreateChannel(channelName);
    }

    StoryboardMessage IStoryboardAPI.ReceiveEvent(int id, int timeoutInMs)
    {
        if (messages.TryGetValue(id, out StoryboardQueue queue))
        {
            StoryboardMessage message = null;
            if (queue.WaitCounter.WaitOne(timeoutInMs))  // If Not Timeout
                lock (syncRoot)
                    if (queue.Queue.TryDequeue(out message)) // If no object then we are shutting down
                        return message;

            return null;
        }
        else
            throw new ApplicationException("ReceiveEvent: Channel Id does not exist.");
    }

    int IStoryboardAPI.SendEvent(int id, StoryboardMessage message)
    {
        lock (syncRoot)
            if (messages.TryGetValue(id + 1, out StoryboardQueue queue)) // Get the Next Channel 
            {
                queue.Queue.Enqueue(message);
                queue.WaitCounter.Release();
                return 1;
            }
            else
                throw new ApplicationException("ReceiveEvent: Channel Id does not exist.");
    }

    void IStoryboardAPI.DestroyChannel(int id)
    {
        if (messages.TryGetValue(id, out StoryboardQueue queue)) // Get the Next Channel 
        {
            // Release Wait
            queue.WaitCounter.Release();
        }
    }
    #endregion

    #region Methods
    private int CreateChannel(string channelName)
    {
        if (channelNames.ContainsKey(channelName))
            return channelNames[channelName];
        else
        {
            nextChannel++;
            channelNames[channelName] = nextChannel;
            messages[nextChannel] = new StoryboardQueue();
            return nextChannel;
        }
    }

    public string EngineRootPath()
    {
        return ".";
    }

    public string GetSBEnginePath()
    {
        return ".";
    }

    public Process StartProcess(string startupCommand, string pathToSBProject, string startupArgs)
    {
        return new Process();
    }
    #endregion

    #region StoryboardQueue
    class StoryboardQueue
    {
        public Queue<StoryboardMessage> Queue { get; set; } = new Queue<StoryboardMessage>();
        public Semaphore WaitCounter { get; set; } = new Semaphore(0, int.MaxValue);
    }
    #endregion
}
