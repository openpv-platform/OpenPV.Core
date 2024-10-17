namespace Ahsoka.Core.Drawing.Base;

public class DrawingTextInfo
{
    public DrawingColor Color { get; set; }
    public float TextSize { get; set; } = 12.0f;
    public DrawingTypeface Typeface { get; set; }
    public DrawingTextAlignment Alignment { get; set; } = DrawingTextAlignment.Left;
    public DrawingTextWeight TextWeight { get; set; }
}


public enum DrawingTextAlignment
{
    Left,
    Center,
    Right
}

public enum DrawingTextWeight
{
    Normal,
    Bold,
    Italic
}
