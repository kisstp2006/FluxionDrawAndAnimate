using FluxionDrawAndAnimate.Core.Drawing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Drawing;

public class BrushDynamicsTests
{
    private static PaintInformation Sample(double pressure) =>
        PaintInformation.FromMouse(0, 0) with { Pressure = pressure };

    [Fact]
    public void Evaluate_returns_base_radius_and_opacity_when_no_bindings()
    {
        var dynamics = BrushDynamics.None();

        var dab = dynamics.Evaluate(Sample(0.5), baseRadius: 5.0, baseOpacity: 0.8);

        Assert.Equal(5.0, dab.Radius, 5);
        Assert.Equal(0.8, dab.Opacity, 5);
    }

    [Fact]
    public void Evaluate_scales_radius_by_size_binding()
    {
        var dynamics = new BrushDynamics
        {
            SizeBinding = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.Linear() },
            OpacityBinding = null
        };

        // Pressure 0.5 → size multiplier 0.5 → radius 5 * 0.5 = 2.5
        var dab = dynamics.Evaluate(Sample(0.5), baseRadius: 5.0);

        Assert.Equal(2.5, dab.Radius, 5);
        Assert.Equal(1.0, dab.Opacity, 5);
    }

    [Fact]
    public void Evaluate_scales_opacity_by_opacity_binding()
    {
        var dynamics = new BrushDynamics
        {
            SizeBinding = null,
            OpacityBinding = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.Linear() }
        };

        var dab = dynamics.Evaluate(Sample(0.5), baseRadius: 5.0, baseOpacity: 1.0);

        Assert.Equal(5.0, dab.Radius, 5);
        Assert.Equal(0.5, dab.Opacity, 5);
    }

    [Fact]
    public void Evaluate_clamps_radius_to_minimum_floor()
    {
        var dynamics = new BrushDynamics
        {
            SizeBinding = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.Linear() }
        };

        // Pressure 0 → size mul 0 → would give radius 0, but the floor is 0.25.
        var dab = dynamics.Evaluate(Sample(0.0), baseRadius: 5.0);

        Assert.Equal(0.25, dab.Radius, 5);
    }

    [Fact]
    public void Evaluate_clamps_opacity_into_zero_to_one()
    {
        var dynamics = new BrushDynamics
        {
            OpacityBinding = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.Linear() }
        };

        var dabHi = dynamics.Evaluate(Sample(1.0), baseRadius: 5.0, baseOpacity: 1.5);
        var dabLo = dynamics.Evaluate(Sample(0.0), baseRadius: 5.0, baseOpacity: 0.0);

        Assert.Equal(1.0, dabHi.Opacity, 5);
        Assert.Equal(0.01, dabLo.Opacity, 5); // opacity floor is 0.01
    }

    [Fact]
    public void Pencil_preset_uses_easeIn_size_curve()
    {
        var pencil = BrushDynamics.Pencil();

        Assert.NotNull(pencil.SizeBinding);
        Assert.Equal(SensorKind.Pressure, pencil.SizeBinding!.Sensor);
        // EaseIn at 0.5 pressure stays below 0.3.
        Assert.True(pencil.SizeBinding.Evaluate(Sample(0.5)) < 0.3);
    }

    [Fact]
    public void Ink_preset_has_no_opacity_binding()
    {
        var ink = BrushDynamics.Ink();

        Assert.Null(ink.OpacityBinding);
        Assert.NotNull(ink.SizeBinding);
    }

    [Fact]
    public void Eraser_preset_has_no_opacity_binding()
    {
        var eraser = BrushDynamics.Eraser();

        Assert.Null(eraser.OpacityBinding);
        Assert.NotNull(eraser.SizeBinding);
    }

    [Fact]
    public void Paint_preset_has_both_size_and_opacity_bindings()
    {
        var paint = BrushDynamics.Paint();

        Assert.NotNull(paint.SizeBinding);
        Assert.NotNull(paint.OpacityBinding);
    }
}
