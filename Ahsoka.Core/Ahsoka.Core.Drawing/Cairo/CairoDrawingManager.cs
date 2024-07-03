using Ahsoka.Core.Drawing.Base;
using TouchEventArgs = Ahsoka.Core.Drawing.Base.TouchEventArgs;

namespace Ahsoka.Core.Drawing.Cairo;


public abstract class CairoDrawingManager : DrawingManager<CairoDrawingApi>, IDrawingManager
{
    #region Fields
    CairoDrawingWindow ahsokaWindow;
    #endregion

    #region Methods
    public CairoDrawingManager()
    {

    }

    void IDrawingManager.Init()
    {
        OnInit();
    }

    void IDrawingManager.PrepareDrawingApi()
    {
        ahsokaWindow = new CairoDrawingWindow(OperatingSystem.IsLinux());
        DrawingApi = new CairoDrawingApi(ahsokaWindow.Bounds);

        float screenWidth = ahsokaWindow.Bounds.Width;
        float screenHeight = ahsokaWindow.Bounds.Height;

        ahsokaWindow.TouchReceived += (o, args) =>
        {

            int x = args.X;
            int y = args.Y;
            DrawingApi.DeviceToScreen(ref x, ref y);

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
            DrawingApi.StartFrame(args.Context);

            DrawFrame();

            DrawingApi.EndFrame();
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
