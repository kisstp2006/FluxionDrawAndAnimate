using Avalonia.Media;
using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Rendering;

public static class AvaloniaColorExtensions
{
    public static Color ToAvaloniaColor(this RgbaColor color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }
}
