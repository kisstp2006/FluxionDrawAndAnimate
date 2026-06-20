using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Ui.Studio.Inspector;

/// <summary>
/// Custom HSV colour picker: outer hue ring + inner saturation/value square.
/// Draws to a WriteableBitmap — no external package required.
/// </summary>
public partial class HsvColorPicker : UserControl
{
    public static readonly StyledProperty<RgbaColor> SelectedColorProperty =
        AvaloniaProperty.Register<HsvColorPicker, RgbaColor>(
            nameof(SelectedColor), defaultBindingMode: BindingMode.TwoWay);

    public RgbaColor SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    // ── Layout constants ─────────────────────────────────────────────────
    private const int Size      = 220;
    private const int RingOuter = 106;
    private const int RingInner = 82;
    private const int SvSize    = 110;

    private static readonly int SvLeft = (Size - SvSize) / 2;
    private static readonly int SvTop  = (Size - SvSize) / 2;

    // ── HSV state ────────────────────────────────────────────────────────
    private double _hue        = 0;
    private double _saturation = 1;
    private double _value      = 1;

    private bool _draggingRing;
    private bool _draggingSv;
    private bool _updatingFromColor;

    // ── Lifecycle ────────────────────────────────────────────────────────

    public HsvColorPicker()
    {
        InitializeComponent();
        SelectedColorProperty.Changed
            .AddClassHandler<HsvColorPicker>((c, _) => c.OnSelectedColorChanged());
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SvOverlay.Width  = SvSize;
        SvOverlay.Height = SvSize;
        Canvas.SetLeft(SvOverlay, SvLeft);
        Canvas.SetTop(SvOverlay, SvTop);
        SyncHsvFromColor();
        Redraw();
    }

    // ── Change handling ──────────────────────────────────────────────────

    private void OnSelectedColorChanged()
    {
        if (_updatingFromColor) return;
        SyncHsvFromColor();
        Redraw();
    }

    private void SyncHsvFromColor()
    {
        var c = SelectedColor;
        (_hue, _saturation, _value) = RgbToHsv(c.R, c.G, c.B);
        UpdateIndicators();
    }

    private void PushColorFromHsv()
    {
        _updatingFromColor = true;
        var (r, g, b) = HsvToRgb(_hue, _saturation, _value);
        SelectedColor = RgbaColor.FromRgb(r, g, b);
        _updatingFromColor = false;
    }

    // ── Input ────────────────────────────────────────────────────────────

