namespace Ahsoka.Core.Drawing.Base;

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
