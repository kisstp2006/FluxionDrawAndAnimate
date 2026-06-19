using FluxionDrawAndAnimate.Core.Drawing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Drawing;

public class RgbaColorTests
{
    [Fact]
    public void FromRgb_sets_alpha_to_fully_opaque()
    {
        var color = RgbaColor.FromRgb(10, 20, 30);

        Assert.Equal(10, color.R);
        Assert.Equal(20, color.G);
        Assert.Equal(30, color.B);
        Assert.Equal(255, color.A);
    }

    [Fact]
    public void FromArgb_sets_all_channels()
    {
        var color = RgbaColor.FromArgb(128, 10, 20, 30);

        Assert.Equal(128, color.A);
        Assert.Equal(10, color.R);
        Assert.Equal(20, color.G);
        Assert.Equal(30, color.B);
    }

    [Fact]
    public void White_and_Black_constants_are_correct()
    {
        Assert.Equal(RgbaColor.FromRgb(255, 255, 255), RgbaColor.White);
        Assert.Equal(RgbaColor.FromRgb(0, 0, 0), RgbaColor.Black);
    }

    [Fact]
    public void Equality_is_value_based()
    {
        var a = RgbaColor.FromArgb(100, 50, 60, 70);
        var b = RgbaColor.FromArgb(100, 50, 60, 70);
        var c = RgbaColor.FromArgb(100, 50, 60, 71);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}

public class PaintInformationTests
{
    [Fact]
    public void FromMouse_sets_full_pressure_zero_tilt_zero_rotation()
    {
        var info = PaintInformation.FromMouse(15, 25);

        Assert.Equal(15, info.X);
        Assert.Equal(25, info.Y);
        Assert.Equal(1.0, info.Pressure);
        Assert.Equal(0, info.TiltX);
        Assert.Equal(0, info.TiltY);
        Assert.Equal(0, info.Rotation);
    }

    [Fact]
    public void With_expression_overrides_a_single_field()
    {
        var info = PaintInformation.FromMouse(0, 0) with { Pressure = 0.5 };

        Assert.Equal(0.5, info.Pressure);
        Assert.Equal(1.0, PaintInformation.FromMouse(0, 0).Pressure); // original unchanged
    }
}
