using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using System.Threading.Tasks;

namespace Ahsoka.Installer.UI.InstallSystem;

internal class WiFiSetupState : InstallerUIStateBase
{
    const string refreshAction = "RefreshWiFiList";
    const string alreadyConnected = "AleadyConnected";
    bool isConnecting = false;
    bool isAutoConnecting = false;

    public WiFiSetupState()
    {

    }

    protected override void OnDrawState(DrawFrameArgs args)
    {
        IDrawingApi drawingApi = args.DrawingApi;
        int width = args.Width;
        int height = args.Height;

        if (InstallerDrawingManager.Default.WiFiViewModel.ConnectionState == Services.Network.WirelessConnectionState.ClientConnected)
        {
            // Draw Waiting for TimeSync
            DrawConnectedToNetwork(drawingApi, width, height);
        }
        else if (isConnecting)
        {
            DrawConnectingToNetworks(drawingApi, width, height);
        }
        else if (InstallerDrawingManager.Default.WiFiViewModel.AvailableNetworks.Count == 0)
        {
            DrawWaitingForNetworks(drawingApi, width, height);
        }
        else
        {
            DrawSelectNetwork(drawingApi, width, height);
        }
    }

    protected override InstallerUIStateBase OnHandleAction(string action)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "WiFi - " + action);

        if (action == "Up")
            InstallerDrawingManager.Default.WiFiViewModel.MoveUp();
        else if (action == "Down")
            InstallerDrawingManager.Default.WiFiViewModel.MoveDown();
        else if (action == "Cancel")
            return new StartState();
        else if (action == "Spark")
            return new SparkConnectionState();
        else if (action == "AutoConnect")
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Attempting AutoConnect");
            isConnecting = true;
            isAutoConnecting = true;
            Task.Run(() =>
            {
                try { InstallerDrawingManager.Default.WiFiViewModel.AttemptAutoConnect(); }
                finally { isAutoConnecting = false; }
            });
            return this;
        }
        else if (action == "Return")
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Exiting WiFi");
            isConnecting = false;
            isAutoConnecting = false;
            return new StartState();
        }
        else if (action == refreshAction)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Discovering Networks");
            InstallerDrawingManager.Default.WiFiViewModel.DiscoverNetworks();
        }

        else if (action == "Select")
        {
            // No Profile so Open Keyboard and allow it to issue the connect.
            if (InstallerDrawingManager.Default.WiFiViewModel.SelectedNetworkProfile == null)
            {
                isConnecting = true;
                return new WiFiKeyboardState(this);
            }
            else
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "Attemping to Connect");
                isConnecting = true;
                Task.Run(() =>
                {
                    InstallerDrawingManager.Default.WiFiViewModel.ConnectNetwork();
                });
            }
        }

        return this;
    }

    private void DrawConnectingToNetworks(IDrawingApi drawingApi, int width, int height)
    {
        var textInfo = new DrawingTextInfo
        {
            Color = new DrawingColor(0xFF111111),
            TextSize = 24,
            Alignment = DrawingTextAlignment.Center,
            Typeface = InstallerDrawingManager.Default.UITypeface
        };

        drawingApi.DrawText($"{(isAutoConnecting ? "Auto " : "")}Connecting to {InstallerDrawingManager.Default.WiFiViewModel.SelectedNetwork.Ssid}", width / 2, height / 2, textInfo);


        textInfo.TextSize = 20;

        string state = InstallerDrawingManager.Default.WiFiViewModel.ConnectionState.ToString().SplitCamelCase();

        drawingApi.DrawText($"{state}", width / 2, height * .5f + 30, textInfo);

        var cancel = drawingApi.CreateImage("Assets/ArrowLeft.png");
        InstallerDrawingManager.Default.AddAction("Cancel", InstallerDrawingManager.Default.GetActionPosition(4, cancel.Width), cancel);
    }

    private void DrawConnectedToNetwork(IDrawingApi drawingApi, int width, int height)
    {
        var textInfo = new DrawingTextInfo
        {
            Color = new DrawingColor(0xFF111111),
            TextSize = 28,
            Alignment = DrawingTextAlignment.Center,
            Typeface = InstallerDrawingManager.Default.UITypeface
        };

        drawingApi.DrawText($"Connected To Network: {InstallerDrawingManager.Default.WiFiViewModel.ConnectedNetwork?.Ssid}", width / 2, height / 2 - 20, textInfo);
        textInfo.TextSize = 18;

        var cancel = drawingApi.CreateImage("Assets/ArrowLeft.png");
        InstallerDrawingManager.Default.AddAction("Return", InstallerDrawingManager.Default.GetActionPosition(4, cancel.Width), cancel);

        drawingApi.DrawText($"Connected - Select Continue For Spark.", width / 2, height / 2 + 10, textInfo);

        var right = drawingApi.CreateImage("Assets/ArrowRight.png");
        InstallerDrawingManager.Default.AddAction("Spark", InstallerDrawingManager.Default.GetActionPosition(1, right.Width), right, DrawingColors.DarkSeaGreen); ;

    }

    private void DrawWaitingForNetworks(IDrawingApi drawingApi, int width, int height)
    {
        var textPaint = new DrawingTextInfo
        {
            Color = new DrawingColor(0xFF111111),
            TextSize = 24,
            Alignment = DrawingTextAlignment.Center,
            Typeface = InstallerDrawingManager.Default.UITypeface
        };

        drawingApi.DrawText($"Searching for Networks", width / 2, height / 2, textPaint);

        var refresh = drawingApi.CreateImage("Assets/Refresh.png");
        InstallerDrawingManager.Default.AddAction(refreshAction, InstallerDrawingManager.Default.GetActionPosition(5, refresh.Width), refresh); ;

        var cancel = drawingApi.CreateImage("Assets/ArrowLeft.png");
        InstallerDrawingManager.Default.AddAction("Cancel", InstallerDrawingManager.Default.GetActionPosition(4, cancel.Width), cancel);
    }

    private void DrawSelectNetwork(IDrawingApi drawingApi, int width, int height)
    {
        var textPaint = new DrawingTextInfo
        {
            Color = new DrawingColor(0xFF111111),
            TextSize = 35,
            Alignment = DrawingTextAlignment.Center,
            Typeface = InstallerDrawingManager.Default.UITypeface
        };

        drawingApi.DrawText($"Select Network", width / 2, height * .5f - 10, textPaint);

        textPaint.TextSize = 20;
        textPaint.Alignment = DrawingTextAlignment.Left;

        // Calculate Start Position
        var textWidth = drawingApi.MeasureText($"{InstallerDrawingManager.Default.WiFiViewModel.SelectedNetwork.Ssid}", textPaint);

        bool hasProfile = InstallerDrawingManager.Default.WiFiViewModel.SelectedNetworkProfile != null;
        int offset = hasProfile ? 16 + 16 : 16; // Half of 1 Icon or Two

        // First Icon Position
        DrawingRect signalPosition = new((width / 2 - textWidth / 2) - offset, height * .5f + 6, 32, 32);

        // Draw Saved Icon
        if (InstallerDrawingManager.Default.WiFiViewModel.SelectedNetworkProfile != null)
        {
            using var saved = drawingApi.CreateImage("Assets/Saved.png");
            drawingApi.DrawBitmap(saved, signalPosition);
            signalPosition.Offset(40, 0);
        }

        // Draw Signal State
        DrawingImage signal;
        if (InstallerDrawingManager.Default.WiFiViewModel.SelectedNetwork.SignalLevel > 66)
            signal = drawingApi.CreateImage("Assets/WiFiSignal100.png");
        else if (InstallerDrawingManager.Default.WiFiViewModel.SelectedNetwork.SignalLevel > 33)
            signal = drawingApi.CreateImage("Assets/WiFiSignal50.png");
        else
            signal = drawingApi.CreateImage("Assets/WiFiSignal33.png");


        drawingApi.DrawBitmap(signal, signalPosition);

        signal.Dispose();

        signalPosition.Offset(40, 0);

        drawingApi.DrawText($"{InstallerDrawingManager.Default.WiFiViewModel.SelectedNetwork.Ssid}", signalPosition.X, height * .5f + 30, textPaint);

        var refresh = drawingApi.CreateImage("Assets/Refresh.png");
        InstallerDrawingManager.Default.AddAction(refreshAction, InstallerDrawingManager.Default.GetActionPosition(5, refresh.Width), refresh); ;

        var cancel = drawingApi.CreateImage("Assets/ArrowLeft.png");
        InstallerDrawingManager.Default.AddAction("Cancel", InstallerDrawingManager.Default.GetActionPosition(4, cancel.Width), cancel);

        var down = drawingApi.CreateImage("Assets/ArrowDown.png");
        InstallerDrawingManager.Default.AddAction("Down", InstallerDrawingManager.Default.GetActionPosition(3, down.Width), down);

        var up = drawingApi.CreateImage("Assets/ArrowUp.png");
        InstallerDrawingManager.Default.AddAction("Up", InstallerDrawingManager.Default.GetActionPosition(2, up.Width), up);

        var right = drawingApi.CreateImage("Assets/ArrowRight.png");
        InstallerDrawingManager.Default.AddAction("Select", InstallerDrawingManager.Default.GetActionPosition(1, right.Width), right, DrawingColors.DarkSeaGreen); ;
    }
}
