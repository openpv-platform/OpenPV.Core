using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.CustomerTools.Views;
public partial class PluginProfileItem : UserControl
{
    public PluginProfileItem()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
