using Ahsoka.Core.Drawing.Base;
using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Cairo;
using Color = Cairo.Color;
using Context = Cairo.Context;
using Rectangle = Cairo.Rectangle;

namespace Ahsoka.Core.Drawing.Cairo;

public class CairoDrawingApi : DrawingApiBase<Context>
{
    public Matrix Matrix { get; private set; } = new();
    public Orientation CurrentOrientation { get; protected set; }

    public CairoDrawingApi(DrawingRect drawingSize, Orientation? drawingOrientation = null)
    {
        drawingOrientation ??= SystemInfo.HardwareInfo.ScreenInfo.CurrentOrientation;

        bool isPortrait = drawingOrientation is Orientation.Rotate90 or Orientation.Rotate270;
        ScreenHeight = isPortrait ? (int)drawingSize.Width : (int)drawingSize.Height;
        ScreenWidth = isPortrait ? (int)drawingSize.Height : (int)drawingSize.Width;

        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Orientation Set to {drawingOrientation}");
        CurrentOrientation = drawingOrientation.Value;

        InitMatrix((int)drawingSize.Width, (int)drawingSize.Height);
    }

    protected override void OnStartFrame(Context context)
    {
        CurrentContext.Save();
        context.Transform(Matrix);
    }

    protected override void OnEndFrame()
    {
        CurrentContext.Restore();
    }

    protected void InitMatrix(int width, int height)
    {
        Matrix.InitIdentity();

        switch (CurrentOrientation)
        {
            case Orientation.DefaultOrientation:
            default:
                break;

            case Orientation.Rotate180:
                Matrix.Translate(width, height);
                Matrix.Rotate(Math.PI);
                break;

            case Orientation.Rotate90:
                Matrix.Translate(width, 0);
                Matrix.Rotate(Math.PI / 2);
                break;

            case Orientation.Rotate270:
                Matrix.Translate(0, height);
                Matrix.Rotate(3 * Math.PI / 2);
                break;
        }

    }

    public void DeviceToScreen(ref int x, ref int y)
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

    #region Drawing
    public override void SetClip(DrawingRect rect)
    {
        CurrentContext.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        CurrentContext.Clip();
    }

    public override void ClearClip()
    {
        CurrentContext.ResetClip();
    }

    public override void Clear(DrawingColor color)
    {
        CurrentContext.Stroke();
        CurrentContext.SetSourceColor(color.AsCairoColor());
        CurrentContext.Paint();
    }

    public override void DrawRectangle(DrawingRect rectangle, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth = 1)
    {
        CurrentContext.Rectangle(rectangle.AsCairoRectangle());
        CurrentContext.ClosePath();   // line back to start point
        FillAndStroke(strokeColor, fillColor, strokeWidth);
    }

    public override void DrawCircle(float centerX, float centerY, float radius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth = 1)
    {
        CurrentContext.Arc(centerX, centerY, radius, 0, Math.PI * 2);
        CurrentContext.ClosePath();   // Line back to start point
        FillAndStroke(strokeColor, fillColor, strokeWidth);
    }

    public override void DrawBitmap(DrawingImage image, DrawingRect rect)
    {
        // Set Set the Operator so we can draw transparent images
        var temp = CurrentContext.Operator;

        CurrentContext.SetSourceColor(new DrawingColor(0x00000000).AsCairoColor());
        var imageSource = image.AsCairoImage();
        CurrentContext.Operator = Operator.Add + 2; // Multiply Defined in Cairo Lib but not CairoSharp
        imageSource.DeviceScale = new PointD(imageSource.Width / rect.Width, imageSource.Height / rect.Height);
        imageSource.Show(CurrentContext, rect.X, rect.Y);

        CurrentContext.Operator = temp;
    }

    public void FillAndStroke(DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth)
    {
        if (fillColor != null && fillColor != DrawingColor.Empty)
        {
            CurrentContext.SetSourceColor(fillColor.AsCairoColor());
            CurrentContext.FillPreserve();
        }

        if (strokeColor != null && strokeColor != DrawingColor.Empty)
        {
            CurrentContext.SetSourceColor(strokeColor.AsCairoColor());
            CurrentContext.LineWidth = strokeWidth;
            CurrentContext.Stroke();
        }
    }

