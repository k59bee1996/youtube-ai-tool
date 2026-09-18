using YoutubeAiFactory.Application.Ideas;
using YoutubeAiFactory.Application.Opportunities;
using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Application.Pilots;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Application.Scripts;
using YoutubeAiFactory.Application.Videos;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Competitors;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Localization;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Projects;
using YoutubeAiFactory.Domain.Research;
using YoutubeAiFactory.Domain.Scripts;
using YoutubeAiFactory.Domain.Videos;

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

    Task<CompetitorAnalysis?> GetCompetitorAnalysisAsync(Guid projectId, Guid analysisId, CancellationToken cancellationToken) =>
        Task.FromResult<CompetitorAnalysis?>(null);

    Task<ArtifactLocalization?> GetArtifactLocalizationAsync(string artifactType, Guid artifactId, int artifactVersion, string locale, CancellationToken cancellationToken) =>
        Task.FromResult<ArtifactLocalization?>(null);
    Task<Job?> GetActiveArtifactLocalizationJobAsync(string artifactType, Guid artifactId, int artifactVersion, string locale, CancellationToken cancellationToken) =>
        Task.FromResult<Job?>(null);
    Task<Job?> GetLatestArtifactLocalizationJobAsync(string artifactType, Guid artifactId, int artifactVersion, string locale, CancellationToken cancellationToken) =>
        Task.FromResult<Job?>(null);
    Task<Job?> TryClaimNextArtifactLocalizationJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore, CancellationToken cancellationToken) =>
        Task.FromResult<Job?>(null);
    Task<Job> EnqueueArtifactLocalizationJobAsync(Job job, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Artifact localization job persistence is not configured.");
    Task RequeueArtifactLocalizationJobAsync(Guid jobId, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Artifact localization job persistence is not configured.");
    Task FailArtifactLocalizationJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable, DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Artifact localization job persistence is not configured.");
    void AddArtifactLocalization(ArtifactLocalization localization) =>
        throw new NotSupportedException("Artifact localization persistence is not configured.");
    Task DeleteArtifactLocalizationAsync(Guid localizationId, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Artifact localization persistence is not configured.");

    Task<IReadOnlyList<CurrentCompetitorAnalysis>> GetCurrentCompetitorAnalysesForProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CurrentCompetitorAnalysis>>([]);

    Task<OpportunityReportWithDetails?> GetLatestOpportunityReportAsync(Guid projectId, CancellationToken cancellationToken) =>
        Task.FromResult<OpportunityReportWithDetails?>(null);
    Task<OpportunityReportWithDetails?> GetOpportunityReportAsync(Guid projectId, Guid reportId, CancellationToken cancellationToken) =>
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

    Task<VideoProject?> GetVideoProjectByPilotVideoAsync(Guid projectId, Guid pilotVideoId, CancellationToken cancellationToken) => Task.FromResult<VideoProject?>(null);
    Task<VideoProject?> GetVideoProjectAsync(Guid projectId, Guid videoProjectId, bool forUpdate, CancellationToken cancellationToken) => Task.FromResult<VideoProject?>(null);
    Task<IReadOnlyList<VideoProject>> ListVideoProjectsAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VideoProject>>([]);
    Task<VideoProjectSource?> GetVideoProjectSourceAsync(Guid projectId, Guid pilotId, Guid pilotVideoId, CancellationToken cancellationToken) => Task.FromResult<VideoProjectSource?>(null);
    Task<VideoProject> CreateVideoProjectIfAbsentAsync(VideoProject project, CancellationToken cancellationToken) => throw new NotSupportedException("Video project persistence is not configured.");

    Task<Job?> GetActiveVideoResearchJobAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task<Job?> GetLatestVideoResearchJobAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task<Job> EnqueueVideoResearchJobAsync(Job job, ResearchRun run, CancellationToken cancellationToken) => throw new NotSupportedException("Video research job persistence is not configured.");
    Task<Job?> TryClaimNextVideoResearchJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore, CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task RequeueVideoResearchJobAsync(Guid jobId, CancellationToken cancellationToken) => throw new NotSupportedException("Video research job persistence is not configured.");
    Task FailVideoResearchJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable, DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken) => throw new NotSupportedException("Video research job persistence is not configured.");
    Task<ResearchRun?> GetResearchRunAsync(Guid projectId, Guid videoProjectId, Guid researchRunId, bool forUpdate, CancellationToken cancellationToken) => Task.FromResult<ResearchRun?>(null);
    Task<ResearchRun?> GetLatestResearchRunAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) => Task.FromResult<ResearchRun?>(null);
    Task<IReadOnlyList<ResearchRun>> ListResearchRunsAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ResearchRun>>([]);
    Task<int> GetNextResearchReportVersionAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) => Task.FromResult(1);
    Task<ResearchReportWithDetails?> GetLatestResearchReportAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) => Task.FromResult<ResearchReportWithDetails?>(null);
    Task<ResearchReportWithDetails?> GetResearchReportAsync(Guid projectId, Guid videoProjectId, Guid reportId, CancellationToken cancellationToken) => Task.FromResult<ResearchReportWithDetails?>(null);
    Task<IReadOnlyList<ResearchReport>> ListResearchReportsAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ResearchReport>>([]);
    void AddResearchSource(ResearchSource source) => throw new NotSupportedException("Research persistence is not configured.");
    void AddResearchEvidence(ResearchEvidence evidence) => throw new NotSupportedException("Research persistence is not configured.");
    void AddResearchReport(ResearchReport report) => throw new NotSupportedException("Research persistence is not configured.");
    void AddResearchClaim(ResearchClaim claim) => throw new NotSupportedException("Research persistence is not configured.");
    void AddResearchClaimEvidence(ResearchClaimEvidence claimEvidence) => throw new NotSupportedException("Research persistence is not configured.");
    void AddResearchConflict(ResearchConflict conflict) => throw new NotSupportedException("Research persistence is not configured.");

    Task<Job?> GetActiveVideoOutlineJobAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
        Task.FromResult<Job?>(null);
    Task<Job?> GetLatestVideoOutlineJobAsync(Guid projectId, Guid videoProjectId, CancellationToken cancellationToken) =>
        Task.FromResult<Job?>(null);
    Task<Job> EnqueueVideoOutlineJobAsync(Job job, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Video outline job persistence is not configured.");
    Task<Job?> TryClaimNextVideoOutlineJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore,
        CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task RequeueVideoOutlineJobAsync(Guid jobId, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Video outline job persistence is not configured.");
    Task FailVideoOutlineJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable,
        DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Video outline job persistence is not configured.");
    Task<bool> CompleteVideoOutlineJobAsync(Guid jobId, Guid leaseId, DateTimeOffset completedAt,
        CancellationToken cancellationToken) => Task.FromResult(false);
    Task<int> GetNextVideoOutlineVersionAsync(Guid projectId, Guid videoProjectId,
        CancellationToken cancellationToken) => Task.FromResult(1);
    Task<VideoOutlineWithDetails?> GetLatestVideoOutlineAsync(Guid projectId, Guid videoProjectId,
        bool forUpdate, CancellationToken cancellationToken) => Task.FromResult<VideoOutlineWithDetails?>(null);
    Task<VideoOutlineWithDetails?> GetVideoOutlineAsync(Guid projectId, Guid videoProjectId, Guid outlineId,
        bool forUpdate, CancellationToken cancellationToken) => Task.FromResult<VideoOutlineWithDetails?>(null);
    Task<VideoOutlineWithDetails?> GetApprovedVideoOutlineAsync(Guid projectId, Guid videoProjectId,
        CancellationToken cancellationToken) => Task.FromResult<VideoOutlineWithDetails?>(null);
    Task<IReadOnlyList<VideoOutline>> ListVideoOutlinesAsync(Guid projectId, Guid videoProjectId,
        CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VideoOutline>>([]);
    void AddVideoOutline(VideoOutline outline) => throw new NotSupportedException("Video outline persistence is not configured.");
    void AddVideoOutlineSection(VideoOutlineSection section) => throw new NotSupportedException("Video outline persistence is not configured.");
    void AddVideoOutlineSectionClaim(VideoOutlineSectionClaim claim) => throw new NotSupportedException("Video outline persistence is not configured.");
    void AddVideoOutlineSectionConflict(VideoOutlineSectionConflict conflict) => throw new NotSupportedException("Video outline persistence is not configured.");
    void AddVideoOutlineSectionGap(VideoOutlineSectionGap gap) => throw new NotSupportedException("Video outline persistence is not configured.");
    Task ReorderVideoOutlineSectionsAsync(VideoOutline outline, IReadOnlyList<Guid> orderedSectionIds,
        CancellationToken cancellationToken) => throw new NotSupportedException("Video outline persistence is not configured.");

    Task<Job?> GetActiveVideoScriptJobAsync(Guid projectId, Guid videoProjectId,
        CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task<Job?> GetLatestVideoScriptJobAsync(Guid projectId, Guid videoProjectId,
        CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task<Job> EnqueueVideoScriptJobAsync(Job job, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Video Script job persistence is not configured.");
    Task<Job?> TryClaimNextVideoScriptJobAsync(DateTimeOffset now, DateTimeOffset staleRunningBefore,
        CancellationToken cancellationToken) => Task.FromResult<Job?>(null);
    Task RequeueVideoScriptJobAsync(Guid jobId, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Video Script job persistence is not configured.");
    Task FailVideoScriptJobAsync(Guid jobId, Guid? aiRunId, string reason, bool retryable,
        DateTimeOffset failedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Video Script job persistence is not configured.");
    Task<bool> CompleteVideoScriptJobAsync(Guid jobId, Guid leaseId, DateTimeOffset completedAt,
        CancellationToken cancellationToken) => Task.FromResult(false);
    Task<int> GetNextVideoScriptVersionAsync(Guid projectId, Guid videoProjectId,
        CancellationToken cancellationToken) => Task.FromResult(1);
    Task<VideoScriptWithDetails?> GetLatestVideoScriptAsync(Guid projectId, Guid videoProjectId,
        bool forUpdate, CancellationToken cancellationToken) => Task.FromResult<VideoScriptWithDetails?>(null);
    Task<VideoScriptWithDetails?> GetVideoScriptAsync(Guid projectId, Guid videoProjectId, Guid scriptId,
        bool forUpdate, CancellationToken cancellationToken) => Task.FromResult<VideoScriptWithDetails?>(null);
    Task<VideoScriptWithDetails?> GetApprovedVideoScriptAsync(Guid projectId, Guid videoProjectId,
        CancellationToken cancellationToken) => Task.FromResult<VideoScriptWithDetails?>(null);
    Task<IReadOnlyList<VideoScript>> ListVideoScriptsAsync(Guid projectId, Guid videoProjectId,
        CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VideoScript>>([]);
    void AddVideoScript(VideoScript script) => throw new NotSupportedException("Video Script persistence is not configured.");
    void AddVideoScriptSection(VideoScriptSection section) => throw new NotSupportedException("Video Script persistence is not configured.");
    void AddVideoScriptBlock(VideoScriptBlock block) => throw new NotSupportedException("Video Script persistence is not configured.");
    void AddVideoScriptBlockClaim(VideoScriptBlockClaim claim) => throw new NotSupportedException("Video Script persistence is not configured.");
    void AddVideoScriptBlockConflict(VideoScriptBlockConflict conflict) => throw new NotSupportedException("Video Script persistence is not configured.");

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

public interface IVideoResearchJobLeaseRenewer
{
    Task<bool> RenewAsync(Guid jobId, Guid leaseId, DateTimeOffset renewedAt, CancellationToken cancellationToken);
}

public interface IVideoOutlineJobLeaseRenewer
{
    Task<bool> RenewAsync(Guid jobId, Guid leaseId, DateTimeOffset renewedAt, CancellationToken cancellationToken);
}

public interface IVideoScriptJobLeaseRenewer
{
    Task<bool> RenewAsync(Guid jobId, Guid leaseId, DateTimeOffset renewedAt,
        CancellationToken cancellationToken);
}
