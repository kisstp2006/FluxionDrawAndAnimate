using FluxionDrawAndAnimate.Core.Drawing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Drawing;

public class StrokeBoundsTests
{
    [Fact]
    public void Calculate_returns_non_empty_bounds_for_single_point_stroke()
    {
        var stroke = new StrokePath(RgbaColor.Black, 10, ToolKind.Pencil, false);
        stroke.Points.Add(new PaintInformation(32, 32, 1, 0, 0, 0, 0));

        var bounds = StrokeBounds.Calculate(stroke);

        Assert.False(bounds.IsEmpty);
        Assert.True(bounds.X < 32);
        Assert.True(bounds.Y < 32);
        Assert.True(bounds.Right > 32);
        Assert.True(bounds.Bottom > 32);
    }

    [Fact]
    public void CalculateRange_returns_only_the_new_segment_area()
    {
        var stroke = new StrokePath(RgbaColor.Black, 10, ToolKind.Pencil, false);
        stroke.Points.Add(new PaintInformation(10, 10, 1, 0, 0, 0, 0));
        stroke.Points.Add(new PaintInformation(20, 10, 1, 0, 0, 0, 0));
        stroke.Points.Add(new PaintInformation(110, 110, 1, 0, 0, 0, 0));

        var bounds = StrokeBounds.CalculateRange(stroke, firstPointIndex: 2);

        Assert.True(bounds.X > 10);
        Assert.True(bounds.Y < 110);
        Assert.True(bounds.Right > 110);
        Assert.True(bounds.Bottom > 110);
    }
}
