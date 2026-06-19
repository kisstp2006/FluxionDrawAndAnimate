using FluxionDrawAndAnimate.Core.Drawing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Drawing;

public class SensorBindingTests
{
    [Fact]
    public void ReadSensor_returns_pressure_directly()
    {
        var binding = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.Linear() };
        var info = PaintInformation.FromMouse(10, 10) with { Pressure = 0.7 };

        Assert.Equal(0.7, binding.ReadSensor(info), 5);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(0.0, 0.5)]
    [InlineData(1.0, 1.0)]
    public void ReadSensor_remaps_tiltX_from_negative_one_to_one_into_zero_to_one(double tilt, double expected)
    {
        var binding = new SensorBinding { Sensor = SensorKind.TiltX, Curve = ResponseCurve.Linear() };
        var info = PaintInformation.FromMouse(0, 0) with { TiltX = tilt };

        Assert.Equal(expected, binding.ReadSensor(info), 5);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(0.0, 0.5)]
    [InlineData(1.0, 1.0)]
    public void ReadSensor_remaps_tiltY_from_negative_one_to_one_into_zero_to_one(double tilt, double expected)
    {
        var binding = new SensorBinding { Sensor = SensorKind.TiltY, Curve = ResponseCurve.Linear() };
        var info = PaintInformation.FromMouse(0, 0) with { TiltY = tilt };

        Assert.Equal(expected, binding.ReadSensor(info), 5);
    }

    [Fact]
    public void Evaluate_applies_curve_on_top_of_sensor_value()
    {
        // EaseIn: at mid pressure, output is well below the input.
        var binding = new SensorBinding { Sensor = SensorKind.Pressure, Curve = ResponseCurve.EaseIn() };
        var info = PaintInformation.FromMouse(0, 0) with { Pressure = 0.5 };

        Assert.True(binding.Evaluate(info) < 0.3, $"EaseIn(0.5) should be < 0.3 but was {binding.Evaluate(info)}");
    }
}
