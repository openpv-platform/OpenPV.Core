using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.CustomerTools.Views;

public partial class SparkProjectMemberListView : UserControl
{
    public SparkProjectMemberListView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
