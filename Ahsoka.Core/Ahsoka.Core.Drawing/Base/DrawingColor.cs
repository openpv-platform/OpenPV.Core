namespace Ahsoka.Core.Drawing.Base;

public class DrawingColor
{
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }
    public byte A { get; init; }

    public static DrawingColor Empty { get; internal set; } = new DrawingColor(0, 0, 0, 0);

    public DrawingColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public DrawingColor(uint color)
    {
        B = (byte)(color & 0xFF);
        G = (byte)((color >> 8) & 0xFF);
        R = (byte)((color >> 16) & 0xFF);
        A = (byte)((color >> 24) & 0xFF);
    }
}
