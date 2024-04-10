namespace Ahsoka.Storyboard;

internal class StoryboardOutputChannel : StoryboardChannelBase
{
    #region Fields
    #endregion

    #region Methods
    public StoryboardOutputChannel(string channelName, IStoryboardAPI api) : base(channelName, api)
    {

    }

    protected override int OnOpenChannel(string channelName)
    {
        return Api.CreateOutboundChannel(channelName);
    }

    public int SendEvent(StoryboardMessage message)
    {
        return Api.SendEvent(this.ChannelId, message);
    }

    public int SendEvent(object message, string target = null)
    {
        return Api.SendEvent(this.ChannelId, new StoryboardMessage(message, target));
    }
    #endregion
}
