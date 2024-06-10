using Ahsoka.ServiceFramework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ahsoka.Storyboard;

internal abstract class StoryboardChannelBase : IDisposable
{
    #region Fields
    static readonly Dictionary<string, StoryboardChannelBase> openChannels = new();
    #endregion

    #region Properties

    public string ChannelName { get; private set; }

    protected int ChannelId { get; private set; } = -1;

    internal IStoryboardAPI Api { get; private set; }
    #endregion

    #region Public Interface
    public StoryboardChannelBase(string channelName, IStoryboardAPI api)
    {
        this.Api = api; ;
        this.ChannelName = channelName;

        if (openChannels.ContainsKey(channelName))
            throw new ApplicationException("You must specify a unique channel name to open this channel");

    }

    protected abstract int OnOpenChannel(string channelName);

    public void Dispose()
    {
        // Close Channel
        openChannels.Remove(this.ChannelName);

        GC.SuppressFinalize(this);
    }

    public void OpenChannel()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Opening Channel {ChannelName}");
        while (ChannelId == -1)
        {
            ChannelId = OnOpenChannel(this.ChannelName);
            if (ChannelId == -1)
                Thread.Sleep(100);
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Opened Channel {ChannelName} with Channel ID {ChannelId}");

    }


    #endregion
}
