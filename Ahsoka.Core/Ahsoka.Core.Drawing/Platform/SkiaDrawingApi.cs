using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using SkiaSharp;
using Ahsoka.Services.LayerManager;

public class SkiaDrawingApi : DrawingApiBase<SKSurface>
{
    private SKMatrix frameMatrix = SKMatrix.CreateIdentity();
    public Orientation CurrentOrientation { get; init; }

    #region Drawing
    DrawFrameArgs frameArgs;
    SKPaint clipRectanglePaint;

    public SKSurface GetSurface()
    {
        return CurrentContext;
    }

    public SkiaDrawingApi(DrawingSize drawingSize, SKSurface surface, Orientation? drawingOrientation = null)
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
        CurrentContext.Canvas.SetMatrix(frameMatrix);

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

    public override void Clear(DrawingColor color)
    {
        CurrentContext.Canvas.Clear(color.AsSKColor());
    }

    public override void DrawBitmap(DrawingImage image, DrawingRect rect)
    {
        SKPaint paint = new SKPaint { IsStroke = false,  Color = new SKColor(255, 255, 255, 255), BlendMode= SKBlendMode.Multiply, IsAntialias = true };
        CurrentContext.Canvas.DrawBitmap(image.NativeObject as SKBitmap, rect.AsSKRect(), paint);
    }
   
    public override void DrawRectangle(DrawingRect rectangle, DrawingPaint paint)
    {
        if (paint.IsFill)
            CurrentContext.Canvas.DrawRect(rectangle.AsSKRect(), paint.AsFillPaint());

        if (paint.IsStroke)
            CurrentContext.Canvas.DrawRect(rectangle.AsSKRect(), paint.AsStrokePaint());
    }
    
    public override void DrawRectangle(DrawingRect rectangle, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth = 1)
    {
        DrawRectangle(rectangle, new DrawingPaint(strokeColor, strokeWidth, fillColor));
    }

    public override void DrawRoundRectangle(DrawingRect rectangle, float cornerRadius, DrawingPaint paint)
    {
        var rect = new SKRoundRect(rectangle.AsSKRect(), cornerRadius, cornerRadius);

        if (paint.IsFill)
            CurrentContext.Canvas.DrawRoundRect(rect, paint.AsFillPaint());

        if (paint.IsStroke)
            CurrentContext.Canvas.DrawRoundRect(rect, paint.AsStrokePaint());
    }

    public override void DrawRoundRectangle(DrawingRect rectangle, float cornerRadius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth)
    {
        DrawRoundRectangle(rectangle, cornerRadius, new DrawingPaint(strokeColor, strokeWidth, fillColor));
    }

    public override void DrawCircle(float centerX, float centerY, float radius, DrawingPaint paint)
    {
        if (paint.IsFill)
            CurrentContext.Canvas.DrawCircle(centerX, centerY, radius, paint.AsFillPaint());

        if (paint.IsStroke)
            CurrentContext.Canvas.DrawCircle(centerX, centerY, radius, paint.AsStrokePaint());
    }

