using Ahsoka.Core.Drawing.Base;

namespace Ahsoka.Core.Drawing.Utility;

public abstract class SkiaDrawingManager : DrawingManager<SkiaDrawingApi>, IDrawingManager
{
    #region Fields
    DrawingWindow ahsokaWindow;
    #endregion

    #region Methods
    public SkiaDrawingManager()
    {

    }

    void IDrawingManager.Init()
    {
        OnInit();
    }

    void IDrawingManager.PrepareDrawingApi()
    {
        ahsokaWindow = new DrawingWindow(OperatingSystem.IsLinux()) { FrameRateEnabled = false };
        DrawingApi = ahsokaWindow.GetApi();

        float screenWidth = ahsokaWindow.Bounds.Width;
        float screenHeight = ahsokaWindow.Bounds.Height;

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
    }

    protected override void OnClose(int result = 0)
    {
        ahsokaWindow?.Close();
    }
    #endregion

}