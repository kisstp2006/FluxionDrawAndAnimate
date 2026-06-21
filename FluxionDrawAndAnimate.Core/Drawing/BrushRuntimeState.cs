using System;

namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>
/// Mutable per-brush runtime state. This remembers user-tuned values when the
/// active brush changes, instead of sharing one global slider value for all tools.
/// </summary>
public sealed class BrushRuntimeState
{
    internal BrushRuntimeState(BrushRuntimeDefaults defaults)
    {
        BrushId = BrushManager.ResolveBrushId(defaults.BrushId, ToolKind.Pencil);
        Size = ClampSize(defaults.Size);
        Opacity = ClampUnit(defaults.Opacity, 1.0);
        Hardness = ClampUnit(defaults.Hardness, 0.8);
        Flow = ClampUnit(defaults.Flow, 1.0);
        BlendMode = defaults.BlendMode;
    }

    public string BrushId { get; }
    public double Size { get; private set; }
    public double Opacity { get; private set; }
    public double Hardness { get; private set; }
    public double Flow { get; private set; }
    public BlendMode BlendMode { get; private set; }

    public void SetSize(double value) => Size = ClampSize(value);
    public void SetOpacity(double value) => Opacity = ClampUnit(value, Opacity);
    public void SetHardness(double value) => Hardness = ClampUnit(value, Hardness);
    public void SetFlow(double value) => Flow = ClampUnit(value, Flow);
    public void SetBlendMode(BlendMode value) => BlendMode = value;

    private static double ClampSize(double value) =>
        Math.Clamp(double.IsFinite(value) ? value : 10.0, 1.0, 500.0);

    private static double ClampUnit(double value, double fallback) =>
        Math.Clamp(double.IsFinite(value) ? value : fallback, 0.0, 1.0);
}
