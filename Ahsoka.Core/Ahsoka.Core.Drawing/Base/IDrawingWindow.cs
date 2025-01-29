
namespace Ahsoka.Core.Drawing.Base;

/// <summary>
///  Interface for Cross Plaform SKIA Window Implementations.
/// </summary>
internal interface IDrawingWindow
{
    IDrawingApi CreateWindow(DrawingWindow apiWindow, bool fullScreen, int width, int height, string appid);
    IDrawingApi CreateWindow(DrawingWindow apiWindow, int surfaceId, string appid);
    void ShowAndRun();
    void Close();
    void HandleEvent(EventHandler handler);
    int UpdateLocationRectangle(int surfaceId, int x, int y, int w, int h, bool commitChanges = true);
    int UpdateSourceRectangle(int surfaceId, int x, int y, int w, int h, bool commitChanges = true);
    int ChangeVisibility(int surfaceId, bool isVisible, float visibility, bool commitChanges = true);
    int GetSurfaceId();

    bool GetSurfaceVisibility(int surfaceId);
    float GetSurfaceOpacity(int surfaceId);
    DrawingRect GetSurfaceLocation(int surfaceId);
}

