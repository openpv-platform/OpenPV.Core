using Ahsoka.Storyboard;
using Ahsoka.Utility;

namespace Ahsoka.Demo.Storyboard;

public class DemoViewModelBase : ViewModelBase
{
    #region Fields
    #endregion

    #region Properties
    public static StoryboardClient StorybardClient { get; set; }
    #endregion

    #region Methods
    public DemoViewModelBase()
    {

    }
    #endregion

    #region Storyboard Updates
    public void UpdateStoryboard()
    {
        OnUpdateStoryboard();
        SendToStoryboard();
    }

    private void SendToStoryboard()
    {
        StorybardClient?.SendEvent(this);
    }

    public virtual void OnUpdateStoryboard() { }
    #endregion

    #region Model Updates
    public void UpdateModel(string target)
    {
        OnUpdateModel(target);
    }

    public virtual void OnUpdateModel(string target)
    {
    }
    #endregion
}
