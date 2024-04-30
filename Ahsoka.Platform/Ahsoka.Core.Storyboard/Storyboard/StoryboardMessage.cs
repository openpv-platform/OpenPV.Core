using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ahsoka.Storyboard;

/// <summary>
/// General Purpose object for managing Storyboard Messages (events) for the SBIO Interface
/// </summary>
public class StoryboardMessage
{
    /// <summary>
    /// Event Name 
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// String representing the In/Out characteristic of Event
    /// </summary>
    public StoryboardMessageDirection Direction { get; set; }

    /// <summary>
    /// Target Object in Storyboard
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// Format of the SBIO Message (See Storyboard Docs for Details)
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// Data being sent / received over SBIO.
    /// </summary>
    public byte[] Data { get; set; }

}
