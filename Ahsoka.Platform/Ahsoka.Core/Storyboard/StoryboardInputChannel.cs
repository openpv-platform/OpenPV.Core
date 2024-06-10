namespace Ahsoka.Storyboard;

internal class StoryboardInputChannel : StoryboardChannelBase
{
    #region Fields
    #endregion

    #region Methods
    public StoryboardInputChannel(string channelName, IStoryboardAPI api) :
        base(channelName, api)
    {

    }

    protected override int OnOpenChannel(string channelName)
    {
        return Api.CreateInboundChannel(channelName);
    }

    public StoryboardMessage ReceiveMessage(int timeoutInMs = 5000)
    {
        // Block until we recieve a message.
        return Api.ReceiveEvent(ChannelId, timeoutInMs);
    }

    internal void CloseChannel()
    {
        Api.DestroyChannel(ChannelId);
    }


    #endregion
}
