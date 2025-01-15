using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Install;
using System;
using System.Threading.Tasks;

namespace Ahsoka.Installer.UI.InstallSystem;

internal class InstallingState : InstallerUIStateBase, IInstallerUI
{
    #region Fields
    const int imageSize = 64;
    bool keyboardShown;
    const int completeBarSizeInPixels = 350;
    int progress = 0;
    string inputPassword;
    readonly string path;
    int passwordAttempts = -1;

    InstallCompleteInfo info;
    #endregion

    #region Prpoerties
    private string Password
    {
        get
        {
            return inputPassword;
        }
        set
        {
            inputPassword = value;

            if (inputPassword.Length > 16)
                inputPassword = inputPassword.Substring(0, 16);
        }
    }

    private string Message
    {
        get;
        set;
    } = String.Empty;

    private int Percent
    {
        get
        {
            return progress;
        }
        set
        {
            progress = Math.Max(0, Math.Min(value, 100));
        }
    }
    #endregion

    #region Methods
    public InstallingState(string pathToInstall)
    {
        path = pathToInstall;
        Message = "Preparing to Install...";

        // Register for Notifications
        InstallerDrawingManager.Default.InstallClient.SetInstallerUI(this);

        // Run Install
        ExecuteInstall();
    }


    protected override void OnDrawState(DrawFrameArgs args)
    {
        IDrawingApi drawingApi = args.DrawingApi;
        int width = args.Width;
        int height = args.Height;

        if (keyboardShown)
        {
            DrawPasswordScreen(drawingApi, width, height);
        }
        else
        {
            DrawInstallingFile(drawingApi, width, height);
        }
    }

    private void DrawInstallingFile(IDrawingApi drawingApi, int width, int height)
    {
        float rectHeight = 25f;
        float rectWidth = Percent / 100f * completeBarSizeInPixels;
        float left = +(width / 2) - (completeBarSizeInPixels) / 2;
        float top = height / 2 - rectHeight / 2;

        drawingApi.DrawRoundRectangle(new DrawingRect(left, top, completeBarSizeInPixels, rectHeight), 5f, DrawingColors.DarkGray, DrawingColors.Gainsboro, 1);

        drawingApi.PushClip(new DrawingRect(left, top, rectWidth, rectHeight));
        drawingApi.DrawRoundRectangle(new DrawingRect(left, top, completeBarSizeInPixels, rectHeight), 5f, DrawingColors.Transparent, DrawingColors.DodgerBlue, 0);
        drawingApi.PopClip();

        var textPaint = new DrawingTextInfo
        {
            Color = new DrawingColor(0xFF111111),
            TextSize = 32,
            Alignment = DrawingTextAlignment.Center,
            Typeface = InstallerDrawingManager.Default.UITypeface
        };

        drawingApi.DrawText(Message, width / 2, height / 2 - imageSize / 2 - 10, textPaint);
    }

    private void DrawPasswordScreen(IDrawingApi drawingApi, int width, int height)
    {
        float padding = 10;

        float topRow = height / 2 - (float)(imageSize * 2) - (float)(padding * 1.5) + padding;
        float leftCol = (width / 2) - (float)(imageSize * 1.5) - padding;

        int index = 0;
        for (int i = 0; i < 4; i++) // Rows
            for (int j = 0; j < 3; j++) // Columns
            {
                index++;

                float x = leftCol + (imageSize * j) + (padding * j);
                float y = topRow + (imageSize * i) + (padding * i);
                DrawingRect position = new(x, y, imageSize, imageSize);

                if (i < 3)
                    InstallerDrawingManager.Default.AddAction(index.ToString(), position, index.ToString());
                else if (i == 3)
                {
                    if (j == 1)
                        InstallerDrawingManager.Default.AddAction("0".ToString(), position, "0".ToString());
                    else if (j == 2)
                    {
                        var backspaceImage = drawingApi.CreateImage("Assets/Backspace.png");
                        InstallerDrawingManager.Default.AddAction("Backspace", position, backspaceImage, DrawingColors.SkyBlue);
                    }
                }
            }

        // Create Enter Password button
        var right = drawingApi.CreateImage("Assets/ArrowRight.png");
        InstallerDrawingManager.Default.AddAction("Enter Password", InstallerDrawingManager.Default.GetActionPosition(1, right.Width), right, DrawingColors.DarkSeaGreen);

        // Create Back button
        var cancel = drawingApi.CreateImage("Assets/ArrowLeft.png");
        InstallerDrawingManager.Default.AddAction("Cancel", InstallerDrawingManager.Default.GetActionPosition(4, cancel.Width), cancel);

        var textPaint = new DrawingTextInfo
        {
            Color = new DrawingColor(0xFF111111),
            TextSize = 40,
            Alignment = DrawingTextAlignment.Center,
            Typeface = InstallerDrawingManager.Default.UITypeface
        };

        drawingApi.DrawText($"Input Password", width / 2, topRow - 65, textPaint);

        if (!String.IsNullOrEmpty(Password))
        {
            textPaint.TextSize = 30;
            drawingApi.DrawText(Password, width / 2, topRow - 25, textPaint);
        }
    }

    protected override InstallerUIStateBase OnHandleAction(string action)
    {
        if (action == "Backspace")
        {
            if (!string.IsNullOrEmpty(inputPassword))
                inputPassword = inputPassword.Remove(inputPassword.Length - 1);
        }
        else if (action == InstallerDrawingManager.InvalidateAction)
        {
            return this;
        }
        else if (action == "Complete")
        {
            return new CompletedInstallState(info);

        }
        else if (action == "Enter Password")
        {
            keyboardShown = false;
            InstallerDrawingManager.Default.Invalidate();
            ExecuteInstall();
        }
        else if (action == "Cancel")
        {
            return new UsbHostState();
        }
        else
        {
            Password += action;
        }
        return this;
    }

    private void ExecuteInstall()
    {
        Task.Run(() =>
        {
            InstallerDrawingManager.Default.InstallClient.StartServiceAndInstall(path, Password, false); // Run All Non Windows Out of Proc
        });
    }

    void IInstallerUI.InstallProgress(InstallProgressInfo info)
    {
        Percent = info.PercentComplete;

        if (!info.UpdatePercentOnly)
            Message = info.Message;

        InstallerDrawingManager.Default.Invalidate();
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, info.PercentComplete.ToString());
    }

    void IInstallerUI.Log(InstallLogInfo info)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, info.LogMessage);
    }

    void IInstallerUI.InstallComplete(InstallCompleteInfo info)
    {
        if (info.Result == InstallResult.PassKeyIncorrect && passwordAttempts < 2)
        {
            passwordAttempts++;
            Password = String.Empty;
            keyboardShown = true;

            InstallerDrawingManager.Default.Invalidate();
        }
        else
        {

            keyboardShown = false;
            this.info = info;
            InstallerDrawingManager.Default.HandleAction("Complete", out bool _);
            InstallerDrawingManager.Default.Invalidate();
        }
    }
    #endregion
}