    public override DrawingImage CreateImage(byte[] data)
    {
        ImageSurface cloudTemp = new("Cloud.png");
        return new DrawingImage(cloudTemp, cloudTemp.Width, cloudTemp.Height);
    }

    public override DrawingImage CreateImage(string fileName)
    {
        ImageSurface cloudTemp = new(fileName);
        return new DrawingImage(cloudTemp, cloudTemp.Width, cloudTemp.Height);
    }

    public override void DrawRoundRectangle(DrawingRect rectangle, float radius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth)
    {
        double x = rectangle.X,
                y = rectangle.Y,
                width = rectangle.Width,
                height = rectangle.Height;

        double degrees = Math.PI / 180.0;

        CurrentContext.NewSubPath();
        CurrentContext.Arc(x + width - radius, y + radius, radius, -90 * degrees, 0 * degrees);
        CurrentContext.Arc(x + width - radius, y + height - radius, radius, 0 * degrees, 90 * degrees);
        CurrentContext.Arc(x + radius, y + height - radius, radius, 90 * degrees, 180 * degrees);
        CurrentContext.Arc(x + radius, y + radius, radius, 180 * degrees, 270 * degrees);
        CurrentContext.ClosePath();

        FillAndStroke(strokeColor, fillColor, strokeWidth);
    }

    public override void DrawText(string text, float x, float y, DrawingTextInfo textInfo)
    {
        SetFontInfo(textInfo);

        if (textInfo.Alignment == DrawingTextAlignment.Center)
        {
            var result = MeasureText(text, textInfo);
            CurrentContext.MoveTo(x - (result) / 2, y);
        }
        else if (textInfo.Alignment == DrawingTextAlignment.Right)
        {
            var result = MeasureText(text, textInfo);
            CurrentContext.MoveTo(x - result, y);
        }
        else
            CurrentContext.MoveTo(x, y);

        CurrentContext.ShowText(text);
        CurrentContext.Stroke();
    }

    private void SetFontInfo(DrawingTextInfo textInfo)
    {
        CurrentContext.SelectFontFace(textInfo.Typeface.TypefaceName,
                        textInfo.TextWeight.HasFlag(DrawingTextWeight.Italic) ? FontSlant.Italic : FontSlant.Normal,
                        textInfo.TextWeight.HasFlag(DrawingTextWeight.Bold) ? FontWeight.Bold : FontWeight.Normal);
        CurrentContext.SetSourceColor(textInfo.Color.AsCairoColor());
        CurrentContext.SetFontSize(textInfo.TextSize);
    }

    public override float MeasureText(string text, DrawingTextInfo textInfo)
    {
        SetFontInfo(textInfo);
        var result = CurrentContext.TextExtents(text);

        return (float)result.Width + (2 * (float)result.XBearing);
    }

    public override DrawingTypeface LoadTypeface(string fileName, string typefaceName)
    {
        if (SystemInfo.CurrentPlatform == PlatformFamily.Windows64)
        {
            // Should this be st
            WindowsFontConfig.AddFont(fileName);
            var fontSet = WindowsFontConfig.GetFontSet();
            return new DrawingTypeface(fontSet, typefaceName);
        }
        else if (SystemInfo.CurrentPlatform == PlatformFamily.MacosArm64)
        {
            //TODO: Support Fonts on Mac
            return new DrawingTypeface(new object(), typefaceName);
        }
        else
        {
            LinuxFontConfig.AddFont(fileName);
            var fontSet = LinuxFontConfig.GetFontSet();
            return new DrawingTypeface(fontSet, typefaceName);
        }

    }
    #endregion
}

static class CairoExtensions
{
    internal static Rectangle AsCairoRectangle(this DrawingRect value)
    {
        return new Rectangle(value.X, value.Y, value.Width, value.Height);
    }

    internal static ImageSurface AsCairoImage(this DrawingImage value)
    {
        return value.NativeObject as ImageSurface;
    }

    internal static Color AsCairoColor(this DrawingColor value)
    {
        return new Color(value.R / 255.0f, value.G / 255.0f, value.B / 255.0f, value.A / 255.0f);
    }
}