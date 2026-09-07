using YoutubeAiFactory.Application.Persistence;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Application.Projects;

public sealed record CreateProjectCommand(
    string Name,
    string MarketName,
    string TargetLanguage,
    string TargetGeography,
    string AudienceDescription);

public sealed class CreateProjectHandler(
    IYoutubeAiFactoryStore store,
    TimeProvider timeProvider)
{
    public async Task<ProjectDto> HandleAsync(
        CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var project = new Project(
            command.Name,
            new Market(command.MarketName, command.TargetLanguage, command.TargetGeography),
            new AudienceProfile(command.AudienceDescription),
            timeProvider.GetUtcNow());

        store.AddProject(project);
        await store.SaveChangesAsync(cancellationToken);

        return ProjectDto.FromDomain(project);
    }
}
