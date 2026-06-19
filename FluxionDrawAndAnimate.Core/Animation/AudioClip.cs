namespace FluxionDrawAndAnimate.Core.Animation;

public sealed class AudioClip
{
    public AudioClip(string name, int startFrame, int durationFrames)
    {
        Name = name;
        StartFrame = startFrame;
        DurationFrames = durationFrames;
    }

    public string Name { get; }
    public int StartFrame { get; }
    public int DurationFrames { get; }
}
