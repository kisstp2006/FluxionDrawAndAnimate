namespace FluxionDrawAndAnimate.Core.Projects;

public sealed class ProjectCreationOptions
{
    public string Name { get; set; } = "Untitled";
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int FrameCount { get; set; } = 24;
    public int FramesPerSecond { get; set; } = 24;
}
