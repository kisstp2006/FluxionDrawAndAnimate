using FluxionDrawAndAnimate.Core.Animation;

namespace FluxionDrawAndAnimate.Core.Projects;

public sealed class DefaultProjectFactory : IProjectFactory
{
    public DrawingProject CreateNewProject(ProjectCreationOptions? options = null)
    {
        options ??= new ProjectCreationOptions();

        var project = new DrawingProject
        {
            Name = string.IsNullOrWhiteSpace(options.Name) ? "Untitled" : options.Name.Trim(),
            Width = Math.Max(1, options.Width),
            Height = Math.Max(1, options.Height),
            FrameCount = Math.Max(1, options.FrameCount),
            FramesPerSecond = Math.Max(1, options.FramesPerSecond)
        };

        project.Layers.Add(new AnimationLayer("Layer 1", project.FrameCount));

        return project;
    }
}
