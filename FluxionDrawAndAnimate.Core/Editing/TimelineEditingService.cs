using System.Linq;
using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Core.Editing;

public sealed class TimelineEditingService
{
    public void AddFrame(DrawingProject project)
    {
        project.FrameCount++;

        foreach (var layer in project.Layers)
        {
            layer.EnsureFrame(project.FrameCount - 1);
        }
    }

    public int DuplicateFrame(DrawingProject project, int layerIndex, int sourceFrameIndex)
    {
        var layer = project.Layers.ElementAtOrDefault(layerIndex);
        if (layer is null)
        {
            return sourceFrameIndex;
        }

        var source = layer.EnsureFrame(sourceFrameIndex);
        var targetIndex = Math.Min(project.FrameCount - 1, sourceFrameIndex + 1);
        var target = layer.EnsureFrame(targetIndex);

        // Snapshot the source strokes before mutating the target: if target and
        // source are the same frame (e.g. duplicating the last frame clamps back
        // onto itself), iterating source.Strokes while adding to it would throw
        // CollectionModified and produce runaway self-duplication.
        var sourceStrokes = source.Strokes.ToArray();
        foreach (var stroke in sourceStrokes)
        {
            target.Strokes.Add(CloneStroke(stroke));
        }

        return targetIndex;
    }

    private static StrokePath CloneStroke(StrokePath stroke)
    {
        // B/5: preserve the preset's BrushSettings, otherwise a duplicated frame
        // would silently fall back to ForTool() defaults and lose its brush character.
        var copy = new StrokePath(stroke.Color, stroke.Size, stroke.ToolKind, stroke.IsVector, stroke.BrushSettings);
        copy.Points.AddRange(stroke.Points);
        return copy;
    }
}
