using FluxionDrawAndAnimate.Core.Drawing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Drawing;

public class BrushSettingsTests
{
    [Fact]
    public void ForTool_returns_pencil_settings_with_round_shape()
    {
        var settings = BrushSettings.ForTool(ToolKind.Pencil);

        Assert.Equal(BrushShape.Round, settings.Shape);
        Assert.NotNull(settings.Dynamics);
    }

    [Fact]
    public void ForTool_returns_ink_settings_with_hard_round_shape()
    {
        var settings = BrushSettings.ForTool(ToolKind.Ink);

        Assert.Equal(BrushShape.HardRound, settings.Shape);
        Assert.Equal(1.0, settings.Hardness, 5);
    }

    [Fact]
    public void ForTool_returns_paint_settings_with_soft_hardness()
    {
        var settings = BrushSettings.ForTool(ToolKind.Paint);

        Assert.True(settings.Hardness < 0.5, $"Paint hardness {settings.Hardness} should be soft (<0.5)");
    }

    [Fact]
    public void ForTool_returns_chisel_shape_for_vector_tool()
    {
        var settings = BrushSettings.ForTool(ToolKind.Vector);

        Assert.Equal(BrushShape.Chisel, settings.Shape);
    }

    [Fact]
    public void ForTool_returns_default_for_unmapped_tool()
    {
        var settings = BrushSettings.ForTool(ToolKind.Select);

        Assert.Equal(BrushShape.Round, settings.Shape);
        Assert.Equal(0.8, settings.Hardness, 5);
    }

    [Fact]
    public void ForTool_pencil_dynamics_use_easeIn_size_curve()
    {
        var settings = BrushSettings.ForTool(ToolKind.Pencil);

        Assert.NotNull(settings.Dynamics.SizeBinding);
        // EaseIn at 0.5 pressure stays below 0.3.
        var sample = PaintInformation.FromMouse(0, 0) with { Pressure = 0.5 };
        Assert.True(settings.Dynamics.SizeBinding!.Evaluate(sample) < 0.3);
    }
}
