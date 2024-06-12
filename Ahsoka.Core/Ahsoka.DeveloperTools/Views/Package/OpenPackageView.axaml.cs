using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.DeveloperTools.Views;

public partial class OpenPackageView : UserControl
{
    public OpenPackageView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        this.FindControl<Control>("FileList").DoubleTapped += OpenPackageView_DoubleTapped;
        this.FindControl<DataGrid>("FileList").CellPointerPressed += OpenPackageView_CellPointerPressed; ;

    }

    private void OpenPackageView_CellPointerPressed(object sender, DataGridCellPointerPressedEventArgs e)
    {
        MainViewModel model = this.DataContext as MainViewModel;
        if (e.Column.DisplayIndex == 0)
        {
            model?.FavoriteChanged(e.Row.DataContext as RecentItem);
        }
        else if (e.Column.DisplayIndex == 4)
        {
            model?.RemoveItem(e.Row.DataContext as RecentItem);
        }


    }

    private void OpenPackageView_DoubleTapped(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        MainViewModel model = this.DataContext as MainViewModel;
        model?.OpenExisting();
    }


}
