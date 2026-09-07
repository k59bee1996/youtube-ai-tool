using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Application.Persistence;

public interface IYoutubeAiFactoryStore
{
    Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken);

    Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Project>> ListProjectsAsync(CancellationToken cancellationToken);

    void AddProject(Project project);

    Task<IReadOnlyList<CompetitorChannel>> ListCompetitorsAsync(
        Guid projectId,
        CancellationToken cancellationToken);

    Task<CompetitorChannel?> GetCompetitorAsync(
        Guid projectId,
        Guid competitorId,
        bool forUpdate,
        CancellationToken cancellationToken);

    Task<CompetitorChannel?> FindCompetitorByYoutubeChannelIdAsync(
        Guid projectId,
        string youtubeChannelId,
        CancellationToken cancellationToken);

    void AddCompetitor(CompetitorChannel competitor);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
