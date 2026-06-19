namespace FluxionDrawAndAnimate.Core.Drawing;

/// <summary>
/// B/4 stroke stabiliser. Smooths shaky input with a weighted moving average
/// over the last <see cref="SampleCount"/> raw positions, producing the visible
/// "póráz" (leash) effect between the live cursor and the stabilised point.
///
/// Behaviour:
///   • <see cref="SampleCount"/> == 0 → pass-through (no smoothing, no lag).
///   • Higher SampleCount → smoother stroke but more lag behind the cursor.
///   • Weights are linear: the newest sample weighs N, the oldest weighs 1.
///     This keeps the stabilised point responsive to deliberate motion while
///     damping high-frequency jitter (tremor, noisy digitiser).
///   • Position (X, Y) is averaged; pressure, tilt and rotation follow the
///     newest sample so the artist's intent isn't smeared across the buffer.
///
/// This is the single biggest quality jump on touch/stylus — it's what makes
/// a shaky hand produce a confident line. It runs inside <see cref="StrokeProcessor"/>
/// before spacing and dynamics, so every downstream stage sees the stabilised
/// position.
/// </summary>
public sealed class Stabilizer
{
    private readonly Queue<PaintInformation> _buffer = new();
    private int _sampleCount;

    /// <summary>Number of recent samples to average. 0 = disabled (pass-through).</summary>
    public int SampleCount
    {
        get => _sampleCount;
        set
        {
            var clamped = Math.Max(0, value);
            if (_sampleCount == clamped)
            {
                return;
            }

            _sampleCount = clamped;
            _buffer.Clear();
        }
    }

    public bool IsEnabled => _sampleCount > 0;

    public int BufferedCount => _buffer.Count;

    /// <summary>Clear the rolling buffer. Call on stroke start.</summary>
    public void Reset()
    {
        _buffer.Clear();
    }

    /// <summary>
    /// Feed one raw sample and return the stabilised sample to feed downstream.
    /// When disabled, returns the input unchanged.
    /// </summary>
    public PaintInformation Process(PaintInformation raw)
    {
        if (!IsEnabled)
        {
            return raw;
        }

        _buffer.Enqueue(raw);
        while (_buffer.Count > _sampleCount)
        {
            _buffer.Dequeue();
        }

        // Weighted average: newest weighs most. The queue is oldest→newest, so
        // weight = (index + 1), giving 1 for the oldest and N for the newest.
        var totalWeight = 0.0;
        var sumX = 0.0;
        var sumY = 0.0;
        var i = 0;
        foreach (var sample in _buffer)
        {
            i++;
            totalWeight += i;
            sumX += sample.X * i;
            sumY += sample.Y * i;
        }

        if (totalWeight <= 0)
        {
            return raw;
        }

        // Pressure / tilt / rotation follow the newest sample — don't smear intent.
        return raw with { X = sumX / totalWeight, Y = sumY / totalWeight };
    }
}
