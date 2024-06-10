using Ahsoka.Storyboard;
using Ahsoka.Utility;
using System.Collections.Generic;

namespace Ahsoka.Demo.Storyboard;

public abstract class DemoViewModelBase : StoryboardViewModelBase
{
    #region Fields
    #endregion

    #region Properties
    public static StoryboardClient StorybardClient { get; set; }
    #endregion

    #region Methods
    public DemoViewModelBase(string name, string format, StoryboardMessageDirection direction) : base(name, format, direction)
    {

    }

    public DemoViewModelBase(StoryboardMessage message) : base(message) { }
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
