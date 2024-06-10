using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.CustomerTools.Views;
public partial class NewPackageView : UserControl
{
    public NewPackageView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<Control>("FileList").DoubleTapped += OpenPackageView_DoubleTapped;
    }

    private void OpenPackageView_DoubleTapped(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        NewPackageViewModel model = this.DataContext as NewPackageViewModel;
        model?.Next();
    }
}
