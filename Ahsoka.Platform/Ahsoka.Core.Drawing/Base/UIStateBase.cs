using Ahsoka.Core.Drawing.Base;

namespace Ahsoka.Core.Drawing;

public abstract class UIStateBase<IDrawingApi>
{
    #region Methods

    public UIStateBase()
    {
    }

    public UIStateBase<IDrawingApi> HandleAction(string action)
    {
        return OnHandleAction(action);
    }

    public void DrawState(IDrawingApi drawingApi, int width, int height)
    {
        OnDrawState(drawingApi, width, height);
    }

    public void HandleTouch(TouchEventArgs args)
    {
        OnHandleTouch(args);
    }
    #endregion

    #region Abstract Methods
    protected abstract void OnDrawState(IDrawingApi drawingApi, int width, int height);

    protected abstract UIStateBase<IDrawingApi> OnHandleAction(string action);

    protected virtual void OnHandleTouch(TouchEventArgs args) { }
    #endregion
}