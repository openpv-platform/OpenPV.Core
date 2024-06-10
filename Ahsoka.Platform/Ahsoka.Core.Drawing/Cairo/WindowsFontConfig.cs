using System.Drawing.Text;

namespace Ahsoka.Core.Drawing.Cairo;

internal static class WindowsFontConfig
{
    static readonly PrivateFontCollection appConfig;
    static WindowsFontConfig()
    {
        appConfig = new PrivateFontCollection();
    }

    public static bool AddFont(string fileName)
    {
        try
        {
            appConfig.AddFontFile(fileName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static PrivateFontCollection GetFontSet()
    {
        return appConfig;
    }
}