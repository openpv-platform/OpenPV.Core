using System.Runtime.InteropServices;

namespace Ahsoka.Core.Drawing.Utility;

public static class ConsoleExtension
{
    const int SW_HIDE = 0;
    const int SW_SHOW = 5;
    readonly static nint handle = GetConsoleWindow();

    [DllImport("kernel32.dll")]
    static extern nint GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(nint hWnd, int nCmdShow);

    public static void Hide()
    {
        ShowWindow(handle, SW_HIDE); //hide the console
    }
    public static void Show()
    {
        ShowWindow(handle, SW_SHOW); //show the console
    }
}
