using Ahsoka.Core.Drawing.Base;

namespace Ahsoka.Core.Drawing;

public abstract class DrawingApiBase<Context> : IDrawingApi where Context : class
{
    public Context CurrentContext { get; protected set; }

    public bool ShowClipRectangles { get; set; }

    public int ScreenWidth { get; protected set; }
    public int ScreenHeight { get; protected set; }

    public abstract void PushClip(List<DrawingRect> rects);
    public abstract void PushClip(DrawingRect rect);
    public abstract void PopClip();

    public abstract void Clear(DrawingColor color);
    public abstract DrawingImage CreateImage(string fileName);
    public abstract DrawingImage CreateImage(byte[] data);
    public abstract DrawingTypeface LoadTypeface(string fileName, string typefaceName);

    public abstract float MeasureText(string text, DrawingTextInfo textInfo);

    public abstract void DrawBitmap(DrawingImage image, DrawingRect rect);
    public abstract void DrawCircle(float centerX, float centerY, float radius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth);
    public abstract void DrawRectangle(DrawingRect rectangle, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth);
    public abstract void DrawRoundRectangle(DrawingRect rectangle, float cornerRadius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth);
    public abstract void DrawText(string text, float x, float y, DrawingTextInfo textInfo);

    public abstract void DeviceToScreen(ref int x, ref int y);
}