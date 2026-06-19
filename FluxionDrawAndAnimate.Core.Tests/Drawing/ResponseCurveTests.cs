using FluxionDrawAndAnimate.Core.Drawing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Drawing;

public class ResponseCurveTests
{
    [Fact]
    public void Linear_returns_input_unchanged_at_endpoints()
    {
        var curve = ResponseCurve.Linear();

        Assert.Equal(0, curve.Evaluate(0), 5);
        Assert.Equal(1, curve.Evaluate(1), 5);
    }

    [Fact]
    public void Linear_returns_input_unchanged_in_middle()
    {
        var curve = ResponseCurve.Linear();

        Assert.Equal(0.5, curve.Evaluate(0.5), 5);
    }

    [Fact]
    public void Evaluate_clamps_input_above_one_to_last_point()
    {
        var curve = ResponseCurve.Linear();

        Assert.Equal(1, curve.Evaluate(1.5), 5);
    }

    [Fact]
    public void Evaluate_clamps_input_below_zero_to_first_point()
    {
        var curve = ResponseCurve.Linear();

        Assert.Equal(0, curve.Evaluate(-0.5), 5);
    }

    [Fact]
    public void EaseIn_needs_strong_pressure_for_full_effect()
    {
        var curve = ResponseCurve.EaseIn();

        // At mid pressure the output should be well below 0.5 (slow start).
        Assert.True(curve.Evaluate(0.5) < 0.3, $"EaseIn at 0.5 should be < 0.3 but was {curve.Evaluate(0.5)}");
        // At full pressure it still reaches 1.
        Assert.Equal(1, curve.Evaluate(1), 5);
    }

    [Fact]
    public void EaseOut_responds_quickly_at_low_pressure()
    {
        var curve = ResponseCurve.EaseOut();

        // At low pressure the output should already be above 0.5 (fast start).
        Assert.True(curve.Evaluate(0.15) > 0.4, $"EaseOut at 0.15 should be > 0.4 but was {curve.Evaluate(0.15)}");
        Assert.Equal(0, curve.Evaluate(0), 5);
    }

    [Fact]
    public void Custom_curve_interpolates_between_control_points()
    {
        var curve = new ResponseCurve
        {
            Points = [new(0, 0), new(0.5, 0.25), new(1, 1)]
        };

        // Halfway between (0,0) and (0.5,0.25) at x=0.25 → y=0.125.
        Assert.Equal(0.125, curve.Evaluate(0.25), 5);
    }

    [Fact]
    public void Evaluate_with_single_point_returns_that_point_y()
    {
        var curve = new ResponseCurve { Points = [new(0.3, 0.7)] };

        Assert.Equal(0.7, curve.Evaluate(0.5), 5);
    }

    [Fact]
    public void Evaluate_with_empty_points_returns_input_identity()
    {
        var curve = new ResponseCurve { Points = [] };

        Assert.Equal(0.42, curve.Evaluate(0.42), 5);
    }
}
