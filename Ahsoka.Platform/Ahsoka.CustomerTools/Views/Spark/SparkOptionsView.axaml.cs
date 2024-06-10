using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.CustomerTools.Views;
public partial class SparkOptionsView : UserControl
{
    public SparkOptionsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
