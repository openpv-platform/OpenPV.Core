using Ahsoka.Storyboard;
using Ahsoka.Utility;

namespace Ahsoka.Application.Storyboard;
public class ApplicationViewModelBase : ViewModelBase
{
    #region Fields
    static readonly object syncRoot = new();
    #endregion

    #region Properties
    public static StoryboardClient StorybardClient { get; set; }
    #endregion

    #region Methods
    public ApplicationViewModelBase()
    {

    }
    #endregion

    #region Storyboard Updates
    public void UpdateStoryboard()
    {
        lock (syncRoot)
        {
            OnUpdateStoryboard();
            SendToStoryboard();
        }
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
