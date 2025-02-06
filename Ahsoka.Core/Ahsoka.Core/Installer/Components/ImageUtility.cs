using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace Ahsoka.Installer.Components;

internal static class ImageUtility
{
    public static MemoryStream RotateImage(int orientation, MemoryStream data)
    {
        var image = Image.Load(data);
        if (orientation != 180)
            image.Mutate(x => x.Rotate(orientation));

        data.Dispose();
        data = new MemoryStream();
        image.SaveAsPng(data);
        data.Seek(0, SeekOrigin.Begin);
        return data;
    }
}