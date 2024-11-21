
namespace Ahsoka.Core.Drawing.Base;

/// <summary>
///  Interface for Cross Plaform SKIA Window Implementations.
/// </summary>
internal interface IDrawingWindow
{
    IDrawingApi CreateWindow(DrawingWindow apiWindow, bool fullScreen, int width, int height, int surfaceId, string appid);
    IDrawingApi CreateWindow(DrawingWindow apiWindow, int surfaceId, string appid);
    void ShowAndRun();
    void Close();
    void HandleEvent(EventHandler handler);
    int ChangeLocation(int surfaceId, int x, int y, int w, int h, bool commitChanges = true);
    int ChangeSource(int surfaceId, int x, int y, int w, int h, bool commitChanges = true);
    int ChangeVisibility(int surfaceId, bool isVisible, float visibility, bool commitChanges = true);
}

