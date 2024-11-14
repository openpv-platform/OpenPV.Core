
namespace Ahsoka.Core.Drawing.Base;

/// <summary>
///  Interface for Cross Plaform SKIA Window Implementations.
/// </summary>
internal interface IDrawingWindow
{
    IDrawingApi CreateWindow(DrawingWindow apiWindow, DrawingRect size, bool fullScreen, string appid, int surfaceId);
    IDrawingApi CreateWindow(DrawingWindow apiWindow, int surfaceId, string appid);
    void ShowAndRun();
    void Close();
    void HandleEvent(EventHandler handler);
}