    public override void DrawCircle(float centerX, float centerY, float radius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth)
    {
        DrawCircle(centerX, centerY, radius, new DrawingPaint(strokeColor, strokeWidth, fillColor));
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

    public override DrawingPath CreatePath(string svgPathData)
    {
        SKPath path = SKPath.ParseSvgPathData(svgPathData);
        return new DrawingPath() { NativeObject = path};
    }

    public override void DrawPath(DrawingPath path, DrawingPaint paint)
    {
        if (paint.IsFill)
            CurrentContext.Canvas.DrawPath(path.AsSKPath(), paint.AsFillPaint());

        if (paint.IsStroke)
            CurrentContext.Canvas.DrawPath(path.AsSKPath(), paint.AsStrokePaint());
    }
    #endregion

    #region MatrixMethods
    public override void PushMatrix(params DrawingMatrix[] matrices)
    {
        CurrentContext.Canvas.Save();

        var newMatrix = CurrentContext.Canvas.TotalMatrix;
        foreach (var item in matrices)
            newMatrix = SKMatrix.Concat(newMatrix, (SKMatrix)item.NativeObject);

       CurrentContext.Canvas.SetMatrix(newMatrix);
    }

    public override void PopMatrix()
    {
        CurrentContext.Canvas.Restore();
    }

    public override DrawingMatrix CreateIdentityMatrix()
    {
        return new DrawingMatrix(SKMatrix.Identity);
    }

    public override DrawingMatrix CreateRotationMatrix(float radians)
    {
        return new DrawingMatrix(SKMatrix.CreateRotation(radians));
    }

    public override DrawingMatrix CreateTranslateMatrix(float x, float y)
    {
        return new DrawingMatrix(SKMatrix.CreateTranslation(x, y));
    }

    public override DrawingMatrix CreateScaleMatrix(float x, float y)
    {
        return new DrawingMatrix(SKMatrix.CreateScale(x, y));
    }
    #endregion

    #region Private Methods
    void InitMatrix(int width, int height)
    {
        switch (CurrentOrientation)
        {
            case Orientation.DefaultOrientation:
            default:
                frameMatrix = SKMatrix.Identity;
                break;

            case Orientation.Rotate180:
                frameMatrix = SKMatrix.Concat(SKMatrix.CreateTranslation(width, height), SKMatrix.CreateRotation((float)Math.PI));
                break;

            case Orientation.Rotate90:
                frameMatrix = SKMatrix.Concat(SKMatrix.CreateTranslation(width, 0), SKMatrix.CreateRotation((float)Math.PI / 2));
                break;

            case Orientation.Rotate270:
                frameMatrix = SKMatrix.Concat(SKMatrix.CreateTranslation(0, height), SKMatrix.CreateRotation(3 * (float)Math.PI / 2));
                break;
        }

    }
    #endregion
}

/// <summary>
/// Extension Methods for Drawing
/// </summary>
public static class DrawingExtensions
{
    public static bool IntersectsRectangle(this List<DrawingRect> value, DrawingRect rectangle)
    {
        foreach (var item in value)
            if (item.AsSKRect().IntersectsWith(rectangle.AsSKRect()))
                return true;

        return false;
    }
}

internal static class SkiaExtensions
{
    internal static SKPath AsSKPath(this DrawingPath value)
    {
        return value.NativeObject as SKPath;
    }

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

    internal static SKPaint AsStrokePaint(this DrawingPaint paint)
    {
        if (paint.NativeStrokeObject == null)
            paint.NativeStrokeObject = new SKPaint { IsStroke = true, StrokeWidth = paint.StrokeWidth, Color = paint.StrokeColor.AsSKColor(), IsAntialias = true };
    
        return paint.NativeStrokeObject as SKPaint;
    }

    internal static SKPaint AsFillPaint(this DrawingPaint paint)
    {
        if (paint.NativeFillObject == null)
        {
            SKPaint nativePaint =  new SKPaint { IsStroke = false, StrokeWidth = paint.StrokeWidth, Color = paint.FillStartColor.AsSKColor(), IsAntialias = true };

            if (paint.PaintStyle != DrawingPaintStyles.Solid && paint.FillBounds != null)
            {
                nativePaint.Shader = SKShader.CreateLinearGradient(new SKPoint(paint.FillBounds.X, paint.FillBounds.MidY),
                                   new SKPoint(paint.FillBounds.Width, paint.FillBounds.MidY),
                                   new SKColor[] { paint.FillStartColor.AsSKColor(), paint.FillEndColor.AsSKColor()},
                                   SKShaderTileMode.Clamp);

                if (paint.GradientAngle != 0)
                    nativePaint.Shader = nativePaint.Shader.WithLocalMatrix(SKMatrix.CreateRotation(paint.GradientAngle, paint.FillBounds.MidY, paint.FillBounds.MidY));
            }

            paint.NativeFillObject = nativePaint;


        }
        return paint.NativeFillObject as SKPaint;
    }
}