using Ahsoka.ServiceFramework;

using System.Threading.Tasks;

namespace Ahsoka.Services.Video;

internal class LinuxVideoServiceImplementation : VideoPlayerImpl
{
    static GLib.MainLoop Loop;
    static int playCount = 0;
    static object syncRoot = new object();
    Gst.Element videoElement;

    PlaybackState playbackState =  PlaybackState.Ready;
    ulong totalMs = 0;
    ulong positionMs = 0;

    public LinuxVideoServiceImplementation()
    {

    }

    protected override VideoStatus OnStartVideo(VideoPlaybackRequest videoPlaybackRequest)
    {
        // Handle Play Video
        if (playbackState != PlaybackState.Playing)
        {
            playbackState = PlaybackState.Playing;

            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Starting Video at {videoPlaybackRequest.VideoURL}");

            if (Loop == null)
            {
                Loop = new GLib.MainLoop();
                Gst.Application.Init();
            }

            string args;

            if (videoPlaybackRequest.PlayFullScreen)
                args = $" ! decodebin ! videoconvert ! videoscale  ! waylandsink display=wayland-1 fullscreen=true";
            else
                args = $" ! decodebin ! videoconvert ! videoscale  ! video/x-raw ,width={videoPlaybackRequest.VideoWidth},height={videoPlaybackRequest.VideoHeight} ! waylandsink display=wayland-1";


            videoElement = Gst.Parse.Launch($"playbin uri={videoPlaybackRequest.VideoURL} {args}");
            videoElement.Bus.AddSignalWatch();
            videoElement.Bus.Message += Handle;
            videoElement.SetState(Gst.State.Playing);

            // Start Run Loop for Video
            if (playCount == 0)
                StartRunLoop();

            playCount++;
        }
        
        return CreateStatus();
        
    }

    protected override VideoStatus OnPauseVideo()
    {
        // Advance To End
        lock (syncRoot)
        {
            playbackState = PlaybackState.Paused;
            videoElement.SetState(Gst.State.Paused);
        }

        return CreateStatus();
    }

    protected override VideoStatus OnResumeVideo()
    {
        // Advance To End
        lock (syncRoot)
        {
            playbackState = PlaybackState.Playing;
            videoElement.SetState(Gst.State.Playing);
        }

        return CreateStatus();
    }

    protected override VideoStatus OnPositionVideo(VideoPosition position)
    {
        return base.OnPositionVideo(position);
    }

    private static void StartRunLoop()
    {
        var task = Task.Run(() =>
        {
            if (!Loop.IsRunning)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Video Loop Started");
                Loop.Run();
            }
        });
    }

    protected override VideoStatus OnStopVideo()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Stopping Playback");
        playbackState =  PlaybackState.Playing;

        // Last Stopped Video Shuts down Loop
        lock (syncRoot)
        {
            // Advance To End
            videoElement.SendEvent(Gst.Event.NewEos());

            playCount--;
            if (playCount == 0)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Video Loop Stopped");
                Loop.Quit();
                Loop = null;
            }
        }

        return CreateStatus();
    }

    protected override VideoStatus OnGetVideoStatus()
    {
        return CreateStatus();
    }

    private VideoStatus CreateStatus()
    {
        long duration = 0;

        // Protect Duration Call
        lock (syncRoot)
            videoElement?.QueryPosition(Gst.Format.Time, out duration);

        positionMs = ((ulong)duration / 1000000);

        return new VideoStatus()
        {
            VideoState = playbackState,
            TotalTimeInMs = (int)totalMs,
            PositionInMs = (int)positionMs,
            VideoPlayerID = this.PlayerID
        };
    }


    void Handle(object e, Gst.MessageArgs args)
    {
        switch (args.Message.Type)
        {
            case Gst.MessageType.StateChanged:
                Gst.State oldstate, newstate, pendingstate;
                args.Message.ParseStateChanged(out oldstate, out newstate, out pendingstate);
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "[StateChange] From " + oldstate + " to " + newstate + " pending at " + pendingstate);
                break;
            case Gst.MessageType.StreamStatus:
                Gst.Element owner;
                Gst.StreamStatusType type;
                args.Message.ParseStreamStatus(out type, out owner);
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "[StreamStatus] Type" + type + " from " + owner);
                break;
            case Gst.MessageType.DurationChanged:
                long duration;
                videoElement.QueryDuration(Gst.Format.Time, out duration);
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "[DurationChanged] New duration is " + (duration) + " seconds");
                break;
            case Gst.MessageType.ResetTime:
                ulong runningtime = args.Message.ParseResetTime();
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "[ResetTime] Running time is " + runningtime);
                break;
            case Gst.MessageType.AsyncDone:
                ulong desiredrunningtime = args.Message.ParseAsyncDone();
                totalMs = (ulong)(desiredrunningtime / 100000000000000);
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "[AsyncDone] Running time is " + desiredrunningtime);
                break;
            case Gst.MessageType.NewClock:
                Gst.Clock clock = args.Message.ParseNewClock();
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "[NewClock] " + clock);
                break;
            case Gst.MessageType.Buffering:
                int percent = args.Message.ParseBuffering();
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "[Buffering] " + percent + " % done");
                break;
            case Gst.MessageType.Tag:
                Gst.TagList list = args.Message.ParseTag();
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "[Tag] Information in scope " + list.Scope + " is " + list.ToString());
                break;
            case Gst.MessageType.Error:
                GLib.GException gerror;
                string debug;
                args.Message.ParseError(out gerror, out debug);
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, "[Error] " + gerror.Message + " debug information " + debug + ". Exiting! ");
               
                videoElement.SetState(Gst.State.Null);
                videoElement = null;

                playbackState =  PlaybackState.Stopped;
                break;
            case Gst.MessageType.Eos:
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, "[Eos] Playback has ended. Exiting!");
                playbackState = PlaybackState.Stopped;
                
                videoElement.SetState(Gst.State.Null);
                videoElement = null;

                break;
            default:
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "[Recv] " + args.Message.Type);
                break;
        }
    }
}