    private void OnRingPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _draggingRing = true;
        UpdateHueFromPointer(e.GetPosition(RingOverlay));
        e.Handled = true;
    }

    private void OnRingPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_draggingRing) return;
        UpdateHueFromPointer(e.GetPosition(RingOverlay));
        e.Handled = true;
    }

    private void OnSvPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _draggingSv = true;
        UpdateSvFromPointer(e.GetPosition(SvOverlay));
        e.Handled = true;
    }

    private void OnSvPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_draggingSv) return;
        UpdateSvFromPointer(e.GetPosition(SvOverlay));
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _draggingRing = _draggingSv = false;
    }

    private void UpdateHueFromPointer(Point pos)
    {
        var cx = Size / 2.0;
        var cy = Size / 2.0;
        var dx = pos.X - cx;
        var dy = pos.Y - cy;
        var dist = Math.Sqrt(dx * dx + dy * dy);

        if (dist < RingInner - 4 || dist > RingOuter + 8) return;

        _hue = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
        PushColorFromHsv();
        Redraw();
    }

    private void UpdateSvFromPointer(Point pos)
    {
        _saturation = Math.Clamp(pos.X / SvSize, 0, 1);
        _value      = Math.Clamp(1 - pos.Y / SvSize, 0, 1);
        PushColorFromHsv();
        RedrawSvOnly();
        UpdateIndicators();
    }

    // ── Drawing ──────────────────────────────────────────────────────────

    private void Redraw()
    {
        var bmp = new WriteableBitmap(
            new PixelSize(Size, Size), new Vector(96, 96),
            PixelFormat.Bgra8888, AlphaFormat.Opaque);

        // IMPORTANT: use stride-aware blit — fb.RowBytes may be larger than
        // Size * 4 due to alignment padding, so we MUST copy row by row rather
        // than doing one Marshal.Copy for the whole flat array.
        using (var fb = bmp.Lock())
        {
            var stride = fb.RowBytes;       // actual bytes per row (may be padded)
            var rowBuf = new byte[Size * 4]; // one row, no padding

            for (var y = 0; y < Size; y++)
            {
                var rowDst = IntPtr.Add(fb.Address, y * stride);

                for (var x = 0; x < Size; x++)
                {
                    var cx = x - Size / 2.0;
                    var cy = y - Size / 2.0;
                    var dist = Math.Sqrt(cx * cx + cy * cy);
                    int r, g, b;

                    if (dist >= RingInner && dist <= RingOuter)
                    {
                        var angle = (Math.Atan2(cy, cx) * 180 / Math.PI + 360) % 360;
                        (r, g, b) = HsvToRgbInt(angle, 1, 1);
                    }
                    else if (x >= SvLeft && x < SvLeft + SvSize &&
                             y >= SvTop  && y < SvTop  + SvSize)
                    {
                        var s = (double)(x - SvLeft) / SvSize;
                        var v = 1.0 - (double)(y - SvTop) / SvSize;
                        (r, g, b) = HsvToRgbInt(_hue, s, v);
                    }
                    else
                    {
                        r = g = b = 20;
                    }

                    var px = x * 4;
                    rowBuf[px]     = (byte)b;  // BGRA
                    rowBuf[px + 1] = (byte)g;
                    rowBuf[px + 2] = (byte)r;
                    rowBuf[px + 3] = 255;
                }

                Marshal.Copy(rowBuf, 0, rowDst, rowBuf.Length);
            }
        }

        HwImage.Source = bmp;
        UpdateIndicators();
    }

    private void RedrawSvOnly()
    {
        // Quick redraw of just the SV square portion on the existing bitmap
        // For simplicity, call full Redraw (fast enough at 220px)
        Redraw();
    }

    private void UpdateIndicators()
    {
        // Hue indicator position on the ring
        var rad = _hue * Math.PI / 180;
        var ringR = (RingInner + RingOuter) / 2.0;
        var cx = Size / 2.0 + ringR * Math.Cos(rad);
        var cy = Size / 2.0 + ringR * Math.Sin(rad);
        Canvas.SetLeft(HueIndicator, cx - 6);
        Canvas.SetTop(HueIndicator, cy - 6);

        // SV indicator position
        var svX = SvLeft + _saturation * SvSize - 5;
        var svY = SvTop  + (1 - _value) * SvSize - 5;
        Canvas.SetLeft(SvIndicator, svX);
        Canvas.SetTop(SvIndicator, svY);
    }

    // ── HSV ↔ RGB conversion ─────────────────────────────────────────────

    private static (int r, int g, int b) HsvToRgbInt(double h, double s, double v)
    {
        var (r, g, b) = HsvToRgbF(h, s, v);
        return ((int)(r * 255), (int)(g * 255), (int)(b * 255));
    }

    private static (byte r, byte g, byte b) HsvToRgb(double h, double s, double v)
    {
        var (r, g, b) = HsvToRgbF(h, s, v);
        return ((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    private static (double r, double g, double b) HsvToRgbF(double h, double s, double v)
    {
        if (s <= 0) return (v, v, v);
        var hh = h / 60.0;
        var i = (int)hh;
        var f = hh - i;
        var p = v * (1 - s);
        var q = v * (1 - f * s);
        var t = v * (1 - (1 - f) * s);
        return (i % 6) switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q)
        };
    }

    private static (double h, double s, double v) RgbToHsv(byte rb, byte gb, byte bb)
    {
        double r = rb / 255.0, g = gb / 255.0, b = bb / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var d = max - min;
        double h = 0;
        if (d > 0)
        {
            if (max == r) h = 60 * (((g - b) / d) % 6);
            else if (max == g) h = 60 * ((b - r) / d + 2);
            else h = 60 * ((r - g) / d + 4);
        }
        if (h < 0) h += 360;
        return (h, max > 0 ? d / max : 0, max);
    }
}
