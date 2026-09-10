using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;

namespace YoutubeAiFactory.Application.Ideas;

public sealed record IdeaGenerationJobPayload(Guid ProjectId, Guid OpportunityId);
public sealed record IdeaEvidenceContext(Guid OpportunityEvidenceId, string Summary);
public sealed record ExistingIdeaContext(Guid Id, string WorkingTitle, string Topic, string Angle, string ContentFormat);
public sealed record IdeaGenerationContext(Guid ProjectId, Guid OpportunityId, Guid OpportunityReportId, int OpportunityReportVersion,
    string Market, string Language, string Geography, string ProjectAudience, string OpportunityName, string OpportunityDescription,
    string OpportunityAudience, string Topic, string ContentFormat, string Angle, string Rationale, int OpportunityScore,
    int ObservedDemandAlignment, int OpportunityEvidenceStrength, IReadOnlyList<IdeaEvidenceContext> Evidence,
    IReadOnlyList<ExistingIdeaContext> ExistingIdeas, IReadOnlyList<string> CompetitorTitles, IReadOnlyList<string> Limitations);
public sealed record IdeaFeatureAssessment(int Novelty, int TitlePotential, int ThumbnailPotential, int StoryPotential,
    int AudienceFit, int ProductionComplexity, int CompetitionRisk, int ResearchRisk);
public sealed record VideoIdeaCandidateResult(string WorkingTitle, string Topic, string Angle, string ContentFormat, string TargetAudience,
    string ViewerIntent, string HookConcept, string ThumbnailConcept, string ViewerPromise, string CoreQuestion,
    string WhyViewerWouldCare, string Hypothesis, IdeaFeatureAssessment Features, IReadOnlyList<Guid> EvidenceIds,
    IReadOnlyList<string> Risks, int Confidence);
public sealed record IdeaGenerationResult(IReadOnlyList<VideoIdeaCandidateResult> Ideas);
public sealed record IdeaScoreBreakdown(int OpportunityFit, int ObservedDemandAlignment, int Novelty, int TitlePotential,
    int ThumbnailPotential, int StoryPotential, int AudienceFit, int EvidenceStrength, int ProductionEase,
    int CompetitionRisk, int ResearchRisk, decimal DuplicationPenalty, decimal OverallScore);
public sealed record IdeaEvidenceDto(Guid Id, Guid OpportunityEvidenceId, string Summary);
public sealed record VideoIdeaDto(Guid Id, Guid OpportunityId, Guid GenerationId, string WorkingTitle, string Topic, string Angle,
    string ContentFormat, string TargetAudience, string ViewerIntent, string HookConcept, string ThumbnailConcept, string ViewerPromise,
    string CoreQuestion, string WhyViewerWouldCare, string Hypothesis, IdeaScoreBreakdown Scores, IReadOnlyList<IdeaEvidenceDto> Evidence,
    IReadOnlyList<string> Risks, int Confidence, string DecisionStatus, DateTimeOffset CreatedAt);
public sealed record IdeaGenerationDto(Guid Id, int Version, Guid OpportunityId, int OpportunityReportVersion, string PromptKey,
    int PromptVersion, string Provider, string Model, string ScoringAlgorithmVersion, DateTimeOffset CreatedAt, bool IsStale,
    IReadOnlyList<VideoIdeaDto> Ideas);
public sealed record IdeaGenerationSummaryDto(Guid Id, int Version, int OpportunityReportVersion, DateTimeOffset CreatedAt, int CandidateCount);
public sealed record IdeaBankDto(IReadOnlyList<VideoIdeaDto> Ideas, IReadOnlyList<IdeaGenerationSummaryDto> Generations,
    IdeaGenerationDto? LatestGeneration, string? ActiveJobStatus, string? LatestJobFailureReason);
public sealed record RunIdeaGenerationResult(Guid JobId, string Status, bool Existing);
public sealed record ApprovedOpportunityWithEvidence(OpportunityCandidate Candidate, OpportunityReport Report, IReadOnlyList<OpportunityEvidence> Evidence);
public sealed record VideoIdeaWithEvidence(VideoIdea Idea, IReadOnlyList<IdeaEvidence> Evidence);
