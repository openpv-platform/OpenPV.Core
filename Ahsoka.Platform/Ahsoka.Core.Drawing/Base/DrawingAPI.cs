using Ahsoka.Core.Drawing.Base;

namespace Ahsoka.Core.Drawing;

public interface IDrawingManager
{
    void PrepareDrawingApi();
    void Init();
    void ShowAndRun();
}

public interface IDrawingInfo
{
    int ScreenWidth { get; }
    int ScreenHeight { get; }
}

public interface IDrawingApi : IDrawingInfo
{
    #region DrawingAPI 
    T GetNativeApi<T>() where T : class;

    // DrawingFunctions
    void Clear(DrawingColor color);
    void DrawRectangle(DrawingRect rectangle, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth);
    void DrawRoundRectangle(DrawingRect rectangle, float cornerRadius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth);
    void DrawCircle(float centerX, float centerY, float radius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth);
    void DrawBitmap(DrawingImage image, DrawingRect rect);
    void DrawText(string text, float x, float y, DrawingTextInfo textInfo);
    float MeasureText(string text, DrawingTextInfo textInfo);

    // Constructors
    DrawingImage CreateImage(string fileName);
    DrawingImage CreateImage(byte[] data);
    DrawingTypeface LoadTypeface(string fileName, string typefaceName);
    void SetClip(DrawingRect drawingRect);
    void ClearClip();
    #endregion
}

public abstract class DrawingApiBase<Context> : IDrawingApi where Context : class
{
    public Context CurrentContext { get; private set; }

    public int ScreenWidth { get; protected set; }
    public int ScreenHeight { get; protected set; }

    public T GetNativeApi<T>() where T : class
    {
        return this as T;
    }

    public void StartFrame(Context context)
    {
        CurrentContext = context;
        OnStartFrame(CurrentContext);
    }

    public void EndFrame()
    {
        OnEndFrame();
    }

    protected abstract void OnStartFrame(Context context);
    protected abstract void OnEndFrame();

    public abstract void SetClip(DrawingRect rect);
    public abstract void ClearClip();

    public abstract void Clear(DrawingColor color);
    public abstract DrawingImage CreateImage(string fileName);
    public abstract DrawingImage CreateImage(byte[] data);
    public abstract DrawingTypeface LoadTypeface(string fileName, string typefaceName);

    public abstract float MeasureText(string text, DrawingTextInfo textInfo);

    public abstract void DrawBitmap(DrawingImage image, DrawingRect rect);
    public abstract void DrawCircle(float centerX, float centerY, float radius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth);
    public abstract void DrawRectangle(DrawingRect rectangle, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth);
    public abstract void DrawRoundRectangle(DrawingRect rectangle, float cornerRadius, DrawingColor strokeColor, DrawingColor fillColor, int strokeWidth);
    public abstract void DrawText(string text, float x, float y, DrawingTextInfo textInfo);
}