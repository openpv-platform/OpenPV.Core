using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.CustomerTools.Views;
public partial class SparkProjectEditView : UserControl
{
    public SparkProjectEditView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}