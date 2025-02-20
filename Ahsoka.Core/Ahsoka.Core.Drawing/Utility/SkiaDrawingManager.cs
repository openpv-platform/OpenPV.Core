using Ahsoka.Core.Drawing.Base;

namespace Ahsoka.Core.Drawing.Utility;

public abstract class SkiaDrawingManager : DrawingManager<SkiaDrawingApi>, IDrawingManager
{
    #region Fields
    DrawingWindow ahsokaWindow;
    string appId;
    int surfaceId;
    #endregion

    #region Methods
    public SkiaDrawingManager(string appid = "AhsokaDrawingWindow", int surfaceid = 9999)
    {
        this.appId = appid;
        this.surfaceId = surfaceid;
    }

    void IDrawingManager.Init()
    {
        OnInit();
    }

    void IDrawingManager.PrepareDrawingApi()
    {
        ahsokaWindow = new DrawingWindow(surfaceId, appId) { FrameRateEnabled = false };
        DrawingApi = ahsokaWindow.GetApi();

        float screenWidth = ahsokaWindow.Size.Width;
        float screenHeight = ahsokaWindow.Size.Height;

        ahsokaWindow.PrepareFrame += (o, args) =>
        {
            PrepareFrame(args);
        };

        ahsokaWindow.RenderFrame += (o, args) =>
        {
            DrawFrame(args);
        };
    }

    void IDrawingManager.ShowAndRun()
    {
        ahsokaWindow.ShowAndRun();

        OnClose();
    }

    protected override void OnClose(int result = 0)
    {
        ahsokaWindow?.Close();
    }
    #endregion

}