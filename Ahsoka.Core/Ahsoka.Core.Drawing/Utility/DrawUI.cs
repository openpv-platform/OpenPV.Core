using Ahsoka.Core.Drawing.Base;
using Ahsoka.Dispatch;

namespace Ahsoka.Core.Drawing.Utility;

/// <summary>
/// Simple Class for Drawing a Background (Checkerboard) with Animation
/// Mostly used for a background on demo applications
/// </summary>
public class DrawUI
{
    // Enabling this will enable use the DamageRectangle Algorithm 
    // to draw only changed parts of the screen.  This is helpfull when drawing complex
    // screens where only small parts change every frame...though may not help with a simple drawing demo
    // such as this.
    static bool enableDamageRectangles = true;

    // Const Colors for our Drawing
    static readonly DrawingColor backgroundColor = new(0xFF4682B4);
    static readonly DrawingColor colorFill = new(0xFFFFFFFF);
    static readonly DrawingColor colorFillTouched = new(0xFF444444);
    static readonly DrawingColor colorStroke = new(0xFF111111);

    /// <summary>
    /// Simple Example of Drawing with AhsokaDrawing
    /// </summary>
    /// <param name="args"></param>
    public void StartAndRun(Dispatcher defaultDispatcher, string title)
    {
        // Create the Ahsoka Cairo Drawing API and matching Window
        DrawingWindow window = new(OperatingSystem.IsLinux());
        SkiaDrawingApi api = window.GetSkiaApi();

        // Setup Drawing Objects and Values
        var cloudTemp = api.CreateImage("Cloud.png");

        // This is our screen bounds
        DrawingSize bounds = window.Size;

        // This is the start position of our Circle / Cloud
        DrawingRect cloudPosition = new(bounds.Width / 2.0f - cloudTemp.Width / 2.0f,
                            bounds.Height / 2.0f - cloudTemp.Height / 2.0f,
                            cloudTemp.Width, cloudTemp.Height);


        // Load our Font.  These can be used for any Text Infos your app creates
        // and disposed when no longer used.
        var typeface = api.LoadTypeface("Font.ttf", "Droid Sans");

        // Create a Text Info to describe our font / text attributes
        DrawingTextInfo textInfo = new()
        {
            Alignment = DrawingTextAlignment.Center,
            Color = backgroundColor,
            TextSize = 28,
            Typeface = typeface
        };

        // Create a Text Info to describe our font / text attributes
        DrawingTextInfo titleTextInfo = new()
        {
            Alignment = DrawingTextAlignment.Left,
            Color = DrawingColors.White,
            TextSize = 42,
            Typeface = typeface
        };

        float velocity = 8.0f;
        float angle = (float)Math.PI * 4;
        bool touched = false;

        // Create Paint Objects for Circle (we show a Gradient Color Here)
        var circlePaint = new DrawingPaint(colorStroke, 2, colorFill, DrawingColors.IndianRed,               // Colors
                                            new DrawingRect(0, 0, cloudPosition.Width, cloudPosition.Height), // Bounds of Circle (Relative to the Matrix when we Draw)
                                            DrawingPaintStyles.Gradient,                                     // Gradient
                                            (float)Math.PI / 4);                                             // Rotate 1/4 PI (45 degrees)

        // Create a simple Path (Just a square using the SVG Syntax
        var path = api.CreatePath("M20.388,10.918L32,12.118l-8.735,7.749L25.914,31.4l-9.893-6.088L6.127,31.4l2.695-11.533L0,12.118 l11.547-1.2L16.026,0.6L20.388,10.918z");

        var circlePaintTouched = new DrawingPaint(colorStroke, 2, colorFillTouched);
        var checkerPaint = new DrawingPaint(DrawingColors.Empty, 0, DrawingColors.DarkGray);

        window.PrepareFrame += (o, args) =>
        {
            // Handle Events for Frame
            foreach (var eventItem in args.Events)
            {
                if (eventItem.Event == TouchEvent.Pressed)
                    touched = true;
                else if (eventItem.Event == TouchEvent.Released)
                    touched = false;
            }

            // Push Previous Frames Cloud Position since we moved the cloud.
            // This is to tell the render pass to draw over the last frames region
            // We inflate by 4 to account for Stroke of 2 
            if (enableDamageRectangles)
                args.ClipRegions.Add(cloudPosition.GetInflatedRectangle(4, 4));

            // Calculate new Cloud Position
            float x = cloudPosition.X + velocity * (float)Math.Cos(angle * Math.PI / 180f);
            float y = cloudPosition.Y + velocity * (float)Math.Sin(angle * Math.PI / 180f);

            // Collision Detection.
            if (x < 0 || x > api.ScreenWidth - cloudPosition.Height)
                angle = 180 - angle;
            else if (y < 0 || y > api.ScreenHeight - cloudPosition.Height)
                angle = 360 - angle;

            cloudPosition = new DrawingRect(x, y, cloudPosition.Width, cloudPosition.Height);

            // Push New Cloud Position since we moved the cloud.
            // This is to tell the render pass to include this frames Rectangle in the Draw
            if (enableDamageRectangles)
                args.ClipRegions.Add(cloudPosition.GetInflatedRectangle(4, 4));

            // Enable Clipping and Drawing
            args.ShouldClip = enableDamageRectangles;
            args.ShouldRender = true;
        };

        // Handle the Draw Frame.
        window.RenderFrame += (o, args) =>
        {
            // Clear Background
            api.Clear(backgroundColor);

            // Here we Draw a Checkboard Background.  Note that we don't include this in the draw rectagles since
            // it doesn't change each frame
            for (int xPos = 0; xPos < api.ScreenWidth; xPos = xPos + 100)
                for (int yPos = 0; yPos < api.ScreenHeight; yPos = yPos + 50)
                {
                    int offset = yPos == 0 || yPos % 100 == 0 ? 50 : 0;
                    var rectangle = new DrawingRect(xPos + offset, yPos, 50, 50);
                    if (!args.ShouldClip || args.ClipRegions.IntersectsRectangle(rectangle))
                        api.DrawRectangle(rectangle, checkerPaint);
                }

            // Translate to our Star
            api.PushMatrix(api.CreateTranslateMatrix(60, 60));
            api.DrawPath(path, circlePaint);
            api.DrawText(title, 45, titleTextInfo.TextSize - 12, titleTextInfo);

            api.PopMatrix();

            // Translate to our Circle Position
            api.PushMatrix(api.CreateTranslateMatrix(cloudPosition.X, cloudPosition.Y));

            // Draw Our Circle
            api.DrawCircle(cloudPosition.Width / 2, cloudPosition.Height / 2, cloudPosition.Width / 2, touched ? circlePaintTouched : circlePaint);

            // Draw the Cloud Icon at its selected Position
            api.DrawBitmap(cloudTemp, new DrawingRect(0, 0, cloudPosition.Width, cloudPosition.Height));

            // Draw the current time in our circle
            api.DrawText(DateTime.Now.ToString("hh:mm tt"), cloudPosition.Width / 2, cloudPosition.Height / 2 + 32, textInfo);

            api.PopMatrix();
        };

        // Start the Dispatcher and Use Our Windows Invoker
        defaultDispatcher.StartAndRun(window.Invoke);

        // Now start the Main Drawing Loop 
        // we will block here until the app shuts down.
        window.ShowAndRun();
    }
}
