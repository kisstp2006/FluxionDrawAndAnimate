using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Editing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Editing;

public class TimelineEditingServiceTests
{
    private static DrawingProject NewProject(int frameCount = 4)
    {
        var project = new DrawingProject { Name = "Test", Width = 100, Height = 100, FrameCount = frameCount };
        project.Layers.Add(new AnimationLayer("Layer 1", frameCount));
        return project;
    }

    [Fact]
    public void AddFrame_increases_frame_count_and_extends_every_layer()
    {
        var project = NewProject(frameCount: 4);
        var service = new TimelineEditingService();

        service.AddFrame(project);

        Assert.Equal(5, project.FrameCount);
        Assert.Equal(5, project.Layers[0].Frames.Count);
    }

    [Fact]
    public void DuplicateFrame_copies_strokes_into_next_frame()
    {
        var project = NewProject(frameCount: 4);
        var service = new TimelineEditingService();
        var source = project.Layers[0].EnsureFrame(0);
        source.Strokes.Add(new StrokePath(RgbaColor.Black, 10, ToolKind.Pencil, false));

        var targetIndex = service.DuplicateFrame(project, layerIndex: 0, sourceFrameIndex: 0);

        Assert.Equal(1, targetIndex);
        var target = project.Layers[0].Frames[1];
        Assert.Single(target.Strokes);
    }

    [Fact]
    public void DuplicateFrame_clones_strokes_so_original_is_not_aliased()
    {
        var project = NewProject(frameCount: 4);
        var service = new TimelineEditingService();
        var source = project.Layers[0].EnsureFrame(0);
        var original = new StrokePath(RgbaColor.Black, 10, ToolKind.Pencil, false);
        source.Strokes.Add(original);

        service.DuplicateFrame(project, 0, 0);

        var copy = project.Layers[0].Frames[1].Strokes[0];
        Assert.NotSame(original, copy);
        // Mutating the copy must not affect the original.
        copy.Points.Add(PaintInformation.FromMouse(99, 99));
        Assert.Empty(original.Points);
    }

    [Fact]
    public void DuplicateFrame_preserves_brush_settings_on_cloned_strokes()
    {
        // B/5 regression guard: the clone must carry the preset's BrushSettings,
        // not fall back to ForTool() defaults.
        var project = NewProject(frameCount: 4);
        var service = new TimelineEditingService();
        var source = project.Layers[0].EnsureFrame(0);
        var settings = new BrushSettings { Shape = BrushShape.Chisel, Hardness = 0.42 };
        source.Strokes.Add(new StrokePath(RgbaColor.Black, 10, ToolKind.Vector, true, settings));

        service.DuplicateFrame(project, 0, 0);

        var copy = project.Layers[0].Frames[1].Strokes[0];
        Assert.Equal(BrushShape.Chisel, copy.BrushSettings.Shape);
        Assert.Equal(0.42, copy.BrushSettings.Hardness, 5);
    }

    [Fact]
    public void DuplicateFrame_returns_source_index_when_layer_missing()
    {
        var project = NewProject(frameCount: 4);
        var service = new TimelineEditingService();

        var result = service.DuplicateFrame(project, layerIndex: 99, sourceFrameIndex: 2);

        Assert.Equal(2, result);
    }

    [Fact]
    public void DuplicateFrame_clamps_target_index_to_frame_count()
    {
        var project = NewProject(frameCount: 4);
        var service = new TimelineEditingService();
        // Source is the last frame → target clamps back to the last frame, which
        // is the same object as the source. The snapshot-then-add path must not
        // throw CollectionModified and must not run away self-duplicating.
        var source = project.Layers[0].EnsureFrame(3);
        source.Strokes.Add(new StrokePath(RgbaColor.Black, 5, ToolKind.Pencil, false));

        var targetIndex = service.DuplicateFrame(project, 0, 3);

        Assert.Equal(3, targetIndex); // clamped to last valid index
        // The single source stroke was snapshotted once and added once → 2 strokes total.
        Assert.Equal(2, project.Layers[0].Frames[3].Strokes.Count);
    }
}
