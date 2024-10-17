
namespace Ahsoka.Core.Drawing.Base;

/// <summary>
///  Interface for Cross Plaform SKIA Window Implementations.
/// </summary>
internal interface IDrawingWindow
{
    IDrawingApi CreateWindow(DrawingWindow apiWindow, ref DrawingRect bounds, bool fullScreen);
    void ShowAndRun();
    void Close();
    void HandleEvent(EventHandler handler);
}

