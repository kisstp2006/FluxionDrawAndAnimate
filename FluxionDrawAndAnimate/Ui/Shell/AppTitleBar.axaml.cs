using Avalonia.Controls;
using Avalonia.Input;

namespace FluxionDrawAndAnimate.Ui.Shell;

public partial class AppTitleBar : UserControl
{
    public AppTitleBar()
    {
        InitializeComponent();
    }

    private void OnDragSurfacePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var window = TopLevel.GetTopLevel(this) as Window;
        window?.BeginMoveDrag(e);
    }

    private void OnDragSurfaceDoubleTapped(object? sender, TappedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window is null)
        {
            return;
        }

        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
