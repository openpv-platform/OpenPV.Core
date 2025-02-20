using ProtoBuf;
using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.LayerManager;

[ProtoContract]
public class LayerManagerMessageTypes
{
    public enum Ids
    {
        NoMessage = 0,
        StopLogo = 1,

        UpdateSurfaceSetup = 2,
        GetSurfaceSetup = 3,

        UpdateSurfaceLocation = 4,
        UpdateSurfaceSource = 5,
        UpdateSurfaceVisibility = 6,
        UpdateSurfaceZOrder = 7,
    }
}

[ProtoContract]
public class LayerManagerConfiguration
{
    [ProtoMember(1)]
    public bool ShowUnknownSurfaces { get; set; }

    [ProtoMember(2)]
    public string BootLogoPath { get; set; }

    [ProtoMember(3)]
    public int Orientation { get; set; }

    [ProtoMember(4, IsRequired = true)]
    public List<SurfaceSetup> SurfaceSetup { get; set; } = new List<SurfaceSetup>();
}

[ProtoContract]
public class SurfaceSetup
{
    [ProtoMember(1)]
    public int SurfaceId { get; set; }

    [ProtoMember(3)]
    public SurfaceRectangle LocationRectangle { get; set; }

    [ProtoMember(4)]
    public SurfaceRectangle SourceRectangle { get; set; }

    [ProtoMember(5)]
    public SurfaceVisibility Visibility { get; set; }

    [ProtoMember(6)]
    public string SurfaceAppName { get; set; }
}

[ProtoContract]
public class SurfaceRectangle
{
    [ProtoMember(1)]
    public int SurfaceId { get; set; }

    [ProtoMember(2)]
    public int X { get; set; }

    [ProtoMember(3)]
    public int Y { get; set; }

    [ProtoMember(4)]
    public int Width { get; set; }

    [ProtoMember(5)]
    public int Height { get; set; }

    [ProtoMember(6)]
    public bool CommitChanges { get; set; }
}

[ProtoContract]
public class SurfaceVisibility
{
    [ProtoMember(1)]
    public int SurfaceId { get; set; }

    [ProtoMember(2)]
    public bool IsVisible { get; set; }

    [ProtoMember(3)]
    public float Opacity { get; set; }

    [ProtoMember(4)]
    public bool CommitChanges { get; set; }
}

[ProtoContract]
public class SurfaceOrder
{
    [ProtoMember(1)]
    public List<int> SurfaceId { get; set; }

    [ProtoMember(4)]
    public bool CommitChanges { get; set; }
}


[ProtoContract]
public class SurfaceRequest
{
    [ProtoMember(1)]
    public int SurfaceId { get; set; }
}
