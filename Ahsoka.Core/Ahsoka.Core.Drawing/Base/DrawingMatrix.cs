

namespace Ahsoka.Core.Drawing.Base;

public class DrawingMatrix
{
    public object NativeObject { get; internal set; }

    public DrawingMatrix(object nativeObject)
    {
        NativeObject = nativeObject;
    }

}
