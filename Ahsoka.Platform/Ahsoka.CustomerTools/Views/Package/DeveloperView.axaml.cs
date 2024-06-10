using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace Ahsoka.CustomerTools.Views;
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
