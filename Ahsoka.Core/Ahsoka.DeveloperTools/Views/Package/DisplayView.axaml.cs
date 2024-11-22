using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace Ahsoka.DeveloperTools.Views;

public partial class DisplayView : UserControl
{
    public DisplayView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
