using FluxionDrawAndAnimate.Core.Drawing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Drawing;

public class StrokeProcessorTests
{
    private static PaintInformation P(double x, double y, double pressure = 1.0) =>
        new(x, y, pressure, 0, 0, 0, 0);

    [Fact]
    public void AddPoint_emits_first_point_immediately()
    {
        var proc = new StrokeProcessor();

        var emitted = proc.AddPoint(P(10, 10), brushSize: 10);

        Assert.Single(emitted);
        Assert.Equal(10, emitted[0].X, 5);
    }

    [Fact]
    public void AddPoint_suppresses_points_closer_than_spacing_fraction()
    {
        var proc = new StrokeProcessor();
        proc.AddPoint(P(0, 0), brushSize: 10);

        // Spacing = 10 * 0.25 = 2.5px. A 1px move should be suppressed.
        var emitted = proc.AddPoint(P(1, 0), brushSize: 10);

        Assert.Empty(emitted);
    }

    [Fact]
    public void AddPoint_emits_once_cursor_moves_past_spacing_threshold()
    {
        var proc = new StrokeProcessor();
        proc.AddPoint(P(0, 0), brushSize: 10);

        // 3px > 2.5px threshold → emit.
        var emitted = proc.AddPoint(P(3, 0), brushSize: 10);

        Assert.Single(emitted);
        Assert.Equal(3, emitted[0].X, 5);
    }

    [Fact]
    public void Reset_clears_state_so_next_point_emits_again()
    {
        var proc = new StrokeProcessor();
        proc.AddPoint(P(0, 0), brushSize: 10);
        proc.AddPoint(P(3, 0), brushSize: 10);

        proc.Reset();

        var emitted = proc.AddPoint(P(3, 0), brushSize: 10);
        Assert.Single(emitted); // first point after reset always emits
    }

    [Fact]
    public void BuildSmoothedPath_returns_raw_when_fewer_than_three_points()
    {
        var proc = new StrokeProcessor();
        proc.AddPoint(P(0, 0), brushSize: 10);
        proc.AddPoint(P(3, 0), brushSize: 10);

        var smoothed = proc.BuildSmoothedPath();

        Assert.Equal(2, smoothed.Count);
    }

    [Fact]
    public void BuildSmoothedPath_interpolates_between_sparse_samples()
    {
        var proc = new StrokeProcessor();
        proc.AddPoint(P(0, 0), brushSize: 10);
        proc.AddPoint(P(50, 0), brushSize: 10);
        proc.AddPoint(P(100, 0), brushSize: 10);

        var smoothed = proc.BuildSmoothedPath();

        // Catmull-Rom subdivides the 50px segments into many sub-samples.
        Assert.True(smoothed.Count > 3, $"Expected >3 smoothed points, got {smoothed.Count}");
        // First interpolated point stays near the start, last near the end.
        Assert.True(smoothed[0].X <= 1, "First smoothed point should be near x=0");
        Assert.True(smoothed[^1].X >= 99, "Last smoothed point should be near x=100");
    }

    [Fact]
    public void BuildSmoothedPath_linearly_interpolates_pressure()
    {
        var proc = new StrokeProcessor();
        proc.AddPoint(P(0, 0, pressure: 0.2), brushSize: 10);
        proc.AddPoint(P(50, 0, pressure: 0.8), brushSize: 10);
        proc.AddPoint(P(100, 0, pressure: 0.2), brushSize: 10);

        var smoothed = proc.BuildSmoothedPath();

        // Pressure should rise from the low endpoints toward the middle peak (0.8)
        // and never exceed the input range. The smoothed midpoint should sit
        // between the endpoint minimum and the central peak.
        var mid = smoothed[smoothed.Count / 2];
        Assert.True(mid.Pressure >= 0.2 && mid.Pressure <= 0.8,
            $"Mid pressure {mid.Pressure} should stay within [0.2, 0.8]");
        // First point stays near the starting low pressure, last near the ending low.
        Assert.True(smoothed[0].Pressure <= 0.3, "First point pressure should be near 0.2");
        Assert.True(smoothed[^1].Pressure <= 0.3, "Last point pressure should be near 0.2");
    }
}
