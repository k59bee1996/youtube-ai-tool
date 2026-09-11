using YoutubeAiFactory.Application.Ideas;
using YoutubeAiFactory.Application.Opportunities;
using YoutubeAiFactory.Application.Pilots;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Pilots;
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

    Task<IReadOnlyList<CurrentCompetitorAnalysis>> GetCurrentCompetitorAnalysesForProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CurrentCompetitorAnalysis>>([]);

    Task<OpportunityReportWithDetails?> GetLatestOpportunityReportAsync(Guid projectId, CancellationToken cancellationToken) =>
        Task.FromResult<OpportunityReportWithDetails?>(null);

    Task<int> GetNextOpportunityReportVersionAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult(1);
    Task<Job?> GetActiveOpportunityAnalysisJobAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task<Job?> GetLatestOpportunityAnalysisJobAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task<Job?> TryClaimNextOpportunityAnalysisJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task<Job> EnqueueOpportunityAnalysisJobAsync(Job job, CancellationToken cancellationToken) => throw new NotSupportedException("Opportunity job persistence is not configured.");
    Task RequeueOpportunityAnalysisJobAsync(Guid jobId, CancellationToken cancellationToken) => throw new NotSupportedException("Opportunity job persistence is not configured.");
    Task FailOpportunityAnalysisJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable, DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken) => throw new NotSupportedException("Opportunity job persistence is not configured.");
    void AddOpportunityReport(OpportunityReport report) => throw new NotSupportedException("Opportunity persistence is not configured.");
    void AddOpportunityReportSource(OpportunityReportSource source) => throw new NotSupportedException("Opportunity persistence is not configured.");
    void AddOpportunityCandidate(OpportunityCandidate candidate) => throw new NotSupportedException("Opportunity persistence is not configured.");
    void AddOpportunityEvidence(OpportunityEvidence evidence) => throw new NotSupportedException("Opportunity persistence is not configured.");
    Task<ApprovedOpportunityWithEvidence?> GetOpportunityWithEvidenceAsync(Guid projectId, Guid opportunityId, bool forUpdate, CancellationToken cancellationToken) => Task.FromResult<ApprovedOpportunityWithEvidence?>(null);
    Task<IReadOnlyList<ExistingIdeaContext>> ListExistingIdeaContextAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ExistingIdeaContext>>([]);
    Task<IReadOnlyList<string>> ListCompetitorTitlesAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);
    Task<IReadOnlyList<IdeaGeneration>> ListIdeaGenerationsAsync(Guid projectId, Guid opportunityId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<IdeaGeneration>>([]);
    Task<IdeaGeneration?> GetIdeaGenerationAsync(Guid projectId, Guid generationId, CancellationToken cancellationToken) => Task.FromResult<IdeaGeneration?>(null);
    Task<IReadOnlyList<VideoIdeaWithEvidence>> ListVideoIdeasAsync(Guid projectId, Guid opportunityId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VideoIdeaWithEvidence>>([]);
    Task<VideoIdeaWithEvidence?> GetVideoIdeaAsync(Guid projectId, Guid ideaId, bool forUpdate, CancellationToken cancellationToken) => Task.FromResult<VideoIdeaWithEvidence?>(null);
    Task<int> GetNextIdeaGenerationVersionAsync(Guid opportunityId, CancellationToken cancellationToken) => Task.FromResult(1);
    Task<Job?> GetActiveIdeaGenerationJobAsync(Guid projectId, Guid opportunityId, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task<Job?> GetLatestIdeaGenerationJobAsync(Guid projectId, Guid opportunityId, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task<Job?> TryClaimNextIdeaGenerationJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task<Job> EnqueueIdeaGenerationJobAsync(Job job, CancellationToken cancellationToken) => throw new NotSupportedException("Idea generation job persistence is not configured.");
    Task RequeueIdeaGenerationJobAsync(Guid jobId, CancellationToken cancellationToken) => throw new NotSupportedException("Idea generation job persistence is not configured.");
    Task FailIdeaGenerationJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable, DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken) => throw new NotSupportedException("Idea generation job persistence is not configured.");
    void AddIdeaGeneration(IdeaGeneration generation) => throw new NotSupportedException("Idea persistence is not configured.");
    void AddVideoIdea(VideoIdea idea) => throw new NotSupportedException("Idea persistence is not configured.");
    void AddIdeaEvidence(IdeaEvidence evidence) => throw new NotSupportedException("Idea persistence is not configured.");

    Task<IReadOnlyList<PilotIdeaContext>> ListApprovedPilotIdeasAsync(Guid projectId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PilotIdeaContext>>([]);
    Task<IReadOnlyList<PilotIdeaContext>> ListPilotIdeaContextAsync(Guid projectId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PilotIdeaContext>>([]);
    Task<Pilot?> GetLatestPilotAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<Pilot?>(null);
    Task<Pilot?> GetPilotAsync(Guid projectId, Guid pilotId, bool forUpdate, CancellationToken cancellationToken) => Task.FromResult<Pilot?>(null);
    Task<IReadOnlyList<Pilot>> ListPilotsAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Pilot>>([]);
    Task<IReadOnlyList<PilotVideo>> ListPilotVideosAsync(Guid pilotId, bool forUpdate, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PilotVideo>>([]);
    Task<int> GetNextPilotVersionAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult(1);
    Task<Job?> GetActivePilotGenerationJobAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task<Job?> GetLatestPilotGenerationJobAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task<Job?> TryClaimNextPilotGenerationJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task<Job> EnqueuePilotGenerationJobAsync(Job job, CancellationToken cancellationToken) => throw new NotSupportedException("Pilot job persistence is not configured.");
    Task RequeuePilotGenerationJobAsync(Guid jobId, CancellationToken cancellationToken) => throw new NotSupportedException("Pilot job persistence is not configured.");
    Task FailPilotGenerationJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable, DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken) => throw new NotSupportedException("Pilot job persistence is not configured.");
    void AddPilot(Pilot pilot) => throw new NotSupportedException("Pilot persistence is not configured.");
    void AddPilotVideo(PilotVideo pilotVideo) => throw new NotSupportedException("Pilot persistence is not configured.");

    Task<Job?> GetActiveCompetitorAnalysisJobAsync(
        Guid projectId, Guid competitorId, CancellationToken cancellationToken) =>
        Task.FromResult<Job?>(null);

    Task<Job?> GetLatestCompetitorAnalysisJobAsync(
        Guid projectId, Guid competitorId, CancellationToken cancellationToken) =>
        Task.FromResult<Job?>(null);

    Task<Job?> TryClaimNextCompetitorAnalysisJobAsync(
        DateTimeOffset now, DateTimeOffset staleRunningBefore, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);

    Task<Job> EnqueueCompetitorAnalysisJobAsync(Job job, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Competitor analysis job persistence is not configured.");

    Task RequeueCompetitorAnalysisJobAsync(Guid jobId, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Competitor analysis job persistence is not configured.");

    Task FailCompetitorAnalysisJobAsync(
        Guid jobId,
        Guid? aiRunId,
        string reason,
        bool retryable,
        DateTimeOffset failedAt,
        DateTimeOffset? retryAt,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("Competitor analysis job persistence is not configured.");

    void AddCompetitorAnalysis(CompetitorAnalysis analysis) =>
        throw new NotSupportedException("Competitor analysis persistence is not configured.");

    void AddAiRun(AiRun aiRun) => throw new NotSupportedException("AI run persistence is not configured.");

    void AddJob(Job job) => throw new NotSupportedException("Job persistence is not configured.");

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
