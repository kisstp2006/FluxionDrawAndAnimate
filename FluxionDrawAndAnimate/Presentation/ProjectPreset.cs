namespace FluxionDrawAndAnimate.Presentation;

public sealed class ProjectPreset
{
    public ProjectPreset(string name, string subtitle, int width, int height, int framesPerSecond, int frameCount)
    {
        Name = name;
        Subtitle = subtitle;
        Width = width;
        Height = height;
        FramesPerSecond = framesPerSecond;
        FrameCount = frameCount;
    }

    public string Name { get; }
    public string Subtitle { get; }
    public int Width { get; }
    public int Height { get; }
    public int FramesPerSecond { get; }
    public int FrameCount { get; }
    public string SizeLabel => $"{Width} x {Height}";
    public string TimingLabel => $"{FrameCount} frames · {FramesPerSecond} fps";
}
