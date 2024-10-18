using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using SkiaSharp;

public class SkiaDrawingApi : DrawingApiBase<SKSurface>
{
    private SKMatrix matrix = SKMatrix.CreateIdentity();
    public Orientation CurrentOrientation { get; init; }

    #region Drawing
    DrawFrameArgs frameArgs;
    SKPaint clipRectanglePaint;

    public SKSurface GetSurface()
    {
        return CurrentContext;
    }

    public SkiaDrawingApi(DrawingRect drawingSize, SKSurface surface, Orientation? drawingOrientation = null)
    {
        drawingOrientation ??= SystemInfo.HardwareInfo.ScreenInfo.CurrentOrientation;

        bool isPortrait = drawingOrientation is Orientation.Rotate90 or Orientation.Rotate270;
        ScreenHeight = isPortrait ? (int)drawingSize.Width : (int)drawingSize.Height;
        ScreenWidth = isPortrait ? (int)drawingSize.Height : (int)drawingSize.Width;

        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Orientation Set to {drawingOrientation}");
        CurrentOrientation = drawingOrientation.Value;

        InitMatrix((int)drawingSize.Width, (int)drawingSize.Height);

        this.CurrentContext = surface;

        clipRectanglePaint = new SKPaint() { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        frameArgs = new DrawFrameArgs() { DrawingApi = this, Height = (int)ScreenHeight, Width = (int)ScreenWidth, Events = new(), ClipRegions = new() };
    }

    internal DrawFrameArgs StartFrame()
    {
        CurrentContext.Canvas.Save();
        CurrentContext.Canvas.SetMatrix(matrix);

        frameArgs.ClipRegions.Clear();
        frameArgs.Events.Clear();
        return frameArgs;
    }

    internal void FinishFrame(DrawFrameArgs args)
    {
        CurrentContext.Canvas.Restore();
    }

    public override void PushClip(List<DrawingRect> rects)
    {
        CurrentContext.Canvas.Save();
        CurrentContext.Canvas.ClipRegion(rects.AsSKRegion(), SKClipOperation.Intersect);
    }

    public override void PushClip(DrawingRect rects)
    {
        CurrentContext.Canvas.Save();
        CurrentContext.Canvas.ClipRect(rects.AsSKRect(), SKClipOperation.Intersect);
    }

    public override void PopClip()
    {
        if (ShowClipRectangles)
            CurrentContext.Canvas.DrawRegion(frameArgs.ClipRegions.AsSKRegion(), clipRectanglePaint);
      
        CurrentContext.Canvas.Restore();
    }

    public override void DeviceToScreen(ref int x, ref int y)
    {
        int origX = x;
        int origY = y;

        switch (CurrentOrientation)
        {
            case Orientation.DefaultOrientation:
            default:
                x = origX;
                y = origY;
                break;

            case Orientation.Rotate180:
                x = ScreenWidth - 1 - origX;
                y = ScreenHeight - 1 - origY;
                break;

            case Orientation.Rotate90:
                x = origY;
                y = ScreenHeight - origX - 1;
                break;

            case Orientation.Rotate270:
                x = ScreenWidth - origY - 1;
                y = origX;
                break;

        }
    }


    protected void InitMatrix(int width, int height)
    {
        switch (CurrentOrientation)
        {
            case Orientation.DefaultOrientation:
            default:
                break;

            case Orientation.Rotate180:
                matrix = SKMatrix.Concat(SKMatrix.CreateTranslation(width, height),SKMatrix.CreateRotation((float)Math.PI));
                break;

            case Orientation.Rotate90:
                matrix = SKMatrix.Concat(SKMatrix.CreateTranslation(width, 0), SKMatrix.CreateRotation((float)Math.PI / 2));
                break;

            case Orientation.Rotate270:
                matrix = SKMatrix.Concat(SKMatrix.CreateTranslation(0, height), SKMatrix.CreateRotation(3 * (float)Math.PI / 2));
                break;
        }

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
        var font = new SKFont()
        {
            Size = textInfo.TextSize,
            Typeface = textInfo.Typeface.AsSKTypeface(),
            Embolden = textInfo.TextWeight == DrawingTextWeight.Bold,
        };

        var paint = new SKPaint
        {
            Color = textInfo.Color.AsSKColor(),
            BlendMode = SKBlendMode.Multiply,
            IsAntialias = true,
        };

        var alignment = textInfo.Alignment == DrawingTextAlignment.Left ? SKTextAlign.Left :
                                                textInfo.Alignment == DrawingTextAlignment.Center ? SKTextAlign.Center :
                                                SKTextAlign.Right;

        CurrentContext.Canvas.DrawText(text, x, y, alignment, font, paint);
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

        var font = new SKFont()
        {
            Size = textInfo.TextSize,
            Typeface = textInfo.Typeface.AsSKTypeface(),
            Embolden = textInfo.TextWeight == DrawingTextWeight.Bold,
        };

        return font.MeasureText(text);
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

    internal static SKRegion AsSKRegion(this List<DrawingRect> value)
    {
        List<SKRectI> list = new();
        foreach (var item in value)
            list.Add(item.AsSKRectI());

        SKRegion region = new SKRegion();
        region.SetRects(new ReadOnlySpan<SKRectI>(list.ToArray()));
        return region;
    }

    internal static SKRectI AsSKRectI(this DrawingRect value)
    {
        return new SKRectI((int)value.X, (int)value.Y, (int)(value.X + value.Width), (int)(value.Y + value.Height));
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