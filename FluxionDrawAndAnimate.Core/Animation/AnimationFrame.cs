using System.Collections.ObjectModel;
using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Core.Animation;

public sealed class AnimationFrame
{
    public AnimationFrame(int number)
    {
        Number = number;
    }

    public int Number { get; }
    public ObservableCollection<StrokePath> Strokes { get; } = new();
    public bool HasDrawing => Strokes.Count > 0;
}
