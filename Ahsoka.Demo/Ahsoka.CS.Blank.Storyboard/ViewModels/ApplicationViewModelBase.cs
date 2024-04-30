using Ahsoka.Storyboard;
using Ahsoka.Utility;

namespace Ahsoka.Application.Storyboard;
public abstract class ApplicationViewModelBase : StoryboardViewModelBase
{
    #region Fields
    static readonly object syncRoot = new();
    #endregion

    #region Properties
    public static StoryboardClient StorybardClient { get; set; }
    #endregion

    #region Methods
    public ApplicationViewModelBase(string name, string format, StoryboardMessageDirection direction) : base(name, format, direction)
    {

    }

    public ApplicationViewModelBase(StoryboardMessage message) : base(message) { }
    #endregion

    #region Storyboard Updates
    public void UpdateStoryboard()
    {
        OnUpdateStoryboard();
        SendToStoryboard();
    }

    private void SendToStoryboard()
    {
        StorybardClient?.SendEvent(Message);
    }

    public virtual void OnUpdateStoryboard() { }
    #endregion

    #region Model Updates
    public void UpdateModel(string target = null)
    {
        if (target == null)
            target = Message.Target;
        OnUpdateModel(target);
    }

    public virtual void OnUpdateModel(string target)
    {
    }
    #endregion
}
