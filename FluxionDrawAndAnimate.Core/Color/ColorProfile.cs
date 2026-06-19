namespace FluxionDrawAndAnimate.Core.Color;

public sealed class ColorProfile
{
    public static ColorProfile Default { get; } = new();

    public ColorProfileKind Kind { get; set; } = ColorProfileKind.Srgb;
    public double Gamma { get; set; } = 2.2;
    public bool UseLinearBlending { get; set; }
    public string? IccProfileName { get; set; }
}

public enum ColorProfileKind
{
    Srgb,
    DisplayP3,
    AdobeRgb,
    Rec709,
    CustomIcc
}
