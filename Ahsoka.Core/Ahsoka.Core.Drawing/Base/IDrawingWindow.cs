
namespace Ahsoka.Core.Drawing.Base;

/// <summary>
///  Interface for Cross Plaform SKIA Window Implementations.
/// </summary>
internal interface IDrawingWindow
{
    IDrawingApi CreateWindow(DrawingWindow apiWindow, DrawingRect size, bool fullScreen, int surfaceId, string appid);
    IDrawingApi CreateWindow(DrawingWindow apiWindow, int surfaceId, string appid);
    void ShowAndRun();
    void Close();
    void HandleEvent(EventHandler handler);
    int UpdateWindowPosition(int surfaceId, int x, int y, int w, int h);
}

