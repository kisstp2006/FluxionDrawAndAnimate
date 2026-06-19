namespace FluxionDrawAndAnimate.Core.Drawing;

public readonly record struct RgbaColor(byte R, byte G, byte B, byte A = 255)
{
    public static RgbaColor FromRgb(byte r, byte g, byte b)
    {
        return new RgbaColor(r, g, b);
    }

    public static RgbaColor FromArgb(byte a, byte r, byte g, byte b)
    {
        return new RgbaColor(r, g, b, a);
    }

    public static RgbaColor White { get; } = FromRgb(255, 255, 255);
    public static RgbaColor Black { get; } = FromRgb(0, 0, 0);
}
