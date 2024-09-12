using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace Ahsoka.DeveloperTools.Views;

public partial class DeviceView : UserControl
{
    public DeviceView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
