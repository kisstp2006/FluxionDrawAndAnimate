using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Editing;
using FluxionDrawAndAnimate.Rendering;

namespace FluxionDrawAndAnimate.Controls;

public sealed class DrawingCanvasControl : Control
{
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

    /// <summary>
    /// Active brush settings (B/5). Bound from the active <see cref="Presentation.ToolPreset"/>'s
    /// <c>EffectiveSettings</c>. When null the canvas falls back to <see cref="BrushSettings.ForTool"/>.
    /// </summary>
    public static readonly StyledProperty<BrushSettings?> BrushSettingsProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, BrushSettings?>(nameof(BrushSettings));

    public static readonly StyledProperty<bool> ShowOnionSkinProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, bool>(nameof(ShowOnionSkin), true);

    public static readonly StyledProperty<UndoStack?> UndoStackProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, UndoStack?>(nameof(UndoStack));

    public static readonly StyledProperty<Action<double, double, double>?> CursorInfoCallbackProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, Action<double, double, double>?>(nameof(CursorInfoCallback));

    private readonly DrawingProjectFrameRenderer _renderer = new();
    private readonly RasterFrameCache _rasterCache = new();
    private readonly StrokeProcessor _strokeProcessor = new();
    private StrokePath? _currentStroke;
    private AnimationFrame? _currentStrokeFrame;

    public DrawingProject? Project
    {
        get => GetValue(ProjectProperty);
        set => SetValue(ProjectProperty, value);
    }

    public int ActiveFrameIndex
    {
        get => GetValue(ActiveFrameIndexProperty);
        set => SetValue(ActiveFrameIndexProperty, value);
    }

    public int ActiveLayerIndex
    {
        get => GetValue(ActiveLayerIndexProperty);
        set => SetValue(ActiveLayerIndexProperty, value);
    }

    public RgbaColor BrushColor
    {
        get => GetValue(BrushColorProperty);
        set => SetValue(BrushColorProperty, value);
    }

    public double BrushSize
    {
        get => GetValue(BrushSizeProperty);
        set => SetValue(BrushSizeProperty, value);
    }

    public ToolKind ActiveToolKind
    {
        get => GetValue(ActiveToolKindProperty);
        set => SetValue(ActiveToolKindProperty, value);
    }

    public BrushSettings? BrushSettings
    {
        get => GetValue(BrushSettingsProperty);
        set => SetValue(BrushSettingsProperty, value);
    }

    public bool ShowOnionSkin
    {
        get => GetValue(ShowOnionSkinProperty);
        set => SetValue(ShowOnionSkinProperty, value);
    }

    public UndoStack? UndoStack
    {
        get => GetValue(UndoStackProperty);
        set => SetValue(UndoStackProperty, value);
    }

    public Action<double, double, double>? CursorInfoCallback
    {
        get => GetValue(CursorInfoCallbackProperty);
        set => SetValue(CursorInfoCallbackProperty, value);
    }

