using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Editing;
using FluxionDrawAndAnimate.Rendering;

namespace FluxionDrawAndAnimate.Controls.Canvas;

/// <summary>
/// Owns the hot input path for the drawing canvas: shortcuts, panning, gestures
/// and active stroke collection. The visual control stays responsible for render.
/// </summary>
internal sealed class CanvasInputController
{
    private const double TouchPanDeadzone = 10.0;
    private const double TouchZoomDeadzone = 5.0;

    private static readonly ToolKind[] NonPaintingTools =
    [
        ToolKind.Select, ToolKind.Lasso, ToolKind.Move, ToolKind.Rotate,
        ToolKind.Text, ToolKind.Fill, ToolKind.Shape,
        ToolKind.Eyedropper, ToolKind.Ruler, ToolKind.Symmetry
    ];

    private readonly Control _target;
    private readonly ViewportState _viewport;
    private readonly RasterFrameCache _rasterCache;
    private readonly StrokeProcessor _strokeProcessor = new();
    private readonly GestureStateMachine _gestures = new(TouchPanDeadzone, TouchZoomDeadzone);
    private readonly StrokeSession _strokeSession = new();
    private readonly Func<CanvasInputContext> _getContext;
    private readonly Func<Size> _getViewportSize;
    private readonly Action _requestInvalidate;
    private readonly Action _notifyScale;
    private bool _isPanning;
    private Point _panLastPos;
    private bool _spaceDown;

    public CanvasInputController(
        Control target,
        ViewportState viewport,
        RasterFrameCache rasterCache,
        Func<CanvasInputContext> getContext,
        Func<Size> getViewportSize,
        Action requestInvalidate,
        Action notifyScale)
    {
        _target = target;
        _viewport = viewport;
        _rasterCache = rasterCache;
        _getContext = getContext;
        _getViewportSize = getViewportSize;
        _requestInvalidate = requestInvalidate;
        _notifyScale = notifyScale;
    }

    public void OnKeyDown(KeyEventArgs e)
    {
        if (IsFitShortcut(e))
        {
            FitToView();
            e.Handled = true;
            return;
        }

        if (IsResetShortcut(e))
        {
            _viewport.Reset(_getViewportSize());
            _notifyScale();
            _requestInvalidate();
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Space:
                if (!_spaceDown)
                {
                    _spaceDown = true;
                    _target.Cursor = new Cursor(StandardCursorType.SizeAll);
                }
                break;
            case Key.OemPlus when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                ZoomAt(new Point(_target.Bounds.Width / 2, _target.Bounds.Height / 2), 1.25);
                e.Handled = true;
                break;
            case Key.OemMinus when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                ZoomAt(new Point(_target.Bounds.Width / 2, _target.Bounds.Height / 2), 0.8);
                e.Handled = true;
                break;
        }
    }

