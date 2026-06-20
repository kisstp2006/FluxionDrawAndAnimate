using Avalonia.Media;
using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Presentation;

/// <summary>Wraps an RgbaColor with a pre-built IBrush for use in the swatch panel DataTemplate.</summary>
public sealed class SwatchEntry
{
    public SwatchEntry(RgbaColor color)
    {
        Color = color;
        Brush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(color.R, color.G, color.B));
    }

    public RgbaColor Color { get; }
    public IBrush Brush { get; }
}
