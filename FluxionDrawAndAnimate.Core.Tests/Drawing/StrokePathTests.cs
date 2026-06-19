using FluxionDrawAndAnimate.Core.Drawing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Drawing;

public class StrokePathTests
{
    [Fact]
    public void Constructor_with_brush_settings_stores_them()
    {
        var settings = new BrushSettings { Shape = BrushShape.Chisel, Hardness = 0.42 };

        var stroke = new StrokePath(
            color: RgbaColor.Black,
            size: 12,
            toolKind: ToolKind.Vector,
            isVector: true,
            brushSettings: settings);

        Assert.Equal(BrushShape.Chisel, stroke.BrushSettings.Shape);
        Assert.Equal(0.42, stroke.BrushSettings.Hardness, 5);
    }

    [Fact]
    public void Constructor_without_brush_settings_falls_back_to_for_tool()
    {
        var stroke = new StrokePath(RgbaColor.Black, 8, ToolKind.Ink, isVector: false);

        Assert.Equal(BrushShape.HardRound, stroke.BrushSettings.Shape);
        Assert.Equal(1.0, stroke.BrushSettings.Hardness, 5);
    }

    [Fact]
    public void Stroke_carries_color_size_and_tool_kind()
    {
        var stroke = new StrokePath(RgbaColor.FromRgb(10, 20, 30), 7, ToolKind.Paint, isVector: false);

        Assert.Equal(RgbaColor.FromRgb(10, 20, 30), stroke.Color);
        Assert.Equal(7, stroke.Size);
        Assert.Equal(ToolKind.Paint, stroke.ToolKind);
        Assert.False(stroke.IsVector);
    }

    [Fact]
    public void Points_list_starts_empty_and_accepts_samples()
    {
        var stroke = new StrokePath(RgbaColor.Black, 5, ToolKind.Pencil, false);

        Assert.Empty(stroke.Points);

        stroke.Points.Add(PaintInformation.FromMouse(1, 2));
        Assert.Single(stroke.Points);
        Assert.Equal(1, stroke.Points[0].X);
    }
}
