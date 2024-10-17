using Ahsoka.Core.Drawing.Utility;

namespace Ahsoka.Core.Drawing.Base;

public class DrawFrameArgs
{
    public IDrawingApi DrawingApi { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public List<TouchEventArgs> Events { get; set; }
    public List<DrawingRect> ClipRegions { get; set; } = new();
    public bool ShouldClip { get; set; } = false;
    public bool ShouldRender { get; set; }
}

