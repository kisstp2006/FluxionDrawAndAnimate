using Avalonia.Controls;
using Avalonia.Threading;

namespace FluxionDrawAndAnimate.Controls.Canvas;

/// <summary>
/// Coalesces hot input-path redraw requests onto the render dispatcher tick.
/// </summary>
internal sealed class CanvasInvalidationScheduler
{
    private bool _isQueued;

    public void Request(Control control)
    {
        if (_isQueued)
        {
            return;
        }

        _isQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _isQueued = false;
            control.InvalidateVisual();
        }, DispatcherPriority.Render);
    }

    public void InvalidateNow(Control control)
    {
        _isQueued = false;
        control.InvalidateVisual();
    }
}
