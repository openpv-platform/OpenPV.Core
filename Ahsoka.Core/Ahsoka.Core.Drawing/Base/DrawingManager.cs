
using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System.Reflection;

namespace Ahsoka.Core.Drawing.Base;

public abstract class DrawingManager<DrawingAPI> where DrawingAPI : class, IDrawingApi
{
    #region Fields
    DrawingTypeface defaultTypeface;

    protected readonly Dictionary<string, TouchArea> TouchAreas = new();
    private static readonly object syncRoot = new();
    private const int BorderMargin = 20;
    private const int StatusBarHeight = 26;

    private bool IsTouched = false;
    private double TouchX = 0;
    private double TouchY = 0;
    #endregion

    #region Static
    public static readonly string RefreshAction = "Refresh";
    #endregion

    #region Properties
    public int IsValidSystem { get; set; } = -1;

    public DrawingTypeface UITypeface
    {
        get
        {
            lock (syncRoot)
            {
                defaultTypeface ??= OnCreateTypeFace();

                return defaultTypeface;
            }
        }
    }

    public int ScreenHeightWithoutStatus { get => DrawingApi.ScreenHeight - StatusBarHeight; }

    protected UIStateBase<IDrawingApi> CurrentState { get; set; }

    protected DrawingAPI DrawingApi { get; set; }
    #endregion

    #region Methods
    public DrawingManager()
    {
        try
        {
            AhsokaStopwatch watch = new("DrawingManager");
            Task.Run(() => { Thread.Sleep(5000); IsValidSystem = SystemInfo.ValidatePlatform() ? 1 : 0; });
        }
        catch { }
    }

    public void Refresh()
    {
        HandleAction(RefreshAction, out bool _);
    }

    protected void RequestDrawFrame()
    {
        OnRequestDraw();
    }

    protected void DrawFrame()
    {
        //bool noClip = OperatingSystem.IsMacOS() || OperatingSystem.IsWindows();

        // Desktop Doesn't Preserve Backbuffer
        /*if (IsTouched && noClip == false) // Handle Touching a Button
        {
            // Find the Touched Rectangle...this is all we will draw
            var ItemTouched = TouchAreas.Values.FirstOrDefault(x => x.Rect.Contains((float)TouchX, (float)TouchY));
            if (ItemTouched != null)
                DrawingApi.SetClip(ItemTouched.Rect);
        }*/


        // Prepare Drawing Data
        foreach (var item in TouchAreas.Values)
            item.Bitmap?.Dispose();

        TouchAreas.Clear();

        // Draw Layers
        DrawBackground();
        CurrentState.DrawState(DrawingApi, DrawingApi.ScreenWidth, ScreenHeightWithoutStatus);
        DrawActions();

        //DrawingApi.ClearClip();
    }

    public void Close(int result = 0)
    {
        AhsokaRuntime.Default.StopAllEndPoints();

        OnClose(result);

        Environment.Exit(result);
    }

    private void DrawActions()
    {
        // Draw Action Buttons
        foreach (var action in TouchAreas)
            DrawActionButton(action.Value);
    }

    protected virtual void DrawActionButton(TouchArea action)
    {
        // Full Screen Touch;
        if (action.Rect.X == 0 &&
            action.Rect.Y == 0 &&
            action.Rect.Width == DrawingApi.ScreenWidth &&
            action.Rect.Height == DrawingApi.ScreenHeight)
            return;


        // Draw background
        DrawingColor strokeColor = new(0xFF444444);
        DrawingColor fillColor;

        if (action.Rect.Contains((int)TouchX, (int)TouchY) && IsTouched)
            fillColor = DrawingColors.DodgerBlue;
        else
            fillColor = action.Color != DrawingColor.Empty ? action.Color : DrawingColors.Gainsboro;

        DrawingApi.DrawCircle(action.Rect.MidX, action.Rect.MidY, action.Rect.Width / 2, strokeColor, fillColor, 2);

        if (action.Bitmap != null)
        {
            DrawingApi.DrawBitmap(action.Bitmap, action.Rect);
        }
        else if (!string.IsNullOrEmpty(action.Text))
        {
            var textInfo = new DrawingTextInfo()
            {
                Color = new DrawingColor(0xFF111111),
                TextSize = action.FontSize > 0 ? action.FontSize : action.Rect.Height * .666f,
                Alignment = DrawingTextAlignment.Center,
                Typeface = UITypeface
            };

            DrawingApi.DrawText(action.Text, action.Rect.MidX, -action.VerticalOffset + action.Rect.MidY + action.Rect.Height / 4, textInfo);
        }
    }

