namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>
/// Holds the active sensor→parameter bindings for one brush.
/// Null binding = that parameter is constant (full value, unmodified).
///
/// Evaluate() produces a <see cref="DabParameters"/> for one paint sample
/// given the base size and opacity from <see cref="BrushSettings"/> / stroke.
/// </summary>
public sealed class BrushDynamics
{
    /// <summary>Sensor that scales dab radius (null = constant full size).</summary>
    public SensorBinding? SizeBinding { get; set; }

    /// <summary>Sensor that scales dab opacity (null = constant full opacity).</summary>
    public SensorBinding? OpacityBinding { get; set; }

    public DabParameters Evaluate(PaintInformation info, double baseRadius, double baseOpacity = 1.0)
    {
        var sizeMul = SizeBinding is not null
            ? Math.Max(0.05, SizeBinding.Evaluate(info))
            : 1.0;

        var opacityMul = OpacityBinding is not null
            ? Math.Max(0.01, OpacityBinding.Evaluate(info))
            : 1.0;

        return new DabParameters(
            Math.Max(0.25, baseRadius * sizeMul),
            Math.Clamp(baseOpacity * opacityMul, 0.01, 1.0));
    }

    // ── Per-tool presets (replaced by BrushPreset in B/5) ────────────

    /// <summary>Pencil: pressure controls size (stiff EaseIn) and opacity (SCurve for organic feel).</summary>
    public static BrushDynamics Pencil() => new()
    {
        SizeBinding    = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.EaseIn() },
        OpacityBinding = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.SCurve() }
    };

    /// <summary>Ink: pressure controls size linearly; opacity is always full (wet ink).</summary>
    public static BrushDynamics Ink() => new()
    {
        SizeBinding    = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.Linear() },
        OpacityBinding = null
    };

    /// <summary>Paint: responsive size (EaseOut) and watery opacity (EaseIn needs pressure to opaque).</summary>
    public static BrushDynamics Paint() => new()
    {
        SizeBinding    = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.EaseOut() },
        OpacityBinding = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.EaseIn() }
    };

    /// <summary>Eraser: linear pressure → size, full opacity.</summary>
    public static BrushDynamics Eraser() => new()
    {
        SizeBinding    = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.Linear() },
        OpacityBinding = null
    };

    /// <summary>Chisel / Vector: pressure drives both size and opacity linearly.</summary>
    public static BrushDynamics Chisel() => new()
    {
        SizeBinding    = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.Linear() },
        OpacityBinding = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.Linear() }
    };

    /// <summary>No dynamics — constant size and opacity regardless of input.</summary>
    public static BrushDynamics None() => new();
}
