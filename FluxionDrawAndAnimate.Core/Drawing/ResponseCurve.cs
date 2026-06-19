namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>
/// A piecewise-linear curve that maps a normalised input (0..1) to a
/// normalised output (0..1). Control points must be sorted by X.
/// B/5 replaces the linear segments with Catmull-Rom and adds a curve editor UI.
/// </summary>
public sealed class ResponseCurve
{
    public List<CurvePoint> Points { get; init; } = [new(0, 0), new(1, 1)];

    /// <summary>Evaluate the curve at <paramref name="input"/> ∈ [0,1].</summary>
    public double Evaluate(double input)
    {
        input = Math.Clamp(input, 0, 1);
        var pts = Points;

        if (pts.Count == 0) return input;
        if (pts.Count == 1) return pts[0].Y;
        if (input <= pts[0].X) return pts[0].Y;
        if (input >= pts[^1].X) return pts[^1].Y;

        for (var i = 1; i < pts.Count; i++)
        {
            if (input > pts[i].X)
            {
                continue;
            }

            var span = pts[i].X - pts[i - 1].X;
            if (span <= 0)
            {
                return pts[i].Y;
            }

            var t = (input - pts[i - 1].X) / span;
            return pts[i - 1].Y + (pts[i].Y - pts[i - 1].Y) * t;
        }

        return pts[^1].Y;
    }

    // ── Built-in presets ────────────────────────────────────────────

    /// <summary>Output equals input (identity).</summary>
    public static ResponseCurve Linear() => new();

    /// <summary>Slow start, fast finish — needs strong pressure for full effect.</summary>
    public static ResponseCurve EaseIn() => new()
    {
        Points = [new(0, 0), new(0.5, 0.15), new(0.85, 0.6), new(1, 1)]
    };

    /// <summary>Fast start, slow finish — light pressure already gives large effect.</summary>
    public static ResponseCurve EaseOut() => new()
    {
        Points = [new(0, 0), new(0.15, 0.6), new(0.5, 0.88), new(1, 1)]
    };

    /// <summary>Low and high pressure are opaque; mid-pressure is transparent.</summary>
    public static ResponseCurve SCurve() => new()
    {
        Points = [new(0, 0), new(0.25, 0.08), new(0.5, 0.5), new(0.75, 0.92), new(1, 1)]
    };
}

public readonly record struct CurvePoint(double X, double Y);
