using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Drawing;
using SkiaSharp;
using Ahsoka.Core.Drawing.Skia;

#pragma warning disable CS0618 // Type or member is obsolete

public class SkiaDrawingApi : DrawingApiBase<SKSurface>
{
    #region Drawing

    public SkiaDrawingApi(DrawingRect bounds)
    {
        this.ScreenWidth  = (int)bounds.Width;
        this.ScreenHeight = (int)bounds.Height;
    }

    public override void StartFrame(DrawFrameArgs frameArgs)
    {
        CurrentContext = frameArgs.Surface;
    }

    public override void FinishFrame()
    {
        //CurrentContext.Flush();
    }

    public override void SetClip(DrawingRect rect)
    {
        CurrentContext.Canvas.ClipRect(rect.AsSKRect());
    }

    public override void ClearClip()
    {
        CurrentContext.Canvas.ClipRect(SKRect.Empty);
    }

    public override void Clear(DrawingColor color)
    {
        CurrentContext.Canvas.Clear(color.AsSKColor());
    }

    public override void DrawBitmap(DrawingImage image, DrawingRect rect)
    {
        SKPaint paint = new SKPaint { IsStroke = false,  Color = new SKColor(255, 255, 255, 255), BlendMode= SKBlendMode.Multiply, IsAntialias = true };
        CurrentContext.Canvas.DrawBitmap(image.NativeObject as SKBitmap, rect.AsSKRect(), paint);
    }

    public override void DrawRoundRectangle(DrawingRect rectangle, float cornerRadius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth)
    {
        var rect = new SKRoundRect(rectangle.AsSKRect(), cornerRadius, cornerRadius);

        if (fillColor != null && fillColor != DrawingColor.Empty)
        {
            SKPaint paint = new SKPaint { IsStroke = false, StrokeWidth = strokeWidth, Color = fillColor.AsSKColor(), IsAntialias = true };
            CurrentContext.Canvas.DrawRoundRect(rect, paint);
        }

        if (strokeColor != null && strokeColor != DrawingColor.Empty)
        {
            SKPaint paint = new SKPaint { IsStroke = true, StrokeWidth = strokeWidth, Color = strokeColor.AsSKColor(), IsAntialias = true };
            CurrentContext.Canvas.DrawRoundRect(rect, paint);
        }
    }


    public override void DrawRectangle(DrawingRect rectangle, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth = 1)
    {
        if (fillColor != null && fillColor != DrawingColor.Empty)
        {
            SKPaint paint = new SKPaint { IsStroke = false, StrokeWidth = strokeWidth, Color = fillColor.AsSKColor(), IsAntialias = true };
            CurrentContext.Canvas.DrawRect(rectangle.AsSKRect(), paint);
        }

        if (strokeColor != null && strokeColor != DrawingColor.Empty)
        {
            SKPaint paint = new SKPaint { IsStroke = true, StrokeWidth = strokeWidth, Color = strokeColor.AsSKColor(), IsAntialias = true };
            CurrentContext.Canvas.DrawRect(rectangle.AsSKRect(), paint);
        }
    }

    public override void DrawCircle(float centerX, float centerY, float radius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth)
    {
        if (fillColor != null && fillColor != DrawingColor.Empty)
        {
            SKPaint paint = new SKPaint {  IsStroke = false, StrokeWidth = strokeWidth, Color = fillColor.AsSKColor(), IsAntialias = true };
            CurrentContext.Canvas.DrawCircle(centerX, centerY, radius, paint);
        }

        if (strokeColor != null && strokeColor != DrawingColor.Empty)
        {
            SKPaint paint = new SKPaint { IsStroke = true, StrokeWidth = strokeWidth, Color = strokeColor.AsSKColor(), IsAntialias = true };
            CurrentContext.Canvas.DrawCircle(centerX, centerY, radius, paint);
        }
    }

    public override void DrawText(string text, float x, float y, DrawingTextInfo textInfo)
    {

        var paint = new SKPaint
        {
            Color = textInfo.Color.AsSKColor(),
            TextSize = textInfo.TextSize,
            Typeface = textInfo.Typeface.AsSKTypeface(),
            FakeBoldText = textInfo.TextWeight == DrawingTextWeight.Bold,
            TextAlign = textInfo.Alignment == DrawingTextAlignment.Left ? SKTextAlign.Left :
                                                textInfo.Alignment == DrawingTextAlignment.Center ? SKTextAlign.Center :
                                                SKTextAlign.Right,
            BlendMode = SKBlendMode.Multiply,
            IsAntialias = true
        };

        CurrentContext.Canvas.DrawText(text, x, y, paint);
    }


    public override DrawingImage CreateImage(string fileName)
    {
        var image = SKBitmap.Decode(fileName);
        return new DrawingImage(image, image.Width, image.Height);
    }

    public override DrawingImage CreateImage(byte[] data)
    {
        var image = SKBitmap.Decode(data);
        return new DrawingImage(image, image.Width, image.Height);
    }

    public override float MeasureText(string text, DrawingTextInfo textInfo)
    {
        var paint = new SKPaint()
        {
            TextSize = textInfo.TextSize,
            Color = textInfo.Color.AsSKColor(),
            Typeface = textInfo.Typeface.AsSKTypeface(),
            FakeBoldText = textInfo.TextWeight == DrawingTextWeight.Bold,
        };

        return paint.MeasureText(text);
    }

    public override DrawingTypeface LoadTypeface(string fileName, string typefaceName)
    {
        return new DrawingTypeface(SKTypeface.FromFile("Assets/Font.ttf"), typefaceName);
    }

    #endregion
}

internal static class SkiaExtensions
{
    internal static SKRect AsSKRect(this DrawingRect value)
    {
        return new SKRect(value.X, value.Y, value.X + value.Width, value.Y + value.Height);
    }

    internal static SKColor AsSKColor(this DrawingColor value)
    {
        return new SKColor(value.R, value.G, value.B, value.A);
    }

    internal static SKTypeface AsSKTypeface(this DrawingTypeface value)
    {
        return (SKTypeface)value.NativeTypeface;
    }
}