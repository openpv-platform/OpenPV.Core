using SkiaSharp;

namespace Ahsoka.Core.Drawing.Base;

public class DrawingPaint
{
    internal object NativeFillObject { get; set; }
    internal object NativeStrokeObject { get; set; }

    public DrawingColor StrokeColor { get; }

    public DrawingColor FillStartColor { get; }
    public DrawingColor FillEndColor { get; }

    public DrawingPaintStyles PaintStyle { get; }
    public float GradientAngle { get; }

    public DrawingRect FillBounds { get; }

    public float StrokeWidth { get; }

    public bool IsStroke { get { return StrokeColor != null && StrokeColor != DrawingColor.Empty; } }
    public bool IsFill { get { return FillStartColor != null && FillStartColor != DrawingColor.Empty; } }

    public DrawingPaint(DrawingColor strokeColor, float strokeWidth, DrawingColor fillColor)
    {
        StrokeColor = strokeColor;
        FillStartColor = fillColor;
        StrokeWidth = strokeWidth;
    }

    public DrawingPaint(DrawingColor strokeColor, float strokeWidth, DrawingColor fillStartColor, DrawingColor fillEndColor, DrawingRect fillBounds, DrawingPaintStyles paintStyle, float gradientAngle)
    {
        StrokeColor = strokeColor;
        FillStartColor = fillStartColor;
        FillEndColor = fillEndColor;
        StrokeWidth = strokeWidth;
        FillBounds = fillBounds;
        PaintStyle = paintStyle;
        GradientAngle = gradientAngle;
    }
}

public enum DrawingPaintStyles
{
    Solid,
    Gradient,
    // Radial // TODO
}
