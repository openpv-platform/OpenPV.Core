using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ahsoka.Services.Video;

internal class LinuxVideoOuOfProcServiceImplementation : VideoPlayerImpl
{
    static int playCount = 0;

    PlaybackState playbackState = PlaybackState.Ready;
    ulong totalMs = 0;
    ulong positionMs = 0;
    Process videoProcess = null;
    string videoPlayerPath = null;

    public LinuxVideoOuOfProcServiceImplementation()
    {
        videoPlayerPath = Directory.GetFiles(Path.GetDirectoryName(Environment.ProcessPath), $"Ahsoka.VideoPlayer.{SystemInfo.CurrentPlatform}", SearchOption.AllDirectories).FirstOrDefault();

        if (videoPlayerPath == null)
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"No Video Player found relative to application.  Ensure that Ahsoka.VideoPlayer.{SystemInfo.CurrentPlatform} exists in the AhsokaLib Directory");

        ProcessUtility.RunProcess("chmod", "+x " + videoPlayerPath, null, out string result, out string error);
    }

    protected override VideoStatus OnStartVideo(VideoPlaybackRequest videoPlaybackRequest)
    {
        // Handle Play Video
        if (playbackState != PlaybackState.Playing)
        {
            playbackState = PlaybackState.Playing;

            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Starting Video at {videoPlaybackRequest.VideoURL}");

            StartPlaybackProcess(videoPlaybackRequest);

            playCount++;
        }

        return CreateStatus();

    }

    protected override void StartPlaybackTimer(VideoPlaybackRequest videoPlaybackRequest)
    {
        // Pauses Video
        if (!videoProcess.HasExited)
            videoProcess.StandardInput.WriteLine($"NOTIFY:{videoPlaybackRequest.StatusUpdateInMs}");

    }

    private void StartPlaybackProcess(VideoPlaybackRequest videoPlaybackRequest)
    { 
        string args = string.Empty;
        if (SystemInfo.CurrentPlatform == PlatformFamily.OpenViewLinux)
        {
            args = $" ! decodebin ! videoconvert ! videoscale ! waylandsink fullscreen=true";
            args = $"filesrc location={videoPlaybackRequest.VideoURL} {args}";

        }
        else
        {
            args = $"playbin uri=file://{videoPlaybackRequest.VideoURL} video-sink=waylandsink";
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"PLAYER {this.PlayerID} SETUP: {args}");

        videoProcess = ProcessUtility.CreateProcess(videoPlayerPath, args, null);
        videoProcess.StartInfo.RedirectStandardInput = true;
        videoProcess.Start();

        videoProcess.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            if (e.Data != null && !HandleMessage(e.Data))
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"PLAYER {this.PlayerID}: " + e.Data);
        });

        videoProcess.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, e.Data);
        });
               
        videoProcess.BeginOutputReadLine();
        videoProcess.BeginErrorReadLine();        

        var task = Task.Run(() =>
        {
            if (videoProcess == null)
            {
                videoProcess.WaitForExit();
                playbackState = PlaybackState.Stopped;
            }
        });

    }

    private bool HandleMessage(string data)
    {
        if (data.Trim().StartsWith("DURATION:"))
        {
            string timeString = data.Substring(data.IndexOf(":") + 2);
            var duration = TimeOnly.Parse(timeString);
            totalMs = (ulong)(duration - new TimeOnly()).TotalMilliseconds;
        
            // Send Notification on Each Duration Message (Comes After Position)
            var currentVideoStatus = OnGetVideoStatus();
            VideoStatusChanged?.Invoke(currentVideoStatus);
            return true;
        }
        else if (data.Trim().StartsWith("POSITION:"))
        {
            string timeString = data.Substring(data.IndexOf(":") + 2);
            var duration = TimeOnly.Parse(timeString);
            positionMs = (ulong)(duration - new TimeOnly()).TotalMilliseconds;

            return true;
        }
        else if (data.Trim().StartsWith("END"))
        {
            if (playbackState != PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Stopped;

                var currentVideoStatus = OnGetVideoStatus();
                VideoStatusChanged?.Invoke(currentVideoStatus);
            }

        }

        return false;
    }


    protected override VideoStatus OnPauseVideo()
    {
        // Advance To End
        playbackState = PlaybackState.Paused;

        // Pauses Video
        if (!videoProcess.HasExited)
            videoProcess.StandardInput.WriteLine("PAUSE");

        return CreateStatus();
    }

    protected override VideoStatus OnResumeVideo()
    {
        // Advance To End
        playbackState = PlaybackState.Playing;

        // Pauses Video
        if (!videoProcess.HasExited)
            videoProcess.StandardInput.WriteLine("PLAY");

        return CreateStatus();
    }

    protected override VideoStatus OnPositionVideo(VideoPosition position)
    {
        // Pauses Video
        if (!videoProcess.HasExited)
            videoProcess.StandardInput.WriteLine($"POSITION:{position.PositionInMs}");

        return CreateStatus();
    }

    protected override VideoStatus OnStopVideo()
    {
        playbackState = PlaybackState.Stopped;

        // Pauses Video
        if (!videoProcess.HasExited)
            videoProcess.StandardInput.WriteLine("QUIT");

        return CreateStatus();
    }

    protected override VideoStatus OnGetVideoStatus()
    {
        return CreateStatus();
    }

    private VideoStatus CreateStatus()
    {
        return new VideoStatus()
        {
            VideoState = playbackState,
            TotalTimeInMs = (int)totalMs,
            PositionInMs = (int)positionMs,
            VideoPlayerID = this.PlayerID
        };
    }
}

  