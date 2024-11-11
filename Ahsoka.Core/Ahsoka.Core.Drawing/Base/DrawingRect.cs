

namespace Ahsoka.Core.Drawing.Base;



public class DrawingSize
{ 
    public float Width { get; set; }
    public float Height { get; set; }

    public DrawingSize(float width, float height)
    {
        Width = width;
        Height = height;
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

    public DrawingRect GetInflatedRectangle(float inflateX, float inflateY)
    {
        return new DrawingRect(X - inflateX / 2, Y - inflateY / 2, Width + inflateX, Height + inflateY);
    }

    public DrawingRect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

}
