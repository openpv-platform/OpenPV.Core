namespace Ahsoka.Core.Drawing.Base;


public class DrawingImage : IDisposable
{
    public object NativeObject { get; internal set; }
    public int Width { get; internal set; }
    public int Height { get; internal set; }

    public DrawingImage(object nativeObject, int width, int height)
    {
        NativeObject = nativeObject;
        Width = width;
        Height = height;
    }

    public void Dispose()
    {
        if (NativeObject is not null and IDisposable dispose)
            dispose.Dispose();
    }
}

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

public class DrawingTypeface
{
    public object NativeTypeface { get; init; }
    public string TypefaceName { get; init; }

    public DrawingTypeface(object nativeTypeface, string typefaceName)
    {
        NativeTypeface = nativeTypeface;
        TypefaceName = typefaceName;
    }
}

public class DrawingRect
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public float MidX { get { return X + Width / 2; } }
    public float MidY { get { return Y + Height / 2; } }

    public static DrawingRect Empty { get; internal set; } = new DrawingRect(0, 0, 0, 0);

    public bool Contains(float x, float y)
    {
        return x >= X
             && y >= Y
             && x <= X + Width
             && y <= Y + Height;
    }

    public void Offset(int xOffset, int yOffset)
    {
        X += xOffset;
        Y += yOffset;
    }

    public DrawingRect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

}

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
