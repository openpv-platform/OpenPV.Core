
namespace Ahsoka.Core.Drawing.Base;

/// <summary>
///  Interface for Cross Plaform SKIA Window Implementations.
/// </summary>
internal interface IDrawingWindow
{
    IDrawingApi CreateWindow(DrawingWindow apiWindow, DrawingSize size, bool fullScreen, string appid, int surfaceId);
    void ShowAndRun();
    void Close();
    void HandleEvent(EventHandler handler);
}

