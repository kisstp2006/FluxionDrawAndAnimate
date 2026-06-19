namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>
/// Processes raw pointer samples into a clean stroke path.
///
/// Pipeline (B/4):
///   1. <see cref="Stabilizer"/> — weighted moving average over the last N raw
///      samples, damping jitter before anything else sees the point.
///   2. Spacing filter — only emits a point when the cursor has moved at least
///      <c>brushSize * SpacingFraction</c> px since the last emission, preventing
///      dab pile-up on slow/stationary input.
///
/// On stroke end (BuildSmoothedPath):
///   • Catmull-Rom interpolation rebuilds the path through smooth cubic curves,
///     removing the jagged zigzags from low-poll-rate or fast input.
///   • Pressure, tilt, rotation are linearly interpolated between raw samples.
/// </summary>
public sealed class StrokeProcessor
{
    public const double SpacingFraction = 0.25;

    private readonly List<PaintInformation> _raw = new();
    private readonly List<PaintInformation> _emitBuffer = new(1);
    private PaintInformation? _lastEmitted;
    private readonly Stabilizer _stabilizer = new();

    /// <summary>
    /// Configure the stabiliser from the active brush settings. Call on stroke
    /// start (or when the active preset changes) so the buffer size matches the
    /// preset's <see cref="BrushSettings.StabilizerSamples"/>.
    /// </summary>
    public void Configure(BrushSettings? brush)
    {
        _stabilizer.SampleCount = brush?.StabilizerSamples ?? 0;
    }

    public void Reset()
    {
        _raw.Clear();
        _emitBuffer.Clear();
        _lastEmitted = null;
        _stabilizer.Reset();
    }

    /// <summary>
    /// Feed one raw sample. Returns the subset that should be rasterized now
    /// (may be empty if the cursor hasn't moved far enough yet).
    /// </summary>
    public IReadOnlyList<PaintInformation> AddPoint(PaintInformation point, double brushSize)
    {
        // B/4: stabilise the raw sample first — spacing and dynamics then work on
        // the smoothed position, so jitter never reaches the dab stream.
        var sample = _stabilizer.Process(point);
        _raw.Add(sample);
        _emitBuffer.Clear();

        if (_lastEmitted is null)
        {
            _lastEmitted = sample;
            _emitBuffer.Add(sample);
            return _emitBuffer;
        }

        var minSpacing = Math.Max(1.0, brushSize * SpacingFraction);
        var dx = sample.X - _lastEmitted.Value.X;
        var dy = sample.Y - _lastEmitted.Value.Y;
        var dist = Math.Sqrt(dx * dx + dy * dy);

        if (dist >= minSpacing)
        {
            _lastEmitted = sample;
            _emitBuffer.Add(sample);
        }

        return _emitBuffer;
    }

    /// <summary>
    /// Returns the Catmull-Rom smoothed version of all collected raw points.
    /// Call once on stroke end; replace stroke.Points with the result.
    /// </summary>
    public IReadOnlyList<PaintInformation> BuildSmoothedPath()
    {
        if (_raw.Count < 3)
        {
            return _raw;
        }

        var result = new List<PaintInformation>(_raw.Count * 4);

        for (var i = 0; i < _raw.Count - 1; i++)
        {
            var p0 = i > 0 ? _raw[i - 1] : Mirror(_raw[i], _raw[i + 1]);
            var p1 = _raw[i];
            var p2 = _raw[i + 1];
            var p3 = i < _raw.Count - 2 ? _raw[i + 2] : Mirror(_raw[i + 1], _raw[i]);

            var dist = Dist(p1, p2);
            var steps = Math.Max(2, (int)(dist / 2.0));

            for (var s = 0; s < steps; s++)
            {
                var t = (double)s / steps;
                result.Add(new PaintInformation(
                    CatmullRom(p0.X, p1.X, p2.X, p3.X, t),
                    CatmullRom(p0.Y, p1.Y, p2.Y, p3.Y, t),
                    Lerp(p1.Pressure, p2.Pressure, t),
                    Lerp(p1.TiltX, p2.TiltX, t),
                    Lerp(p1.TiltY, p2.TiltY, t),
                    Lerp(p1.Rotation, p2.Rotation, t),
                    Lerp(p1.TimestampMs, p2.TimestampMs, t)));
            }
        }

        result.Add(_raw[^1]);
        return result;
    }

    // ------------------------------------------------------------------
    private static double CatmullRom(double p0, double p1, double p2, double p3, double t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return 0.5 * ((2 * p1)
                      + (-p0 + p2) * t
                      + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2
                      + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
    }

    private static PaintInformation Mirror(PaintInformation anchor, PaintInformation other) =>
        anchor with { X = 2 * anchor.X - other.X, Y = 2 * anchor.Y - other.Y };

    private static double Dist(PaintInformation a, PaintInformation b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
