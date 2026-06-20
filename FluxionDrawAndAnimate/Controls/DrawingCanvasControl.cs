using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Editing;
using FluxionDrawAndAnimate.Rendering;

namespace FluxionDrawAndAnimate.Controls;

/// <summary>
/// Interactive drawing canvas with a fully featured viewport:
///   Pan     — Space+drag (desktop) · middle-mouse drag · two-finger drag (touch)
///   Zoom    — Ctrl+scroll · pinch (touch) · Ctrl+Plus/Minus · F = fit · Ctrl+0 = reset
///   Rotate  — Alt+scroll (desktop) · two-finger twist (touch)
/// </summary>
public sealed class DrawingCanvasControl : Control
{
    // ── Styled properties ────────────────────────────────────────────────

    public static readonly StyledProperty<DrawingProject?> ProjectProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, DrawingProject?>(nameof(Project));
    public static readonly StyledProperty<int> ActiveFrameIndexProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, int>(nameof(ActiveFrameIndex));
    public static readonly StyledProperty<int> ActiveLayerIndexProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, int>(nameof(ActiveLayerIndex));
    public static readonly StyledProperty<RgbaColor> BrushColorProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, RgbaColor>(nameof(BrushColor), RgbaColor.White);
    public static readonly StyledProperty<double> BrushSizeProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, double>(nameof(BrushSize), 12d);
    public static readonly StyledProperty<ToolKind> ActiveToolKindProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, ToolKind>(nameof(ActiveToolKind), ToolKind.Pencil);
    public static readonly StyledProperty<BrushSettings?> BrushSettingsProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, BrushSettings?>(nameof(BrushSettings));
    public static readonly StyledProperty<bool> ShowOnionSkinProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, bool>(nameof(ShowOnionSkin), true);
    public static readonly StyledProperty<UndoStack?> UndoStackProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, UndoStack?>(nameof(UndoStack));
    public static readonly StyledProperty<Action<double, double, double>?> CursorInfoCallbackProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, Action<double, double, double>?>(nameof(CursorInfoCallback));
    public static readonly StyledProperty<Action<double>?> ScaleChangedCallbackProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, Action<double>?>(nameof(ScaleChangedCallback));

    /// <summary>Incrementing this triggers <see cref="FitToView"/> — bind from VM's FitToView relay.</summary>
    public static readonly StyledProperty<int> FitToViewTriggerProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, int>(nameof(FitToViewTrigger));

    // ── Fields ───────────────────────────────────────────────────────────

    private readonly DrawingProjectFrameRenderer _renderer = new();
    private readonly RasterFrameCache _rasterCache = new();
    private readonly StrokeProcessor _strokeProcessor = new();
    private StrokePath? _currentStroke;
    private AnimationFrame? _currentStrokeFrame;

    // Viewport state
    private double _vpScale = 1.0;
    private double _vpOffsetX;
    private double _vpOffsetY;
    private double _vpRotation;

    // Pan state
    private bool _isPanning;
    private Point _panLastPos;
    private bool _spaceDown;

    // Touch gesture state
    private readonly List<(int Id, Point Pos)> _touches = new();
    private double _touchStartDist;
    private Point _touchStartCenter;
    private double _touchStartScale;
    private double _touchStartOffX;
    private double _touchStartOffY;
    private double _touchStartAngle;
    private double _touchStartRotation;

    // Static brushes / pens
    private static readonly IBrush WorkspaceBrush = new SolidColorBrush(Color.FromRgb(21, 22, 25));
    private static readonly IBrush PaperBrush = Brushes.White;

    // ── Properties ───────────────────────────────────────────────────────

