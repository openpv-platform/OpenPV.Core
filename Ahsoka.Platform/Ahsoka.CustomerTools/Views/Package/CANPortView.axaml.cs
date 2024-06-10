using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.CustomerTools.Views;
public partial class CANPortView : UserControl
{
    public CANPortView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
