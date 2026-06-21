using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using FluxionDrawAndAnimate.Controls.Canvas;
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
    public static readonly StyledProperty<int> ResetViewTriggerProperty =
        AvaloniaProperty.Register<DrawingCanvasControl, int>(nameof(ResetViewTrigger));

    // ── Fields ───────────────────────────────────────────────────────────

    private readonly RasterFrameCache _rasterCache = new();
    private readonly ViewportState _viewport = new();
    private readonly CanvasInvalidationScheduler _invalidation = new();
    private readonly CanvasInputController _input;

    // Static brushes / pens
    private static readonly IBrush WorkspaceBrush = new SolidColorBrush(Color.FromRgb(21, 22, 25));
    private static readonly IBrush PaperBrush = Brushes.White;
    private static readonly IBrush DocumentBorderBrush = new SolidColorBrush(Color.FromRgb(80, 90, 100));

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
    public int ResetViewTrigger { get => GetValue(ResetViewTriggerProperty); set => SetValue(ResetViewTriggerProperty, value); }

    // ── Static constructor ────────────────────────────────────────────────

    static DrawingCanvasControl()
    {
        AffectsRender<DrawingCanvasControl>(
            ProjectProperty, ActiveFrameIndexProperty, ActiveLayerIndexProperty,
            BrushColorProperty, BrushSizeProperty, ActiveToolKindProperty,
            BrushSettingsProperty, ShowOnionSkinProperty);

        FitToViewTriggerProperty.Changed
            .AddClassHandler<DrawingCanvasControl>((c, _) => c.FitToView());
        ResetViewTriggerProperty.Changed
            .AddClassHandler<DrawingCanvasControl>((c, _) => c.ResetView());
    }

    public DrawingCanvasControl()
    {
        Focusable = true;
        _input = new CanvasInputController(
            this,
            _viewport,
            _rasterCache,
            CreateInputContext,
            GetViewportSize,
            RequestInvalidate,
            NotifyScale);
    }

    // ── Public viewport API ───────────────────────────────────────────────

    public void FitToView()
    {
        _input.FitToView();
    }

    public void ResetView()
    {
        _viewport.Reset();
        NotifyScale();
        InvalidateNow();
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
        var matrix = _viewport.BuildMatrix(GetViewportSize(), GetDocumentSize(project));
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
        var borderPen = new Pen(DocumentBorderBrush, 1.0 / _viewport.Scale);
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

    private void NotifyScale() => ScaleChangedCallback?.Invoke(_viewport.Scale);

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
        _rasterCache.Dispose();
    }

    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        _input.OnKeyDown(e);
    }

    private void OnGlobalKeyUp(object? sender, KeyEventArgs e)
    {
        _input.OnKeyUp(e);
    }

    // ── Scroll wheel ─────────────────────────────────────────────────────

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        _input.OnPointerWheelChanged(e);
    }

    // ── Pointer pressed ──────────────────────────────────────────────────

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _input.OnPointerPressed(e);
    }

    // ── Pointer moved ────────────────────────────────────────────────────

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _input.OnPointerMoved(e);
    }

    // ── Pointer released ─────────────────────────────────────────────────

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _input.OnPointerReleased(e);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private CanvasInputContext CreateInputContext() => new(
        Project,
        ActiveFrameIndex,
        ActiveLayerIndex,
        BrushColor,
        BrushSize,
        ActiveToolKind,
        BrushSettings,
        UndoStack,
        CursorInfoCallback);
    private Size GetViewportSize() => new(Bounds.Width, Bounds.Height);
    private static Size GetDocumentSize(DrawingProject project) => new(project.Width, project.Height);
    private void RequestInvalidate() => _invalidation.Request(this);
    private void InvalidateNow() => _invalidation.InvalidateNow(this);
}
