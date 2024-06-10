using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;

namespace Ahsoka.Storyboard;

/// <summary>
/// Interface for the Storyboard API on different platforms.
/// </summary>
public interface IStoryboardAPI
{
    /// <summary>
    /// Returns id of sbio channel created for use in ReceiveEvent
    /// </summary>
    /// <param name="channelName">Name of the new Input Channel</param>
    /// <returns>Returns -1 on failure</returns>
    int CreateInboundChannel(string channelName);

    /// <summary>
    /// Returns id of sbio channel created for use in SendEvent
    /// </summary>
    /// <param name="channelName">Name of the new Output Channel</param>
    /// <returns>Returns -1 on failure</returns>
    int CreateOutboundChannel(string channelName);

    // Send Event to the Channel
    /// <summary>
    /// Sends a Message to SBIO
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <param name="message">Message to Send</param>
    /// <returns></returns>
    int SendEvent(int id, StoryboardMessage message);

    /// <summary>
    /// Block waiting for an event from the channel
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <param name="timeoutInMs">Timeout for message listener</param>
    /// <returns></returns>
    StoryboardMessage ReceiveEvent(int id, int timeoutInMs = 5000);

    // 
    /// <summary>
    /// Shut down and Destroy the Specified Channel Index.
    /// </summary>
    /// <param name="channel"></param>
    void DestroyChannel(int channel);

    /// <summary>
    ///  Path to the SB Engine Rooot.
    /// </summary>
    /// <returns></returns>
    string EngineRootPath();

    /// <summary>
    /// Get the Path to the Engine
    /// </summary>
    /// <returns></returns>
    string GetSBEnginePath();

    /// <summary>
    /// Start Storyboard
    /// </summary>
    /// <param name="startupCommand"></param>
    /// <param name="pathToSBProject"></param>
    /// <param name="startupArgs"></param>
    /// <returns></returns>
    Process StartProcess( string startupCommand, string pathToSBProject, string startupArgs);
}

