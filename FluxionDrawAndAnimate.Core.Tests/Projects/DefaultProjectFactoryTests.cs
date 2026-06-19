using FluxionDrawAndAnimate.Core.Animation;
using FluxionDrawAndAnimate.Core.Projects;
using Xunit;

namespace FluxionDrawAndAnimate.Core.Tests.Projects;

public class DefaultProjectFactoryTests
{
    [Fact]
    public void CreateNewProject_with_no_options_uses_sane_defaults()
    {
        var factory = new DefaultProjectFactory();

        var project = factory.CreateNewProject();

        Assert.False(string.IsNullOrWhiteSpace(project.Name));
        Assert.True(project.Width > 0);
        Assert.True(project.Height > 0);
        Assert.True(project.FrameCount > 0);
        Assert.True(project.FramesPerSecond > 0);
    }

    [Fact]
    public void CreateNewProject_applies_options()
    {
        var factory = new DefaultProjectFactory();
        var options = new ProjectCreationOptions
        {
            Name = "My Animation",
            Width = 1280,
            Height = 720,
            FrameCount = 24,
            FramesPerSecond = 30
        };

        var project = factory.CreateNewProject(options);

        Assert.Equal("My Animation", project.Name);
        Assert.Equal(1280, project.Width);
        Assert.Equal(720, project.Height);
        Assert.Equal(24, project.FrameCount);
        Assert.Equal(30, project.FramesPerSecond);
    }

    [Fact]
    public void CreateNewProject_clamps_invalid_dimensions_to_minimum_one()
    {
        var factory = new DefaultProjectFactory();
        var options = new ProjectCreationOptions
        {
            Name = "Bad",
            Width = -100,
            Height = 0,
            FrameCount = -5,
            FramesPerSecond = 0
        };

        var project = factory.CreateNewProject(options);

        Assert.Equal(1, project.Width);
        Assert.Equal(1, project.Height);
        Assert.Equal(1, project.FrameCount);
        Assert.Equal(1, project.FramesPerSecond);
    }

    [Fact]
    public void CreateNewProject_trims_whitespace_from_name()
    {
        var factory = new DefaultProjectFactory();
        var options = new ProjectCreationOptions { Name = "  Spaced  " };

        var project = factory.CreateNewProject(options);

        Assert.Equal("Spaced", project.Name);
    }

    [Fact]
    public void CreateNewProject_falls_back_to_untitled_when_name_is_blank()
    {
        var factory = new DefaultProjectFactory();
        var options = new ProjectCreationOptions { Name = "   " };

        var project = factory.CreateNewProject(options);

        Assert.Equal("Untitled", project.Name);
    }

    [Fact]
    public void CreateNewProject_creates_one_layer_with_full_frame_count()
    {
        var factory = new DefaultProjectFactory();
        var options = new ProjectCreationOptions { FrameCount = 12 };

        var project = factory.CreateNewProject(options);

        Assert.Single(project.Layers);
        Assert.Equal(12, project.Layers[0].Frames.Count);
    }
}
