using Avalonia;
using Avalonia.Markup.Xaml;

namespace Ahsoka.DeveloperTools;

internal class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
    }

    public override void RegisterServices()
    {
        base.RegisterServices();
    }
}
