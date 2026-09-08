using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Jobs;
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

    Task<CompetitorAnalysis?> GetLatestCompetitorAnalysisAsync(
        Guid projectId, Guid competitorId, CancellationToken cancellationToken) =>
        Task.FromResult<CompetitorAnalysis?>(null);

    Task<int> GetNextCompetitorAnalysisVersionAsync(
        Guid competitorId, CancellationToken cancellationToken) => Task.FromResult(1);

    Task<Job?> GetActiveCompetitorAnalysisJobAsync(
        Guid projectId, Guid competitorId, CancellationToken cancellationToken) =>
        Task.FromResult<Job?>(null);

    Task<Job?> GetLatestCompetitorAnalysisJobAsync(
        Guid projectId, Guid competitorId, CancellationToken cancellationToken) =>
        Task.FromResult<Job?>(null);

    Task<Job?> TryClaimNextCompetitorAnalysisJobAsync(
        DateTimeOffset now, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);

    void AddCompetitorAnalysis(CompetitorAnalysis analysis) =>
        throw new NotSupportedException("Competitor analysis persistence is not configured.");

    void AddAiRun(AiRun aiRun) => throw new NotSupportedException("AI run persistence is not configured.");

    void AddJob(Job job) => throw new NotSupportedException("Job persistence is not configured.");

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
