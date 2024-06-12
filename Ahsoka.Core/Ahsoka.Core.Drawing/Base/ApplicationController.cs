using Ahsoka.Utility;

namespace Ahsoka.Core.Drawing;

public class ApplicationController
{
    public void RunApplication(IDrawingManager drawingManager)
    {
        using var stopwatch = new AhsokaStopwatch();
        stopwatch.ResetTiming("App Started");

        drawingManager.PrepareDrawingApi();
        stopwatch.ResetTiming("Drawing API's Ready");

        drawingManager.Init();
        stopwatch.ResetTiming("Init Completed");

        drawingManager.ShowAndRun(); // Blocks App Until Exit
    }
}
