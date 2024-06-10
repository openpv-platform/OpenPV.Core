using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace Ahsoka.CustomerTools.Views;

public partial class PackageInfoView : UserControl
{
    public PackageInfoView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