    public void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key != Key.Space)
        {
            return;
        }

        _spaceDown = false;
        _isPanning = false;
        _target.Cursor = Cursor.Default;
    }

    public void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        var pivot = e.GetPosition(_target);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            _viewport.RotateBy(e.Delta.Y > 0 ? -0.05 : 0.05);
            _requestInvalidate();
            e.Handled = true;
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.Delta.X == 0)
        {
            var factor = e.Delta.Y > 0 ? 1.12 : 1.0 / 1.12;
            ZoomAt(pivot, factor);
            e.Handled = true;
        }
        else
        {
            _viewport.PanBy(-e.Delta.X * 20, -e.Delta.Y * 20);
            _requestInvalidate();
            e.Handled = true;
        }
    }

    public void OnPointerPressed(PointerPressedEventArgs e)
    {
        _target.Focus();

        var pos = e.GetPosition(_target);
        var props = e.GetCurrentPoint(_target).Properties;
        var context = _getContext();
        var project = context.Project;

        if (e.Pointer.Type == PointerType.Touch)
        {
            if (project is not null && _gestures.Press(e.Pointer.Id, pos))
            {
                CancelActiveStroke();
                e.Pointer.Capture(_target);
                e.Handled = true;
                return;
            }

            e.Pointer.Capture(_target);
        }

        if (props.IsMiddleButtonPressed || (props.IsLeftButtonPressed && _spaceDown))
        {
            _isPanning = true;
            _panLastPos = pos;
            e.Pointer.Capture(_target);
            e.Handled = true;
            return;
        }

        if (project is null || _gestures.IsGestureInProgress)
        {
            return;
        }

        if (Array.IndexOf(NonPaintingTools, context.ActiveToolKind) >= 0)
        {
            return;
        }

        if (context.ActiveLayerIndex < 0 || context.ActiveLayerIndex >= project.Layers.Count)
        {
            return;
        }

        var layer = project.Layers[context.ActiveLayerIndex];
        if (layer.IsLocked || !layer.IsVisible)
        {
            return;
        }

        if (!TryMakePaintInfo(context, e.GetCurrentPoint(_target), out var paintInfo))
        {
            return;
        }

        _strokeProcessor.Reset();
        var frame = layer.EnsureFrame(context.ActiveFrameIndex);
        var brushSettings = context.BrushSettings ?? BrushSettings.ForTool(context.ActiveToolKind);
        _strokeProcessor.Configure(brushSettings);

        var stroke = new StrokePath(
            context.BrushColor,
            context.BrushSize,
            context.ActiveToolKind,
            context.ActiveToolKind == ToolKind.Vector,
            brushSettings);
        _strokeSession.Begin(frame, context.ActiveFrameIndex, context.ActiveLayerIndex, stroke);

        foreach (var point in _strokeProcessor.AddPoint(paintInfo, context.BrushSize))
        {
            stroke.Points.Add(point);
        }

        frame.Strokes.Add(stroke);
        _rasterCache.AppendStrokeRange(context.ActiveFrameIndex, context.ActiveLayerIndex, stroke, 0);

        e.Pointer.Capture(_target);
        e.Handled = true;
        _requestInvalidate();
    }

    public void OnPointerMoved(PointerEventArgs e)
    {
        var pos = e.GetPosition(_target);
        var context = _getContext();
        var project = context.Project;

        if (e.Pointer.Type == PointerType.Touch)
        {
            if (project is not null &&
                _gestures.Move(e.Pointer.Id, pos, _viewport, _getViewportSize(), GetDocumentSize(project)))
            {
                _notifyScale();
                _requestInvalidate();
                e.Handled = true;
                return;
            }
        }

        if (_isPanning)
        {
            _viewport.PanBy(pos.X - _panLastPos.X, pos.Y - _panLastPos.Y);
            _panLastPos = pos;
            _requestInvalidate();
            e.Handled = true;
            return;
        }

        if (!_strokeSession.IsActive || project is null || _gestures.IsGestureInProgress)
        {
            return;
        }

        var redraw = false;
        foreach (var intermediate in e.GetIntermediatePoints(_target))
        {
            if (TryMakePaintInfo(context, intermediate, out var info))
            {
                redraw |= AppendToStroke(context, info);
            }
        }

        if (TryMakePaintInfo(context, e.GetCurrentPoint(_target), out var current))
        {
            redraw |= AppendToStroke(context, current);
        }

        if (redraw)
        {
            e.Handled = true;
            _requestInvalidate();
        }
    }

    public void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Touch)
        {
            var wasGesture = _gestures.IsGestureInProgress;
            _gestures.Release(e.Pointer.Id);
            if (wasGesture && !_strokeSession.IsActive)
            {
                e.Pointer.Capture(null);
                e.Handled = true;
                _requestInvalidate();
                return;
            }
        }

        if (_isPanning)
        {
            _isPanning = false;
            e.Handled = true;
            return;
        }

        if (_strokeSession.TryEnd(out var stroke, out var frame, out var frameIndex, out var layerIndex))
        {
            var smoothed = _strokeProcessor.BuildSmoothedPath();
            _rasterCache.InvalidateStroke(frameIndex, layerIndex, stroke);
            stroke.Points.Clear();
            foreach (var point in smoothed)
            {
                stroke.Points.Add(point);
            }

            _rasterCache.InvalidateStroke(frameIndex, layerIndex, stroke);
            _getContext().UndoStack?.Push(new AddStrokeCommand(frame, stroke, changed =>
            {
                _rasterCache.InvalidateStroke(frameIndex, layerIndex, changed);
                _requestInvalidate();
            }));
        }

        e.Pointer.Capture(null);
        e.Handled = true;
        _requestInvalidate();
    }

    public void FitToView()
    {
        var project = _getContext().Project;
        if (project is null || _target.Bounds.Width <= 0 || _target.Bounds.Height <= 0)
        {
            return;
        }

        _viewport.FitToView(_getViewportSize(), GetDocumentSize(project));
        _notifyScale();
        _requestInvalidate();
    }

    private void ZoomAt(Point pivot, double factor)
    {
        _viewport.ZoomAt(_getViewportSize(), pivot, factor);
        _notifyScale();
        _requestInvalidate();
    }

    private bool AppendToStroke(CanvasInputContext context, PaintInformation info)
    {
        var stroke = _strokeSession.Stroke;
        if (stroke is null)
        {
            return false;
        }

        var emitted = _strokeProcessor.AddPoint(info, context.BrushSize);
        if (emitted.Count == 0)
        {
            return false;
        }

        var firstNewPointIndex = stroke.Points.Count;
        foreach (var point in emitted)
        {
            stroke.Points.Add(point);
        }

        _rasterCache.AppendStrokeRange(_strokeSession.FrameIndex, _strokeSession.LayerIndex, stroke, firstNewPointIndex);
        return true;
    }

    private bool TryMakePaintInfo(CanvasInputContext context, PointerPoint point, out PaintInformation info)
    {
        var project = context.Project;
        if (project is null ||
            !_viewport.TryScreenToDocument(_getViewportSize(), GetDocumentSize(project), point.Position, out var doc))
        {
            info = default;
            return false;
        }

        var props = point.Properties;
        var pressure = props.Pressure > 0 ? (double)props.Pressure : 1.0;
        info = new PaintInformation(doc.X, doc.Y, pressure,
            props.XTilt / 90.0, props.YTilt / 90.0,
            props.Twist * Math.PI / 180.0, 0);

        context.CursorInfoCallback?.Invoke(doc.X, doc.Y, pressure);
        return true;
    }

    private void CancelActiveStroke()
    {
        if (_strokeSession.TryEnd(out var stroke, out var frame, out var frameIndex, out var layerIndex))
        {
            frame.Strokes.Remove(stroke);
            _rasterCache.InvalidateStroke(frameIndex, layerIndex, stroke);
            _requestInvalidate();
        }

        _strokeProcessor.Reset();
    }

    private static Size GetDocumentSize(DrawingProject project) => new(project.Width, project.Height);

    private static bool IsFitShortcut(KeyEventArgs e) =>
        e.Key == Key.F && e.KeyModifiers == KeyModifiers.None;

    private static bool IsResetShortcut(KeyEventArgs e) =>
        e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
        (e.Key == Key.D0 || e.Key == Key.NumPad0);
}
