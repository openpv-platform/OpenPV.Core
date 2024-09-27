using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace Ahsoka.Installer.Components;

internal static class ImageUtility
{
    public static MemoryStream RotateImage(Orientation orientation, MemoryStream data)
    {
        var image = Image.Load(data);
        if (orientation == Orientation.Rotate180)
            image.Mutate(x => x.Rotate(180));
        else if (orientation == Orientation.Rotate270)
            image.Mutate(x => x.Rotate(270));
        else if (orientation == Orientation.Rotate90)
            image.Mutate(x => x.Rotate(90));

        data.Dispose();
        data = new MemoryStream();
        image.SaveAsPng(data);
        data.Seek(0, SeekOrigin.Begin);
        return data;
    }
}