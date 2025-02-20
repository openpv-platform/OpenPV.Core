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
