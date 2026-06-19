namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>
/// Geometric + dynamic parameters for one brush dab.
/// B/5 BrushPreset serialises these; for now ForTool() provides per-tool defaults.
/// </summary>
public sealed class BrushSettings
{
    public BrushShape Shape { get; set; } = BrushShape.Round;

    /// <summary>0 = fully soft (feathers from centre), 1 = fully hard (1-px AA only).</summary>
    public double Hardness { get; set; } = 0.8;

    /// <summary>
    /// B/4 stabiliser strength. Number of recent raw samples averaged together
    /// before spacing/dynamics. 0 = disabled (pass-through); higher = smoother
    /// but laggier ("póráz" between cursor and stabilised point).
    /// Typical: 0 for ink, 4–8 for pencil/sketch, 2 for paint.
    /// </summary>
    public int StabilizerSamples { get; set; } = 0;

    /// <summary>Sensor→parameter bindings that make each stroke feel alive.</summary>
    public BrushDynamics Dynamics { get; set; } = BrushDynamics.None();

    public static BrushSettings ForTool(ToolKind toolKind) => toolKind switch
    {
        ToolKind.Pencil => new BrushSettings
        {
            Shape    = BrushShape.Round,
            Hardness = 0.75,
            Dynamics = BrushDynamics.Pencil()
        },
        ToolKind.Ink => new BrushSettings
        {
            Shape    = BrushShape.HardRound,
            Hardness = 1.00,
            Dynamics = BrushDynamics.Ink()
        },
        ToolKind.Paint => new BrushSettings
        {
            Shape    = BrushShape.Round,
            Hardness = 0.35,
            Dynamics = BrushDynamics.Paint()
        },
        ToolKind.Eraser => new BrushSettings
        {
            Shape    = BrushShape.Round,
            Hardness = 0.85,
            Dynamics = BrushDynamics.Eraser()
        },
        ToolKind.Vector => new BrushSettings
        {
            Shape    = BrushShape.Chisel,
            Hardness = 0.90,
            Dynamics = BrushDynamics.Chisel()
        },
        _ => new BrushSettings()
    };
}