    static DrawingCanvasControl()
    {
        AffectsRender<DrawingCanvasControl>(
            ProjectProperty,
            ActiveFrameIndexProperty,
            ActiveLayerIndexProperty,
            BrushColorProperty,
            BrushSizeProperty,
            ActiveToolKindProperty,
            BrushSettingsProperty,
            ShowOnionSkinProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var project = Project;
        if (project is null)
        {
            context.DrawRectangle(Brushes.Black, null, Bounds);
            return;
        }

        _rasterCache.Configure(project);

        _renderer.DrawWorkspaceBackground(context, Bounds);
        var documentRect = _renderer.CalculateDocumentRect(project, Bounds, 28);
        _renderer.DrawPaperSurface(context, documentRect);

        if (ShowOnionSkin)
        {
            DrawFrameLayer(context, project, ActiveFrameIndex - 1, documentRect, 0.22);
            DrawFrameLayer(context, project, ActiveFrameIndex + 1, documentRect, 0.16);
        }

        DrawFrameLayer(context, project, ActiveFrameIndex, documentRect, 1.0);
        _renderer.DrawBorder(context, documentRect);
    }

    private void DrawFrameLayer(DrawingContext context, DrawingProject project, int frameIndex, Rect documentRect, double opacity)
    {
        if (frameIndex < 0 || frameIndex >= project.FrameCount)
        {
            return;
        }

        var bitmap = _rasterCache.GetFrameBitmap(frameIndex);
        if (bitmap is null)
        {
            return;
        }

        if (opacity < 1)
        {
            using var layer = context.PushOpacity(opacity);
            context.DrawImage(bitmap, documentRect);
        }
        else
        {
            context.DrawImage(bitmap, documentRect);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var project = Project;
        if (project is null || ActiveToolKind == ToolKind.Select || ActiveToolKind == ToolKind.Text)
        {
            return;
        }

        var layer = project.Layers.ElementAtOrDefault(ActiveLayerIndex);
        if (layer is null || layer.IsLocked || !layer.IsVisible)
        {
            return;
        }

        if (!TryMakePaintInfo(project, e.GetCurrentPoint(this), out var paintInfo))
        {
            return;
        }

        _strokeProcessor.Reset();
        var frame = layer.EnsureFrame(ActiveFrameIndex);
        // B/5: prefer the file-described BrushSettings from the active preset; fall
        // back to the per-tool defaults only when no preset is bound (e.g. legacy tools).
        var brushSettings = BrushSettings ?? BrushSettings.ForTool(ActiveToolKind);
        // B/4: configure the stabiliser from the preset before the stroke starts.
        _strokeProcessor.Configure(brushSettings);
        _currentStroke = new StrokePath(BrushColor, BrushSize, ActiveToolKind, ActiveToolKind == ToolKind.Vector, brushSettings);
        _currentStrokeFrame = frame;

        var emitted = _strokeProcessor.AddPoint(paintInfo, BrushSize);
        foreach (var p in emitted)
        {
            _currentStroke.Points.Add(p);
        }

        frame.Strokes.Add(_currentStroke);
        _rasterCache.InvalidateStroke(ActiveFrameIndex, _currentStroke);

        e.Pointer.Capture(this);
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var project = Project;
        if (_currentStroke is null || project is null)
        {
            return;
        }

        var redraw = false;

        // Consume coalesced (intermediate) samples first for smooth fast strokes
        foreach (var intermediate in e.GetIntermediatePoints(this))
        {
            if (!TryMakePaintInfo(project, intermediate, out var info))
            {
                continue;
            }

            redraw |= AppendToStroke(info);
        }

        // Current sample
        if (TryMakePaintInfo(project, e.GetCurrentPoint(this), out var current))
        {
            redraw |= AppendToStroke(current);
        }

        if (redraw)
        {
            e.Handled = true;
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_currentStroke is not null && _currentStrokeFrame is not null)
        {
            // Replace raw/spacing-filtered points with the smooth Catmull-Rom path
            var smoothed = _strokeProcessor.BuildSmoothedPath();
            _currentStroke.Points.Clear();
            foreach (var p in smoothed)
            {
                _currentStroke.Points.Add(p);
            }

            // Re-rasterize the full stroke with the smoothed points
            _rasterCache.InvalidateStroke(ActiveFrameIndex, _currentStroke);

            var stroke = _currentStroke;
            var frame = _currentStrokeFrame;
            var frameIndex = ActiveFrameIndex;
            UndoStack?.Push(new AddStrokeCommand(frame, stroke, changedStroke =>
            {
                _rasterCache.InvalidateStroke(frameIndex, changedStroke);
                InvalidateVisual();
            }));
        }

        _currentStroke = null;
        _currentStrokeFrame = null;
        e.Pointer.Capture(null);
        e.Handled = true;
        InvalidateVisual();
    }

    private bool AppendToStroke(PaintInformation info)
    {
        var emitted = _strokeProcessor.AddPoint(info, BrushSize);
        if (emitted.Count == 0)
        {
            return false;
        }

        foreach (var p in emitted)
        {
            _currentStroke!.Points.Add(p);
        }

        _rasterCache.InvalidateStroke(ActiveFrameIndex, _currentStroke!);
        return true;
    }

    private bool TryMakePaintInfo(DrawingProject project, PointerPoint pointerPoint, out PaintInformation info)
    {
        var position = pointerPoint.Position;
        var documentRect = _renderer.CalculateDocumentRect(project, Bounds, 28);

        if (!documentRect.Contains(position))
        {
            info = default;
            return false;
        }

        var x = (position.X - documentRect.X) / documentRect.Width * project.Width;
        var y = (position.Y - documentRect.Y) / documentRect.Height * project.Height;

        var props = pointerPoint.Properties;
        var pressure = props.Pressure > 0 ? (double)props.Pressure : 1.0;
        var tiltX = (double)props.XTilt / 90.0;
        var tiltY = (double)props.YTilt / 90.0;
        var rotation = (double)props.Twist * Math.PI / 180.0;

        info = new PaintInformation(x, y, pressure, tiltX, tiltY, rotation, 0);

        // Report live cursor info to the status bar
        CursorInfoCallback?.Invoke(x, y, pressure);

        return true;
    }
}
