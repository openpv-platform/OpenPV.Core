using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace Ahsoka.DeveloperTools.Views;

public partial class LicensingView : UserControl
{
    public LicensingView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
