namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>
/// Initial runtime values for a brush. These are copied once when the brush state
/// is first created; user changes then live in <see cref="BrushRuntimeState"/>.
/// </summary>
public readonly record struct BrushRuntimeDefaults(
    string BrushId,
    double Size,
    double Opacity,
    double Hardness,
    double Flow,
    BlendMode BlendMode)
{
    public static BrushRuntimeDefaults FromPreset(
        BrushPreset? preset,
        ToolKind fallbackToolKind,
        double fallbackSize,
        double defaultOpacity = 1.0,
        double defaultFlow = 1.0)
    {
        var settings = preset?.Settings ?? BrushSettings.ForTool(fallbackToolKind);
        var size = preset?.DefaultSize > 0
            ? preset.DefaultSize
            : fallbackSize > 0
                ? fallbackSize
                : 10.0;

        return new BrushRuntimeDefaults(
            BrushManager.ResolveBrushId(preset?.Id, fallbackToolKind),
            size,
            defaultOpacity,
            settings.Hardness,
            defaultFlow,
            BlendMode.Normal);
    }
}
