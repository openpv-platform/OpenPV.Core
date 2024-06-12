using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace Ahsoka.DeveloperTools.Views;

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
