using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Core.Editing;

/// <summary>
/// Records a single stroke addition. Undo removes the stroke and triggers a
/// dirty-tile re-rasterization (tile surface is a cache; strokes are the truth).
/// Redo re-inserts the stroke at its original position.
/// The <paramref name="onChanged"/> callback handles cache invalidation and
/// visual refresh — kept as an Action so the command stays Avalonia-free.
/// </summary>
public sealed class AddStrokeCommand : IUndoableCommand
{
    private readonly AnimationFrame _frame;
    private readonly StrokePath _stroke;
    private readonly int _strokeIndex;
    private readonly Action<StrokePath> _onChanged;

    public AddStrokeCommand(AnimationFrame frame, StrokePath stroke, Action<StrokePath> onChanged)
    {
        _frame = frame;
        _stroke = stroke;
        _strokeIndex = frame.Strokes.IndexOf(stroke);
        _onChanged = onChanged;
    }

    public void Undo()
    {
        _frame.Strokes.Remove(_stroke);
        _onChanged(_stroke);
    }

    public void Redo()
    {
        var index = Math.Min(_strokeIndex, _frame.Strokes.Count);
        _frame.Strokes.Insert(index, _stroke);
        _onChanged(_stroke);
    }
}
