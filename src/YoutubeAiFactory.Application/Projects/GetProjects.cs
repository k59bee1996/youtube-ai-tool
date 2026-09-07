using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;

namespace YoutubeAiFactory.Application.Projects;

public sealed class GetProjectHandler(IYoutubeAiFactoryStore store)
{
    public async Task<ProjectDto> HandleAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await store.GetProjectAsync(projectId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Project '{projectId}' was not found.");

        return ProjectDto.FromDomain(project);
    }
}

public sealed class ListProjectsHandler(IYoutubeAiFactoryStore store)
{
    public async Task<IReadOnlyList<ProjectDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var projects = await store.ListProjectsAsync(cancellationToken);
        return projects.Select(ProjectDto.FromDomain).ToArray();
    }
}
