using Ahsoka.Core.Drawing;
using Ahsoka.Core.Drawing.Base;
using Ahsoka.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Ahsoka.Installer.UI.InstallSystem;

internal class WiFiKeyboardState : InstallerUIStateBase
{
    readonly InstallerUIStateBase returnState;
    KeyboardAction keyboardState = KeyboardAction.Lower;

    string currentString = string.Empty;

    public WiFiKeyboardState(InstallerUIStateBase returnState)
    {
        this.returnState = returnState;
    }

    protected override void OnDrawState(DrawFrameArgs args)
    {
        IDrawingApi drawingApi = args.DrawingApi;
        int width = args.Width;
        int height = args.Height;

        DrawKeyboard(drawingApi, width, height);
    }

    private void DrawKeyboard(IDrawingApi drawingApi, int width, int height)
    {
        var textInfo = new DrawingTextInfo
        {
            Color = new DrawingColor(0xFF111111),
            TextSize = 35,
            Alignment = DrawingTextAlignment.Center,
            Typeface = InstallerDrawingManager.Default.UITypeface
        };

        float topRow = DrawKeyboardButtons(drawingApi, width, height);

        drawingApi.DrawText($"Password: {currentString}_", width / 2, topRow - 40, textInfo);

        var cancel = drawingApi.CreateImage("Assets/ArrowLeft.png");
        InstallerDrawingManager.Default.AddAction("Cancel", InstallerDrawingManager.Default.GetActionPosition(4, cancel.Width), cancel);

        var right = drawingApi.CreateImage("Assets/ArrowRight.png");
        InstallerDrawingManager.Default.AddAction("Select", InstallerDrawingManager.Default.GetActionPosition(1, right.Width), right, DrawingColors.DarkSeaGreen); ;
    }

    private float DrawKeyboardButtons(IDrawingApi drawingApi, int width, int height)
    {
        KeyBoard layout = GetKeyLayout();

        float padding = 4;
        float imageSize = 56;

        if (width < 500)
            imageSize = 35;
        else if (width < 601)
            imageSize = 44;

        int maxColumns = 0;
        foreach (var item in layout.KeyRow)
            maxColumns = Math.Max(item.KeyInfo.Count, maxColumns);

        float startY = (height / 2) - (float)((imageSize + padding) * layout.KeyRow.Count) / 2;
        float startX = (width / 2) - (float)((imageSize + padding) * maxColumns) / 2;

        for (int i = 0; i < layout.KeyRow.Count; i++) // Rows
            for (int j = 0; j < layout.KeyRow[i].KeyInfo.Count; j++) // Columns
            {
                KeyInfo keyInfo = layout.KeyRow[i].KeyInfo[j];

                float x = layout.KeyRow[i].HorizontalRowOffset + startX + (imageSize * j) + (padding * j);
                float y = startY + (imageSize * i) + (padding * i);

                DrawingRect position = new(x, y, imageSize, imageSize);

                if (keyInfo.State == KeyboardAction.Lower)
                {
                    InstallerDrawingManager.Default.AddAction(keyInfo.KeyText, position, null, DrawingColors.DarkGray, "a", 8, 28);
                    InstallerDrawingManager.Default.AddAction("", position, null, DrawingColors.Transparent, "_", 5, 26);
                }
                else if (keyInfo.State == KeyboardAction.Caps)
                {
                    InstallerDrawingManager.Default.AddAction(keyInfo.KeyText, position, null, DrawingColors.DarkGray, "A", 8, 26);
                    InstallerDrawingManager.Default.AddAction("", position, null, DrawingColors.Transparent, "_", 5, 26);
                }
                else if (keyInfo.State == KeyboardAction.SymbolsAndNumbers)
                {
                    InstallerDrawingManager.Default.AddAction(keyInfo.KeyText, position, null, DrawingColors.DarkGray, "#", 8, 26);
                    InstallerDrawingManager.Default.AddAction("", position, null, DrawingColors.Transparent, "_", 5, 26);
                }
                else if (keyInfo.State == KeyboardAction.Backspace)
                {
                    var backspaceImage = drawingApi.CreateImage("Assets/Backspace.png");
                    InstallerDrawingManager.Default.AddAction(keyInfo.KeyText, position, backspaceImage, DrawingColors.SkyBlue);
                }
                else if (keyInfo.State == KeyboardAction.Space)
                {
                    var backspaceImage = drawingApi.CreateImage("Assets/Space.png");
                    InstallerDrawingManager.Default.AddAction(keyInfo.KeyText, position, backspaceImage);
                }
                else
                    InstallerDrawingManager.Default.AddAction(keyInfo.KeyText, position, keyInfo.KeyText, layout.KeyRow[i].VerticalTextOffset);
            }

        return startY;

    }

    private KeyBoard GetKeyLayout()
    {

        KeyBoard keys = new();

        if (keyboardState == KeyboardAction.Caps)
            keys = JsonUtility.Deserialize<KeyBoard>(File.ReadAllText("Assets/CapsKeyboard.json"));
        if (keyboardState == KeyboardAction.Lower)
            keys = JsonUtility.Deserialize<KeyBoard>(File.ReadAllText("Assets/LowerKeyboard.json"));
        if (keyboardState == KeyboardAction.SymbolsAndNumbers)
            keys = JsonUtility.Deserialize<KeyBoard>(File.ReadAllText("Assets/SymbolKeyboard.json"));

        return keys;
    }

    protected override InstallerUIStateBase OnHandleAction(string action)
    {
        if (action == "Cancel")
        {
            return returnState;
        }
        else if (action == "Select")
        {
            Task.Run(() =>
            {
                InstallerDrawingManager.Default.WiFiViewModel.ConnectNetwork(currentString);
            });

            return returnState;
        }
        else if (action == KeyboardAction.Caps.ToString())
        {
            keyboardState = KeyboardAction.Caps;
        }
        else if (action == KeyboardAction.Lower.ToString())
        {
            keyboardState = KeyboardAction.Lower;
        }
        else if (action == KeyboardAction.SymbolsAndNumbers.ToString())
        {
            keyboardState = KeyboardAction.SymbolsAndNumbers;
        }
        else if (action == KeyboardAction.Backspace.ToString())
        {
            if (currentString.Length > 0)
                currentString = currentString.Substring(0, currentString.Length - 1);
        }
        else if (action == KeyboardAction.Space.ToString())
        {
            currentString += " ";
        }
        else if (action != "Refresh")
        {
            currentString += action;
        }

        return this;
    }
}

public enum KeyboardAction
{
    None,
    Caps,
    Lower,
    SymbolsAndNumbers,
    Backspace,
    Space,

}

public class KeyBoard
{
    public List<KeyRow> KeyRow = new();
}

public class KeyRow
{
    public int HorizontalRowOffset;
    public int VerticalTextOffset;
    public List<KeyInfo> KeyInfo = new();
}

public class KeyInfo
{
    public string KeyText;
    public KeyboardAction State;

}

