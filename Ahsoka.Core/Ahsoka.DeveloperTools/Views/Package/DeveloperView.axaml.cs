using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace Ahsoka.DeveloperTools.Views;
public partial class DeveloperView : UserControl
{
    public DeveloperView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
