using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;
using FluxionDrawAndAnimate.Core.Editing;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Editing;

public class AddStrokeCommandTests
{
    private static AnimationFrame NewFrame() => new(1);

    private static StrokePath NewStroke() =>
        new(RgbaColor.Black, 10, ToolKind.Pencil, false);

    [Fact]
    public void Constructor_captures_current_stroke_index()
    {
        var frame = NewFrame();
        var stroke = NewStroke();
        frame.Strokes.Add(stroke);

        var cmd = new AddStrokeCommand(frame, stroke, _ => { });

        // Command was constructed after the stroke was added, so index is 0.
        // Undo should remove it.
        cmd.Undo();
        Assert.Empty(frame.Strokes);
    }

    [Fact]
    public void Undo_removes_the_stroke_from_the_frame()
    {
        var frame = NewFrame();
        var stroke = NewStroke();
        frame.Strokes.Add(stroke);
        var cmd = new AddStrokeCommand(frame, stroke, _ => { });

        cmd.Undo();

        Assert.Empty(frame.Strokes);
    }

    [Fact]
    public void Redo_reinserts_the_stroke_at_its_original_position()
    {
        var frame = NewFrame();
        var first = NewStroke();
        var second = NewStroke();
        frame.Strokes.Add(first);
        frame.Strokes.Add(second);
        var cmd = new AddStrokeCommand(frame, second, _ => { });

        cmd.Undo();
        Assert.Single(frame.Strokes);
        Assert.Same(first, frame.Strokes[0]);

        cmd.Redo();
        Assert.Equal(2, frame.Strokes.Count);
        // Second stroke should be back at index 1.
        Assert.Same(second, frame.Strokes[1]);
    }

    [Fact]
    public void Undo_invokes_change_callback()
    {
        var frame = NewFrame();
        var stroke = NewStroke();
        frame.Strokes.Add(stroke);

        var calls = 0;
        var cmd = new AddStrokeCommand(frame, stroke, _ => calls++);

        cmd.Undo();
        cmd.Redo();

        Assert.Equal(2, calls);
    }

    [Fact]
    public void Redo_clamps_index_when_other_strokes_were_removed_in_between()
    {
        var frame = NewFrame();
        var stroke = NewStroke();
        frame.Strokes.Add(stroke);
        var cmd = new AddStrokeCommand(frame, stroke, _ => { });

        cmd.Undo();
        // Simulate some other edit clearing the frame entirely.
        frame.Strokes.Clear();

        // Redo must not throw even though the original index is out of range.
        cmd.Redo();
        Assert.Single(frame.Strokes);
        Assert.Same(stroke, frame.Strokes[0]);
    }
}
