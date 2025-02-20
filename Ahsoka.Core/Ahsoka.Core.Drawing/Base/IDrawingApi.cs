using Ahsoka.Core.Drawing.Base;

namespace Ahsoka.Core.Drawing;

public interface IDrawingApi
{
    #region DrawingAPI 

    int ScreenWidth { get; }
    int ScreenHeight { get; }

    // DrawingFunctions
    void Clear(DrawingColor color);

    void DrawRectangle(DrawingRect rectangle, DrawingPaint paint);
    void DrawRectangle(DrawingRect rectangle, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth);

    void DrawRoundRectangle(DrawingRect rectangle, float cornerRadius, DrawingPaint paint);
    void DrawRoundRectangle(DrawingRect rectangle, float cornerRadius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth);

    void DrawCircle(float centerX, float centerY, float radius, DrawingPaint paint);
    void DrawCircle(float centerX, float centerY, float radius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth);

    void DrawBitmap(DrawingImage image, DrawingRect rect);
    void DrawText(string text, float x, float y, DrawingTextInfo textInfo);
    float MeasureText(string text, DrawingTextInfo textInfo);

    DrawingPath CreatePath(string svgPathData);
    void DrawPath(DrawingPath path, DrawingPaint paint);

    DrawingImage CreateImage(string fileName);
    DrawingImage CreateImage(byte[] data);
    DrawingTypeface LoadTypeface(string fileName, string typefaceName);
    void PushClip(DrawingRect rect);
    void PushClip(List<DrawingRect> rects);
    void PopClip();

    void PushMatrix(params DrawingMatrix[] matrices);
    void PopMatrix();
    DrawingMatrix CreateIdentityMatrix();
    DrawingMatrix CreateRotationMatrix(float radians);
    DrawingMatrix CreateTranslateMatrix(float x, float y);
    DrawingMatrix CreateScaleMatrix(float x, float y);

    void DeviceToScreen(ref int x, ref int y);
    #endregion
}
