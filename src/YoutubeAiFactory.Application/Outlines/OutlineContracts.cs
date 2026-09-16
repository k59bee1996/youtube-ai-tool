using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Domain.Outlines;
using YoutubeAiFactory.Domain.Pilots;
using YoutubeAiFactory.Domain.Research;

namespace YoutubeAiFactory.Application.Outlines;

public sealed record OutlineJobPayload(Guid ProjectId, Guid VideoProjectId, Guid ResearchReportId,
    int ResearchReportVersion, string InputFingerprint, string ReturnStatus);

public sealed record OutlineNarrativeStrategyResult(OutlineStructureType StructureType, string CoreQuestion,
    string CoreTension, string OpeningHookConcept, string NarrativeProgression, string Payoff, string PacingStrategy);

public sealed record OutlineExperimentAlignmentResult(string HowOutlineImplementsExperiment,
    IReadOnlyList<string> RisksToExperimentIntegrity);

public sealed record OutlineSectionClaimReference(Guid ClaimId, OutlineClaimUsageRole UsageRole);

public sealed record OutlineSectionResult(int Sequence, string Heading, OutlineSectionPurpose Purpose,
    string Objective, string Summary, string? ViewerQuestion, string? TransitionIntent,
    IReadOnlyList<OutlineSectionClaimReference> ClaimReferences, IReadOnlyList<Guid> ConflictIds,
    IReadOnlyList<int> ResearchGapIndexes, int? EstimatedSeconds);

public sealed record OutlineGenerationResult(OutlineNarrativeStrategyResult NarrativeStrategy,
    OutlineExperimentAlignmentResult ExperimentAlignment, IReadOnlyList<OutlineSectionResult> Sections);

public sealed record OutlineContextFinding(string Summary, string Category, IReadOnlyList<Guid> ClaimIds);
public sealed record OutlineContextClaim(Guid Id, string Statement, string Type, string SupportStatus,
    bool IsCritical, decimal Confidence, IReadOnlyList<Guid> EvidenceIds);
public sealed record OutlineContextEvidence(Guid Id, Guid SourceId, string Fact, string SupportingExcerpt,
    string SourceLocator, string Type, decimal Confidence, string Domain);
public sealed record OutlineContextConflict(Guid Id, Guid ClaimId, string Explanation, bool IsResolved);
public sealed record OutlineContextGap(int Index, string Description, IReadOnlyList<Guid> ClaimIds);

public sealed record OutlineGenerationContext(Guid ProjectId, Guid VideoProjectId, Guid ResearchReportId,
    int ResearchReportVersion, string ResearchInputFingerprint, string OutlineInputFingerprint,
    string WorkingTitle, string Topic, string Angle, string ContentFormat, string TargetAudience,
    string ViewerPromise, string HookConcept, string TargetLanguage, string TargetGeography,
    PilotExperimentType ExperimentType, string PilotHypothesis, string VariableBeingTested,
    string ControlStrategy, string PrimaryMetric, string SuccessSignal, string ExecutiveSummary,
    IReadOnlyList<OutlineContextFinding> KeyFindings, IReadOnlyList<OutlineContextClaim> Claims,
    IReadOnlyList<OutlineContextEvidence> Evidence, IReadOnlyList<OutlineContextConflict> Conflicts,
    IReadOnlyList<OutlineContextGap> ResearchGaps, IReadOnlyList<string> ResearchWarnings,
    IReadOnlyList<string> ResearchLimitations);

public sealed record RunVideoOutlineResult(Guid JobId, string Status, bool Existing);
public sealed record OutlineJobDto(Guid Id, string Status, string? FailureReason);
public sealed record OutlineEvidenceDto(Guid Id, string Fact, string SupportingExcerpt, string SourceLocator,
    string Type, decimal Confidence, ResearchSourceDto Source);
public sealed record OutlineClaimDto(Guid Id, string Statement, string Type, string SupportStatus,
    decimal Confidence, bool IsCritical, string UsageRole, IReadOnlyList<OutlineEvidenceDto> Evidence);
public sealed record OutlineConflictDto(Guid Id, Guid ClaimId, string Explanation, bool IsResolved);
public sealed record OutlineGapDto(int Index, string Description, IReadOnlyList<Guid> ClaimIds);
public sealed record VideoOutlineSectionDto(Guid Id, int Sequence, string Heading, string Purpose,
    string Objective, string Summary, string? ViewerQuestion, string? TransitionIntent, int? EstimatedSeconds,
    IReadOnlyList<OutlineClaimDto> Claims, IReadOnlyList<OutlineConflictDto> Conflicts,
    IReadOnlyList<OutlineGapDto> ResearchGaps);
public sealed record ExperimentAlignmentDto(string ExperimentType, string VariableBeingTested,
    string ControlStrategy, string HowOutlineImplementsExperiment, IReadOnlyList<string> RisksToExperimentIntegrity);
public sealed record VideoOutlineDto(Guid Id, Guid ProjectId, Guid VideoProjectId, Guid ResearchReportId,
    int ResearchReportVersion, int Version, string Status, string OutlineAlgorithmVersion, string PromptKey,
    int PromptVersion, string Provider, string Model, string StructureType, string CoreQuestion,
    string CoreTension, string OpeningHookConcept, string ViewerPromise, string NarrativeProgression,
    string Payoff, string PacingStrategy, ExperimentAlignmentDto ExperimentAlignment,
    int? TotalEstimatedSeconds, bool TransitionsRequireReview, bool IsStale, IReadOnlyList<string> Warnings,
    IReadOnlyList<VideoOutlineSectionDto> Sections, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    DateTimeOffset? ApprovedAt);
public sealed record OutlineStatusDto(VideoOutlineDto? LatestOutline, OutlineJobDto? ActiveJob,
    OutlineJobDto? LatestJob, bool CanGenerate, string? BlockReason);
public sealed record VideoOutlineHistoryItemDto(Guid Id, int Version, string Status,
    int ResearchReportVersion, string StructureType, bool IsStale, DateTimeOffset CreatedAt, DateTimeOffset? ApprovedAt);

public sealed record UpdateVideoOutlineSectionRequest(Guid SectionId, string Heading, string Objective,
    string Summary, string? ViewerQuestion, string? TransitionIntent, int? EstimatedSeconds);
public sealed record UpdateVideoOutlineRequest(IReadOnlyList<UpdateVideoOutlineSectionRequest> Sections);
public sealed record ReorderVideoOutlineSectionsRequest(IReadOnlyList<Guid> SectionIds);

public sealed record VideoOutlineWithDetails(VideoOutline Outline, IReadOnlyList<VideoOutlineSection> Sections,
    IReadOnlyList<VideoOutlineSectionClaim> SectionClaims,
    IReadOnlyList<VideoOutlineSectionConflict> SectionConflicts,
    IReadOnlyList<VideoOutlineSectionGap> SectionGaps);
