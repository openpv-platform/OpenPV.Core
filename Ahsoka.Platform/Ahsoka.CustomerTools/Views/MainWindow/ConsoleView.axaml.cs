using Ahsoka.CustomerTools.ViewModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace Ahsoka.CustomerTools.Views;
public partial class ConsoleView : UserControl
{
    public ConsoleView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnDataContextEndUpdate()
    {
        base.OnDataContextEndUpdate();
        if (this.DataContext is ConsoleViewModel vm)
            vm.PropertyChanged += Vm_PropertyChanged;
    }

    private async void Vm_PropertyChanged(object sender, global::System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ConsoleOutput")
        {
            await Task.Delay(100).ContinueWith(t =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.Get<TextBox>("Scroller").CaretIndex = int.MaxValue;
                });
            });
        }
    }

}