    protected virtual void DrawBackground()
    {
        int height = DrawingApi.ScreenHeight;
        int width = DrawingApi.ScreenWidth;

        // Clear the canvas
        DrawingApi.Clear(new DrawingColor(0xFFFFFFFF));

        // Draw Status Bar
        DrawingApi.DrawRectangle(new DrawingRect(-1, height - StatusBarHeight, width + 2, height), DrawingColors.Silver, DrawingColors.Gainsboro, 1);

        DrawingTextInfo textPaint;
        if (IsValidSystem == 0)
        {
            textPaint = new() { Color = DrawingColors.IndianRed, TextSize = 16, Alignment = DrawingTextAlignment.Center, Typeface = UITypeface };
            DrawingApi.DrawText($"***Warning: Invalid Factory Data***", width / 2, height - 8, textPaint);
        }
        else if (IsValidSystem == 1)
        {
            textPaint = new() { Color = DrawingColors.Black, TextSize = 16, Alignment = DrawingTextAlignment.Center, Typeface = UITypeface };
            DrawingApi.DrawText($"Passed Factory Data Check", width / 2, height - 8, textPaint);
        }

        OnDrawBackground();

        height -= StatusBarHeight;

        textPaint = new() { Color = new DrawingColor(0xFF111111), TextSize = 14, Typeface = UITypeface };

        int xPos = 20;
        int yOffset = 86;
        DrawingApi.DrawText($"Hardware: {SystemInfo.CurrentPlatform}", xPos, height - yOffset, textPaint);
        yOffset -= 18;
        DrawingApi.DrawText($"App Version: {Assembly.GetEntryAssembly().GetName().Version} ", xPos, height - yOffset, textPaint);
        yOffset -= 18;
        DrawingApi.DrawText($"Part #: {SystemInfo.HardwareInfo.FactoryInfo.PartNumber} ", xPos, height - yOffset, textPaint);
        yOffset -= 18;
        DrawingApi.DrawText($"Serial #: {SystemInfo.HardwareInfo.FactoryInfo.SerialNumber} ", xPos, height - yOffset, textPaint);
        yOffset -= 18;
        DrawingApi.DrawText($"Device ID: {SystemInfo.HardwareInfo.FactoryInfo.DeviceId} ", xPos, height - yOffset, textPaint);
    }

    #endregion

    #region Abstract
    protected abstract DrawingTypeface OnCreateTypeFace();
    protected abstract void OnRequestDraw();
    protected virtual void OnDrawBackground() { }
    protected abstract void OnInit();
    protected abstract void OnClose(int result);
    #endregion

    #region Touch Support
    protected void HandleInputEvent(TouchEventArgs args)
    {
        TouchX = args.X;
        TouchY = args.Y;

        bool handled = false;

        if (args.Event == TouchEvent.Pressed)
        {
            IsTouched = true;
        }
        else if (args.Event == TouchEvent.Released) // Handle Button Release if still in hit rectangle
        {
            // Get Flags
            foreach (var action in TouchAreas)
            {
                if (action.Value.Rect.Contains(args.X, args.Y) && IsTouched)
                {
                    HandleAction(action.Key, out handled);
                }
            }

            IsTouched = false;
        }

        CurrentState.HandleTouch(args);

        if (!handled)
            RequestDrawFrame();
    }

    public void HandleAction(string action, out bool handled)
    {
        CurrentState = CurrentState.HandleAction(action);
        handled = true;

        RequestDrawFrame();
    }

    public DrawingRect GetActionPosition(int buttonPosition, int buttonSize)
    {
        return buttonPosition switch
        {
            1 => new DrawingRect(DrawingApi.ScreenWidth - BorderMargin - buttonSize, ScreenHeightWithoutStatus - BorderMargin - buttonSize,
                                              buttonSize, buttonSize),
            2 => new DrawingRect(DrawingApi.ScreenWidth - BorderMargin - buttonSize, ScreenHeightWithoutStatus * .5f - buttonSize - BorderMargin / 2,
                                              buttonSize, buttonSize),
            3 => new DrawingRect(DrawingApi.ScreenWidth - BorderMargin - buttonSize, ScreenHeightWithoutStatus * .5f + BorderMargin / 2,
                                              buttonSize, buttonSize),
            4 => new DrawingRect(BorderMargin, BorderMargin,
                                              buttonSize, buttonSize),
            5 => new DrawingRect(DrawingApi.ScreenWidth - BorderMargin - buttonSize, BorderMargin,
                                              buttonSize, buttonSize),
            _ => DrawingRect.Empty,
        };
    }

    public void AddAction(string buttonAction, DrawingRect position, DrawingImage bitmap)
    {
        AddAction(buttonAction, position, bitmap, DrawingColor.Empty);
    }

    public void AddAction(string buttonAction, DrawingRect position, string text, int verticalOffset = 0, int overrideFontSize = 0)
    {
        AddAction(buttonAction, position, null, DrawingColor.Empty, text, verticalOffset, overrideFontSize);
    }

    public void AddAction(string buttonAction, DrawingRect position, DrawingImage bitmap, DrawingColor color, string text = null, int verticalOffset = 0, int overrideFontSize = 0)
    {
        TouchAreas.Add(buttonAction, new TouchArea()
        {
            Text = text,
            Bitmap = bitmap,
            Rect = position,
            Color = color,
            VerticalOffset = verticalOffset,
            FontSize = overrideFontSize
        });
    }
    #endregion

    #region TouchArea
    public class TouchArea
    {
        public DrawingRect Rect;
        public DrawingImage Bitmap;
        public string Text;
        public DrawingColor Color = DrawingColor.Empty;

        public int VerticalOffset;
        public int FontSize;
    }
    #endregion
}

public enum TouchEvent
{
    Pressed = 0,
    Released = 1,
    Continue = 2,
}

public class TouchEventArgs
{
    public TouchEvent Event { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Index { get; set; }
    public bool Handled { get; set; }
}
