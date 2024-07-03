using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ahsoka.Installer.UI.InstallSystem;

internal class CompletedInstallState : InstallerUIStateBase
{
    const int imageSize = 64;
    string text;
    readonly Services.Install.InstallCompleteInfo info;
    int count = 5;

    public CompletedInstallState(Services.Install.InstallCompleteInfo info)
    {
        this.info = info;
    }

    protected override void OnDrawState(IDrawingApi drawingApi, int width, int height)
    {
        DrawingImage installImage;

        if (info.Result == Services.Install.InstallResult.Success)
            installImage = drawingApi.CreateImage("Assets/ResultSuccess.png");
        else
            installImage = drawingApi.CreateImage("Assets/FolderError.png");

        DrawingRect installImagePosition = new(width / 2 - installImage.Width / 2,
            height / 2 - installImage.Height / 2,
            installImage.Width,
            installImage.Height);

        drawingApi.DrawBitmap(installImage, installImagePosition);

        installImage.Dispose();

        var textInfo = new DrawingTextInfo
        {
            Color = new DrawingColor(0xFF111111),
            TextSize = 40,
            Alignment = DrawingTextAlignment.Center,
            Typeface = InstallerDrawingManager.Default.UITypeface
        };

        string resultText = Regex.Replace(info.Result.ToString(), "([a-z])([A-Z])", "$1 $2");
        drawingApi.DrawText(resultText, width / 2, height / 2 - imageSize / 2 - 70, textInfo);

        text = $"Exiting in {count} seconds";
        textInfo.TextSize = 30;
        drawingApi.DrawText(text, width / 2, height / 2 - imageSize / 2 - 30, textInfo);

        textInfo.TextSize = 24;
        textInfo.Color = DrawingColors.White;
        int result = (int)info.Result;

        drawingApi.DrawText(result.ToString(), installImagePosition.X + 92, installImagePosition.Y + 30, textInfo);

        Task.Delay(1000).Wait();

        if (count > 0)
        {
            InstallerDrawingManager.Default.Refresh();
            count--;
        }
        else
        {
            InstallerDrawingManager.Default.SystemClient.RequestServiceShutdown(Services.System.ShutdownTypes.Reboot);
        }

    }

    protected override InstallerUIStateBase OnHandleAction(string action)
    {
        if (action == InstallerDrawingManager.RefreshAction)
        {
            return this;
        }

        return this;
    }
}
