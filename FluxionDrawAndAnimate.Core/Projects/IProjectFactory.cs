using FluxionDrawAndAnimate.Core.Animation;

namespace FluxionDrawAndAnimate.Core.Projects;

public interface IProjectFactory
{
    DrawingProject CreateNewProject(ProjectCreationOptions? options = null);
}
