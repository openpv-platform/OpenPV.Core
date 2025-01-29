using Ahsoka.Utility;
using Avalonia;
using Avalonia.Svg.Skia;
using System;
using System.Threading;

namespace Ahsoka.DeveloperTools;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length > 0)
            MainViewModel.StartupArg = args[args.Length - 1];

        // Initialing Assembly Resolver
        AssemblyResolver.Init();

        AppDomain.CurrentDomain.UnhandledException += MainViewModel.ErrorOccurred;

        GC.KeepAlive(typeof(SvgImageExtension).Assembly);
        GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);
        GC.KeepAlive(typeof(SvgImage).Assembly);

        var builder = BuildAvaloniaApp();

        return builder.StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnMainWindowClose);
    }


    private static void SilenceConsole()
    {
        new Thread(() =>
        {
            Console.CursorVisible = false;
            while (true)
                Console.ReadKey(true);
        })
        { IsBackground = true }.Start();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .WithInterFont()
            .UsePlatformDetect();

}
