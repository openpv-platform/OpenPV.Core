using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.DeveloperTools.Views;
public partial class PluginProfileView : UserControl
{
    public PluginProfileView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Binding(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
}
