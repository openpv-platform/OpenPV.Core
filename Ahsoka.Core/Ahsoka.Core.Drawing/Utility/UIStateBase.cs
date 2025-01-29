using Ahsoka.Core.Drawing.Base;

namespace Ahsoka.Core.Drawing.Utility;

public abstract class UIStateBase<DrawingApiT> where DrawingApiT : class
{
    #region Methods

    public UIStateBase()
    {
    }

    public UIStateBase<DrawingApiT> HandleAction(string action)
    {
        return OnHandleAction(action);
    }

    public void DrawState(DrawFrameArgs args)
    {
        OnDrawState(args);
    }

    public void HandleTouch(TouchEventArgs args)
    {
        OnHandleTouch(args);
    }
    #endregion

    #region Abstract Methods
    protected abstract void OnDrawState(DrawFrameArgs args);

    protected abstract UIStateBase<DrawingApiT> OnHandleAction(string action);

    protected virtual void OnHandleTouch(TouchEventArgs args) { }
    #endregion
}