using Ahsoka.ServiceFramework;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace Ahsoka.DeveloperTools.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        MainViewModel dataContext = (MainViewModel)this.DataContext;
        if (dataContext.CheckForPackageChanges())
        {
            e.Cancel = true;
            dataContext.IsExit = true;
            dataContext.PromptSave();
        }
        else
            base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        AhsokaRuntime.Default.StopAllEndPoints();
    }

    private void InitializeComponent()
    {
        //this.RendererDiagnostics.DebugOverlays = Avalonia.Rendering.RendererDebugOverlays.Fps;
        this.Width = 900;
        AvaloniaXamlLoader.Load(this);
    }
}
