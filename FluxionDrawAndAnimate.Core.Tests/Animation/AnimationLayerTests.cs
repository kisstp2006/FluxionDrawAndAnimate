using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Animation;

public class AnimationLayerTests
{
    [Fact]
    public void Constructor_allocates_one_frame_per_frame_count()
    {
        var layer = new AnimationLayer("Layer", frameCount: 5);

        Assert.Equal(5, layer.Frames.Count);
        Assert.Equal("Layer", layer.Name);
    }

    [Fact]
    public void Frames_are_numbered_starting_at_one()
    {
        var layer = new AnimationLayer("Layer", frameCount: 3);

        Assert.Equal(1, layer.Frames[0].Number);
        Assert.Equal(2, layer.Frames[1].Number);
        Assert.Equal(3, layer.Frames[2].Number);
    }

    [Fact]
    public void New_layer_is_visible_and_unlocked()
    {
        var layer = new AnimationLayer("Layer", 1);

        Assert.True(layer.IsVisible);
        Assert.False(layer.IsLocked);
    }

    [Fact]
    public void EnsureFrame_returns_existing_frame_when_in_range()
    {
        var layer = new AnimationLayer("Layer", frameCount: 3);

        var frame = layer.EnsureFrame(1);

        Assert.Same(layer.Frames[1], frame);
    }

    [Fact]
    public void EnsureFrame_appends_frames_up_to_requested_index()
    {
        var layer = new AnimationLayer("Layer", frameCount: 1);

        var frame = layer.EnsureFrame(4);

        Assert.Equal(5, layer.Frames.Count);
        Assert.Same(layer.Frames[4], frame);
    }

    [Fact]
    public void EnsureFrame_continues_frame_numbering()
    {
        var layer = new AnimationLayer("Layer", frameCount: 2);

        layer.EnsureFrame(4);

        Assert.Equal(3, layer.Frames[2].Number);
        Assert.Equal(4, layer.Frames[3].Number);
        Assert.Equal(5, layer.Frames[4].Number);
    }

    [Fact]
    public void Name_is_observable()
    {
        var layer = new AnimationLayer("Old", 1);
        var changes = new List<string?>();
        layer.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AnimationLayer.Name))
            {
                changes.Add(layer.Name);
            }
        };

        layer.Name = "New";

        Assert.Equal("New", layer.Name);
        Assert.Single(changes);
        Assert.Equal("New", changes[0]);
    }

    [Fact]
    public void IsVisible_is_observable()
    {
        var layer = new AnimationLayer("L", 1);
        var fires = 0;
        layer.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AnimationLayer.IsVisible))
            {
                fires++;
            }
        };

        layer.IsVisible = false;

        Assert.Equal(1, fires);
        Assert.False(layer.IsVisible);
    }
}

public class AnimationFrameTests
{
    [Fact]
    public void HasDrawing_is_false_for_empty_frame()
    {
        var frame = new AnimationFrame(1);

        Assert.False(frame.HasDrawing);
    }

    [Fact]
    public void HasDrawing_is_true_after_a_stroke_is_added()
    {
        var frame = new AnimationFrame(1);
        frame.Strokes.Add(new StrokePath(RgbaColor.Black, 5, ToolKind.Pencil, false));

        Assert.True(frame.HasDrawing);
    }

    [Fact]
    public void Number_is_preserved()
    {
        var frame = new AnimationFrame(42);

        Assert.Equal(42, frame.Number);
    }
}
