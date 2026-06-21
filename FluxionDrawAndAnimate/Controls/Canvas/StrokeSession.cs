using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Drawing;

namespace FluxionDrawAndAnimate.Controls.Canvas;

/// <summary>
/// Owns the currently active stroke transaction for the canvas input loop.
/// </summary>
internal sealed class StrokeSession
{
    public StrokePath? Stroke { get; private set; }
    public AnimationFrame? Frame { get; private set; }
    public int FrameIndex { get; private set; }
    public bool IsActive => Stroke is not null && Frame is not null;

    public void Begin(AnimationFrame frame, int frameIndex, StrokePath stroke)
    {
        Frame = frame;
        FrameIndex = frameIndex;
        Stroke = stroke;
    }

    private void Cancel()
    {
        Stroke = null;
        Frame = null;
        FrameIndex = 0;
    }

    public bool TryEnd(out StrokePath stroke, out AnimationFrame frame, out int frameIndex)
    {
        if (Stroke is null || Frame is null)
        {
            stroke = null!;
            frame = null!;
            frameIndex = 0;
            return false;
        }

        stroke = Stroke;
        frame = Frame;
        frameIndex = FrameIndex;
        Cancel();
        return true;
    }
}