    public DrawingProject? Project { get => GetValue(ProjectProperty); set => SetValue(ProjectProperty, value); }
    public int ActiveFrameIndex { get => GetValue(ActiveFrameIndexProperty); set => SetValue(ActiveFrameIndexProperty, value); }
    public int ActiveLayerIndex { get => GetValue(ActiveLayerIndexProperty); set => SetValue(ActiveLayerIndexProperty, value); }
    public RgbaColor BrushColor { get => GetValue(BrushColorProperty); set => SetValue(BrushColorProperty, value); }
    public double BrushSize { get => GetValue(BrushSizeProperty); set => SetValue(BrushSizeProperty, value); }
    public ToolKind ActiveToolKind { get => GetValue(ActiveToolKindProperty); set => SetValue(ActiveToolKindProperty, value); }
    public BrushSettings? BrushSettings { get => GetValue(BrushSettingsProperty); set => SetValue(BrushSettingsProperty, value); }
    public bool ShowOnionSkin { get => GetValue(ShowOnionSkinProperty); set => SetValue(ShowOnionSkinProperty, value); }
    public UndoStack? UndoStack { get => GetValue(UndoStackProperty); set => SetValue(UndoStackProperty, value); }
    public Action<double, double, double>? CursorInfoCallback { get => GetValue(CursorInfoCallbackProperty); set => SetValue(CursorInfoCallbackProperty, value); }
    public Action<double>? ScaleChangedCallback { get => GetValue(ScaleChangedCallbackProperty); set => SetValue(ScaleChangedCallbackProperty, value); }
    public int FitToViewTrigger { get => GetValue(FitToViewTriggerProperty); set => SetValue(FitToViewTriggerProperty, value); }

    // ── Static constructor ────────────────────────────────────────────────

    static DrawingCanvasControl()
    {
        AffectsRender<DrawingCanvasControl>(
            ProjectProperty, ActiveFrameIndexProperty, ActiveLayerIndexProperty,
            BrushColorProperty, BrushSizeProperty, ActiveToolKindProperty,
            BrushSettingsProperty, ShowOnionSkinProperty);

        FitToViewTriggerProperty.Changed
            .AddClassHandler<DrawingCanvasControl>((c, _) => c.FitToView());
    }

    // ── Public viewport API ───────────────────────────────────────────────

