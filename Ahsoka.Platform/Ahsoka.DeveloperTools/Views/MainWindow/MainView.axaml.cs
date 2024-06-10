using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.DeveloperTools.Views;

public partial class MainView : UserControl
{
    MainViewModel viewModel;

    public MainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (e.Root is MainWindow window)
        {
            this.viewModel = new MainViewModel(TopLevel.GetTopLevel(e.Root as Visual));
            this.DataContext = viewModel;
            window.DataContext = this.DataContext;
        }
    }
}
