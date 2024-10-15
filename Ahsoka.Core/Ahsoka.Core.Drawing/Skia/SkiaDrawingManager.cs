using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing.Skia;
using TouchEventArgs = Ahsoka.Core.Drawing.Base.TouchEventArgs;

namespace Ahsoka.Core.Drawing.Skia;

public abstract class SkiaDrawingManager : DrawingManager<SkiaDrawingApi>, IDrawingManager
{
    #region Fields
    SkiaDrawingWindow ahsokaWindow;
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
        ahsokaWindow = new SkiaDrawingWindow(OperatingSystem.IsLinux());
        DrawingApi = new SkiaDrawingApi(ahsokaWindow.Bounds);

        float screenWidth = ahsokaWindow.Bounds.Width;
        float screenHeight = ahsokaWindow.Bounds.Height;

        ahsokaWindow.TouchReceived += (o, args) =>
        {

            int x = args.X;
            int y = args.Y;
            //DrawingApi.DeviceToScreen(ref x, ref y);

            var inputTouchEvent = new TouchEventArgs()
            {
                Event = args.Event,
                Index = 1,
                X = x,
                Y = y,
            };

            HandleInputEvent(inputTouchEvent);
            args.Handled = true; // We've handled the event, stop processing
        };

        ahsokaWindow.DrawFrame += (o, args) =>
        {
            DrawingApi.StartFrame(args);

            DrawFrame();

            DrawingApi.FinishFrame();
        };
    }

    void IDrawingManager.ShowAndRun()
    {
        ahsokaWindow.ShowAndRun();
    }

    protected override void OnRequestDraw()
    {
        // Release One Draw Frame
        ahsokaWindow.RequestDraw();
    }

    protected override void OnClose(int result = 0)
    {
        ahsokaWindow?.Close();
    }
    #endregion

}