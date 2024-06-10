using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.CustomerTools.Views;

public partial class NewItemView : UserControl
{
    public NewItemView()
    {
        InitializeComponent();

        var hostTb = this.FindControl<TextBox>("ItemText");
        if (hostTb != null)
        {
            hostTb.AttachedToVisualTree += (s, e) => hostTb.Focus();
            hostTb.KeyDown += (s, e) =>
            {

                NewItemViewModel model = this.DataContext as NewItemViewModel;
                if (e.Key is Avalonia.Input.Key.Return or Avalonia.Input.Key.Enter)
                {
                    model.ItemText = hostTb.Text;
                    model?.Accept();
                }
                else if (e.Key == Avalonia.Input.Key.Escape)
                    model?.Cancel();
            };
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