    public void FitToView()
    {
        var project = Project;
        if (project is null || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        var pad = 40;
        _vpScale = Math.Min((Bounds.Width - pad) / project.Width, (Bounds.Height - pad) / project.Height);
        _vpOffsetX = _vpOffsetY = _vpRotation = 0;
        NotifyScale();
        InvalidateVisual();
    }

    public void ResetView()
    {
        _vpScale = 1.0;
        _vpOffsetX = _vpOffsetY = _vpRotation = 0;
        NotifyScale();
        InvalidateVisual();
    }

    // ── Render ───────────────────────────────────────────────────────────

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Workspace background in LOCAL coordinates (0,0 = control top-left).
        // Bounds has the parent-relative position so we must NOT use it directly here.
        context.DrawRectangle(WorkspaceBrush, null, new Rect(0, 0, Bounds.Width, Bounds.Height));

        var project = Project;
        if (project is null) return;

        _rasterCache.Configure(project);

        // Push viewport transform — everything below is in document space
        var matrix = BuildMatrix(project);
        using var tf = context.PushTransform(matrix);

        var docRect = new Rect(0, 0, project.Width, project.Height);

        // Paper (white background)
        context.DrawRectangle(PaperBrush, null, docRect);

        // Onion skin frames
        if (ShowOnionSkin)
        {
            DrawCachedFrame(context, project, ActiveFrameIndex - 1, 0.22);
            DrawCachedFrame(context, project, ActiveFrameIndex + 1, 0.16);
        }

        // Current frame
        DrawCachedFrame(context, project, ActiveFrameIndex, 1.0);

        // Document border (1 px in screen space, so divide by scale)
        var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(80, 90, 100)), 1.0 / _vpScale);
        context.DrawRectangle(null, borderPen, docRect);
    }

    private void DrawCachedFrame(DrawingContext context, DrawingProject project, int frameIndex, double opacity)
    {
        if (frameIndex < 0 || frameIndex >= project.FrameCount) return;
        var bitmap = _rasterCache.GetFrameBitmap(frameIndex);
        if (bitmap is null) return;
        var docRect = new Rect(0, 0, project.Width, project.Height);
        if (opacity < 1) { using var op = context.PushOpacity(opacity); context.DrawImage(bitmap, docRect); }
        else context.DrawImage(bitmap, docRect);
    }

    // ── Matrix helpers ───────────────────────────────────────────────────

    private Matrix BuildMatrix(DrawingProject project)
    {
        // doc centre → origin → scale → rotate → canvas centre + pan
        return Matrix.CreateTranslation(-project.Width / 2.0, -project.Height / 2.0)
             * Matrix.CreateScale(_vpScale, _vpScale)
             * Matrix.CreateRotation(_vpRotation)
             * Matrix.CreateTranslation(Bounds.Width / 2 + _vpOffsetX, Bounds.Height / 2 + _vpOffsetY);
    }

    // Transform a screen point to document space (returns false if outside bounds or matrix is degenerate)
    private bool ScreenToDoc(DrawingProject project, Point screen, out Point doc)
    {
        doc = default;
        var matrix = BuildMatrix(project);
        try
        {
            var inv = matrix.Invert();
            doc = ApplyMatrix(inv, screen);
            return doc.X >= 0 && doc.X <= project.Width && doc.Y >= 0 && doc.Y <= project.Height;
        }
        catch { return false; }
    }

    private static Point ApplyMatrix(Matrix m, Point p) =>
        new(p.X * m.M11 + p.Y * m.M21 + m.M31,
            p.X * m.M12 + p.Y * m.M22 + m.M32);

    // ── Viewport operations ──────────────────────────────────────────────

    private void ZoomAt(Point pivot, double factor)
    {
        var newScale = Math.Clamp(_vpScale * factor, 0.05, 32.0);
        var f = newScale / _vpScale;
        _vpOffsetX = pivot.X - Bounds.Width / 2 - (pivot.X - Bounds.Width / 2 - _vpOffsetX) * f;
        _vpOffsetY = pivot.Y - Bounds.Height / 2 - (pivot.Y - Bounds.Height / 2 - _vpOffsetY) * f;
        _vpScale = newScale;
        NotifyScale();
        InvalidateVisual();
    }

    private void NotifyScale() => ScaleChangedCallback?.Invoke(_vpScale);

    // ── Keyboard (subscribed to TopLevel to catch Space without focus req) ──

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var tl = TopLevel.GetTopLevel(this);
        tl?.AddHandler(KeyDownEvent, OnGlobalKeyDown, RoutingStrategies.Tunnel);
        tl?.AddHandler(KeyUpEvent, OnGlobalKeyUp, RoutingStrategies.Tunnel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        var tl = TopLevel.GetTopLevel(this);
        tl?.RemoveHandler(KeyDownEvent, OnGlobalKeyDown);
        tl?.RemoveHandler(KeyUpEvent, OnGlobalKeyUp);
    }

    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space:
                if (!_spaceDown)
                {
                    _spaceDown = true;
                    Cursor = new Cursor(StandardCursorType.SizeAll);
                }
                break;
            case Key.F when !e.Handled:
                FitToView();
                e.Handled = true;
                break;
            case Key.D0 when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                ResetView();
                e.Handled = true;
                break;
            case Key.OemPlus when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                ZoomAt(new Point(Bounds.Width / 2, Bounds.Height / 2), 1.25);
                e.Handled = true;
                break;
            case Key.OemMinus when e.KeyModifiers.HasFlag(KeyModifiers.Control):
                ZoomAt(new Point(Bounds.Width / 2, Bounds.Height / 2), 0.8);
                e.Handled = true;
                break;
        }
    }

    private void OnGlobalKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _spaceDown = false;
            _isPanning = false;
            Cursor = Cursor.Default;
        }
    }

    // ── Scroll wheel ─────────────────────────────────────────────────────

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var pivot = e.GetPosition(this);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            // Rotate
            _vpRotation += e.Delta.Y > 0 ? -0.05 : 0.05;
            InvalidateVisual();
            e.Handled = true;
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.Delta.X == 0)
        {
            // Zoom (Ctrl+scroll OR vertical-only scroll without modifier also zooms for trackpads)
            var factor = e.Delta.Y > 0 ? 1.12 : 1.0 / 1.12;
            ZoomAt(pivot, factor);
            e.Handled = true;
        }
        else
        {
            // Horizontal scroll → pan
            _vpOffsetX -= e.Delta.X * 20;
            _vpOffsetY -= e.Delta.Y * 20;
            InvalidateVisual();
            e.Handled = true;
        }
    }

    // ── Pointer pressed ──────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pos = e.GetPosition(this);
        var props = e.GetCurrentPoint(this).Properties;
        var project = Project;

        // ── Touch: track fingers ───────────────────────────────────────
        if (e.Pointer.Type == PointerType.Touch)
        {
            _touches.RemoveAll(t => t.Id == e.Pointer.Id);
            _touches.Add((e.Pointer.Id, pos));

            if (_touches.Count == 2)
            {
                _touchStartDist = Dist(_touches[0].Pos, _touches[1].Pos);
                _touchStartCenter = Mid(_touches[0].Pos, _touches[1].Pos);
                _touchStartScale = _vpScale;
                _touchStartOffX = _vpOffsetX;
                _touchStartOffY = _vpOffsetY;
                _touchStartAngle = Angle(_touches[0].Pos, _touches[1].Pos);
                _touchStartRotation = _vpRotation;
                _currentStroke = null; // cancel any stroke
            }

            e.Pointer.Capture(this);
            if (_touches.Count >= 2) { e.Handled = true; return; }
        }

        // ── Middle mouse / Space+Left → pan ───────────────────────────
        if (props.IsMiddleButtonPressed || (props.IsLeftButtonPressed && _spaceDown))
        {
            _isPanning = true;
            _panLastPos = pos;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        // ── Painting tools ────────────────────────────────────────────
        if (project is null) return;

        // Non-painting tools — block
        var nonPainting = new[]
        {
            ToolKind.Select, ToolKind.Lasso, ToolKind.Move, ToolKind.Rotate,
            ToolKind.Text, ToolKind.Fill, ToolKind.Shape,
            ToolKind.Eyedropper, ToolKind.Ruler, ToolKind.Symmetry
        };
        if (Array.IndexOf(nonPainting, ActiveToolKind) >= 0) return;

        var layer = project.Layers.ElementAtOrDefault(ActiveLayerIndex);
        if (layer is null || layer.IsLocked || !layer.IsVisible) return;

        if (!TryMakePaintInfo(project, e.GetCurrentPoint(this), out var paintInfo)) return;

        _strokeProcessor.Reset();
        var frame = layer.EnsureFrame(ActiveFrameIndex);
        var brushSettings = BrushSettings ?? BrushSettings.ForTool(ActiveToolKind);
        _strokeProcessor.Configure(brushSettings);
        _currentStroke = new StrokePath(BrushColor, BrushSize, ActiveToolKind, ActiveToolKind == ToolKind.Vector, brushSettings);
        _currentStrokeFrame = frame;

        foreach (var p in _strokeProcessor.AddPoint(paintInfo, BrushSize))
            _currentStroke.Points.Add(p);

        frame.Strokes.Add(_currentStroke);
        _rasterCache.InvalidateStroke(ActiveFrameIndex, _currentStroke);

        e.Pointer.Capture(this);
        e.Handled = true;
        InvalidateVisual();
    }

    // ── Pointer moved ────────────────────────────────────────────────────

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var pos = e.GetPosition(this);
        var project = Project;

        // ── Touch gesture (two fingers) ───────────────────────────────
        if (e.Pointer.Type == PointerType.Touch)
        {
            for (var i = 0; i < _touches.Count; i++)
            {
                if (_touches[i].Id == e.Pointer.Id)
                {
                    _touches[i] = (e.Pointer.Id, pos);
                    break;
                }
            }

            if (_touches.Count == 2)
            {
                var dist = Dist(_touches[0].Pos, _touches[1].Pos);
                var center = Mid(_touches[0].Pos, _touches[1].Pos);
                var angle = Angle(_touches[0].Pos, _touches[1].Pos);

                if (_touchStartDist > 0)
                    _vpScale = Math.Clamp(_touchStartScale * dist / _touchStartDist, 0.05, 32.0);

                _vpOffsetX = _touchStartOffX + (center.X - _touchStartCenter.X);
                _vpOffsetY = _touchStartOffY + (center.Y - _touchStartCenter.Y);
                _vpRotation = _touchStartRotation + (angle - _touchStartAngle);

                NotifyScale();
                InvalidateVisual();
                e.Handled = true;
                return;
            }
        }

        // ── Pan ────────────────────────────────────────────────────────
        if (_isPanning)
        {
            _vpOffsetX += pos.X - _panLastPos.X;
            _vpOffsetY += pos.Y - _panLastPos.Y;
            _panLastPos = pos;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        // ── Drawing ────────────────────────────────────────────────────
        if (_currentStroke is null || project is null) return;

        var redraw = false;
        foreach (var intermediate in e.GetIntermediatePoints(this))
        {
            if (TryMakePaintInfo(project, intermediate, out var info))
                redraw |= AppendToStroke(info);
        }
        if (TryMakePaintInfo(project, e.GetCurrentPoint(this), out var current))
            redraw |= AppendToStroke(current);

        if (redraw) { e.Handled = true; InvalidateVisual(); }
    }

    // ── Pointer released ─────────────────────────────────────────────────

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        // Touch: remove finger
        if (e.Pointer.Type == PointerType.Touch)
        {
            _touches.RemoveAll(t => t.Id == e.Pointer.Id);
            if (_touches.Count < 2) _touchStartDist = 0;
        }

        if (_isPanning) { _isPanning = false; e.Handled = true; return; }

        // Finish stroke
        if (_currentStroke is not null && _currentStrokeFrame is not null)
        {
            var smoothed = _strokeProcessor.BuildSmoothedPath();
            _currentStroke.Points.Clear();
            foreach (var p in smoothed) _currentStroke.Points.Add(p);

            _rasterCache.InvalidateStroke(ActiveFrameIndex, _currentStroke);

            var stroke = _currentStroke;
            var frame = _currentStrokeFrame;
            var frameIndex = ActiveFrameIndex;
            UndoStack?.Push(new AddStrokeCommand(frame, stroke, changed =>
            {
                _rasterCache.InvalidateStroke(frameIndex, changed);
                InvalidateVisual();
            }));
        }

        _currentStroke = null;
        _currentStrokeFrame = null;
        e.Pointer.Capture(null);
        e.Handled = true;
        InvalidateVisual();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private bool AppendToStroke(PaintInformation info)
    {
        var emitted = _strokeProcessor.AddPoint(info, BrushSize);
        if (emitted.Count == 0) return false;
        foreach (var p in emitted) _currentStroke!.Points.Add(p);
        _rasterCache.InvalidateStroke(ActiveFrameIndex, _currentStroke!);
        return true;
    }

    private bool TryMakePaintInfo(DrawingProject project, PointerPoint pp, out PaintInformation info)
    {
        if (!ScreenToDoc(project, pp.Position, out var doc))
        {
            info = default;
            return false;
        }

        var props = pp.Properties;
        var pressure = props.Pressure > 0 ? (double)props.Pressure : 1.0;
        info = new PaintInformation(doc.X, doc.Y, pressure,
            props.XTilt / 90.0, props.YTilt / 90.0,
            props.Twist * Math.PI / 180.0, 0);

        CursorInfoCallback?.Invoke(doc.X, doc.Y, pressure);
        return true;
    }

    // Touch geometry helpers
    private static double Dist(Point a, Point b)
    {
        var dx = b.X - a.X; var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static Point Mid(Point a, Point b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2);
    private static double Angle(Point a, Point b) => Math.Atan2(b.Y - a.Y, b.X - a.X);
}
