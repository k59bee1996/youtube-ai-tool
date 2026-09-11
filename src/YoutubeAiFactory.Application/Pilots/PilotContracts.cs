using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Pilots;

namespace YoutubeAiFactory.Application.Pilots;

public sealed record PilotGenerationJobPayload(Guid ProjectId);
public sealed record PilotIdeaContext(Guid VideoIdeaId, Guid OpportunityId, string OpportunityName, string WorkingTitle,
    string Topic, string Angle, string ContentFormat, string TargetAudience, string HookConcept, string ThumbnailConcept,
    string ViewerPromise, string Hypothesis, decimal OverallScore, int EvidenceStrength, int ProductionEase,
    int Novelty, int StoryPotential, IdeaDecisionStatus DecisionStatus, OpportunityDecisionStatus OpportunityDecisionStatus, int TopicFrequency, int FormatFrequency,
    int OpportunityFrequency);
public sealed record PilotGenerationContext(Guid ProjectId, string Market, string Audience, int EligibleIdeaCount, IReadOnlyList<PilotIdeaContext> Ideas);
public sealed record PilotVideoResult(int Sequence, Guid VideoIdeaId, Guid OpportunityId, PilotExperimentType ExperimentType,
    string Hypothesis, string VariableBeingTested, string ControlStrategy, string PrimaryMetric, string SuccessSignal,
    string Rationale, IReadOnlyList<string>? SecondaryMetrics = null, string? Notes = null);
public sealed record PilotExperimentSummary(int TopicCount, int PackagingCount, int StorytellingCount);
public sealed record PilotPlanResult(string Name, string Objective, IReadOnlyList<PilotVideoResult> Videos,
    PilotExperimentSummary ExperimentSummary, IReadOnlyList<string> Assumptions, IReadOnlyList<string> Limitations);
public sealed record PilotVideoDto(Guid Id, int Sequence, Guid VideoIdeaId, Guid OpportunityId, string WorkingTitle,
    string OpportunityName, decimal OverallIdeaScore, string ExperimentType, string Hypothesis, string VariableBeingTested,
    string ControlStrategy, string PrimaryMetric, string SuccessSignal, string Rationale, IReadOnlyList<string> SecondaryMetrics,
    string? Notes);
public sealed record PilotDto(Guid Id, int Version, string Name, string Objective, string Status, DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt, int EligibleIdeaCount, string PromptKey, int PromptVersion, string Provider, string Model,
    string PlanningAlgorithmVersion, IReadOnlyList<string> Assumptions, IReadOnlyList<string> Limitations,
    IReadOnlyList<string> Warnings, bool RequiresReview, IReadOnlyList<PilotVideoDto> Videos);
public sealed record PilotStatusDto(PilotDto? LatestPilot, string? ActiveJobStatus, string? LatestJobFailureReason,
    int EligibleIdeaCount, int RequiredIdeaCount = 12);
public sealed record RunPilotGenerationResult(Guid JobId, string Status, bool Existing);
public sealed record ReplacePilotSlotRequest(Guid VideoIdeaId);
public sealed record MovePilotSlotRequest(string Direction);
public sealed record PilotCandidateDto(Guid VideoIdeaId, Guid OpportunityId, string OpportunityName, string WorkingTitle,
    string Topic, string ContentFormat, decimal OverallScore);
