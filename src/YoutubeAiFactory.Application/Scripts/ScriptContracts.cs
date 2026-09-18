using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Domain.Scripts;

namespace YoutubeAiFactory.Application.Scripts;

public enum ScriptJobOperation
{
    Generate,
    Validate,
}

public sealed record ScriptJobPayload(Guid ProjectId, Guid VideoProjectId, ScriptJobOperation Operation,
    Guid VideoOutlineId, int VideoOutlineVersion, Guid ResearchReportId, int ResearchReportVersion,
    string InputFingerprint, Guid? VideoScriptId, string ReturnStatus);

public sealed record ScriptContextEvidence(Guid Id, string Stance, string Fact, string SupportingExcerpt,
    string SourceLocator, string SourceDomain);
public sealed record ScriptContextClaim(Guid Id, string Statement, string Type, string SupportStatus,
    decimal Confidence, bool IsCritical, IReadOnlyList<ScriptContextEvidence> Evidence);
public sealed record ScriptContextConflict(Guid Id, Guid ClaimId, string Explanation, bool IsResolved);
public sealed record ScriptContextGap(int Index, string Description, IReadOnlyList<Guid> ClaimIds);
public sealed record ScriptContextSection(Guid OutlineSectionId, int Sequence, string Heading, string Purpose,
    string Objective, string Summary, string? ViewerQuestion, string? TransitionIntent, int? EstimatedSeconds,
    int TargetWordCount, IReadOnlyList<ScriptContextClaim> Claims,
    IReadOnlyList<ScriptContextConflict> Conflicts, IReadOnlyList<ScriptContextGap> ResearchGaps);

public sealed record ScriptGenerationContext(Guid ProjectId, Guid VideoProjectId, Guid VideoOutlineId,
    int VideoOutlineVersion, Guid ResearchReportId, int ResearchReportVersion, string InputFingerprint,
    string WorkingTitle, string Topic, string Angle, string ContentFormat, string TargetAudience,
    string ProjectAudience, string ViewerPromise, string ContentLanguage, string TargetGeography,
    string StructureType, string CoreQuestion, string CoreTension, string OpeningHookConcept,
    string NarrativeProgression, string Payoff, string PacingStrategy, string ExperimentType,
    string PilotHypothesis, string VariableBeingTested, string ControlStrategy,
    string HowOutlineImplementsExperiment, int TargetDurationSeconds, int TargetWordCount,
    int MinimumWordCount, int MaximumWordCount, IReadOnlyList<ScriptContextSection> Sections);

public sealed record ScriptBlockResult(int Sequence, ScriptBlockType Type, string Text,
    IReadOnlyList<Guid> ClaimIds, IReadOnlyList<Guid> ConflictIds);
public sealed record ScriptSectionResult(Guid OutlineSectionId, int Sequence,
    IReadOnlyList<ScriptBlockResult> Blocks);
public sealed record VideoScriptResult(IReadOnlyList<ScriptSectionResult> Sections, string ClosingPayoff);

public sealed record ScriptGroundingIssueResult(int SectionSequence, int BlockSequence,
    ScriptGroundingIssueType IssueType, ScriptGroundingIssueSeverity Severity, string ProblematicText,
    IReadOnlyList<Guid> RelevantClaimIds, string Explanation);
public sealed record ScriptGroundingAuditResult(ScriptGroundingStatus Status,
    IReadOnlyList<ScriptGroundingIssueResult> Issues);

public sealed record RunVideoScriptResult(Guid JobId, string Status, bool Existing);
public sealed record ScriptJobDto(Guid Id, string Status, string Operation, string? FailureReason);
public sealed record ScriptGroundingIssueDto(int SectionSequence, int BlockSequence, string IssueType,
    string Severity, string ProblematicText, IReadOnlyList<Guid> RelevantClaimIds, string Explanation);
public sealed record ScriptClaimDto(Guid Id, string Statement, string Type, string SupportStatus,
    decimal Confidence, bool IsCritical, IReadOnlyList<OutlineEvidenceDto> Evidence);
public sealed record ScriptConflictDto(Guid Id, Guid ClaimId, string Explanation, bool IsResolved);
public sealed record VideoScriptBlockDto(Guid Id, int Sequence, string Type, string Text, int WordCount,
    IReadOnlyList<ScriptClaimDto> Claims, IReadOnlyList<ScriptConflictDto> Conflicts);
public sealed record VideoScriptSectionDto(Guid Id, Guid OutlineSectionId, int Sequence, string Heading,
    int WordCount, int EstimatedDurationSeconds, IReadOnlyList<VideoScriptBlockDto> Blocks);
public sealed record VideoScriptDto(Guid Id, Guid ProjectId, Guid VideoProjectId, Guid VideoOutlineId,
    int VideoOutlineVersion, Guid ResearchReportId, int ResearchReportVersion, int Version, string Status,
    string GroundingStatus, string ScriptEngineVersion, string PromptKey, int PromptVersion,
    string Provider, string Model, string ContentLanguage, int TotalWordCount, int EstimatedDurationSeconds,
    bool IsStale, IReadOnlyList<string> Warnings, IReadOnlyList<ScriptGroundingIssueDto> GroundingIssues,
    IReadOnlyList<VideoScriptSectionDto> Sections, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    DateTimeOffset? ApprovedAt);
public sealed record ScriptStatusDto(VideoScriptDto? LatestScript, ScriptJobDto? ActiveJob,
    ScriptJobDto? LatestJob, bool CanGenerate, string? BlockReason);
public sealed record VideoScriptHistoryItemDto(Guid Id, int Version, string Status, string GroundingStatus,
    int VideoOutlineVersion, int ResearchReportVersion, bool IsStale, DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt);

public sealed record UpdateVideoScriptBlockRequest(Guid BlockId, string Text);
public sealed record UpdateVideoScriptRequest(IReadOnlyList<UpdateVideoScriptBlockRequest> Blocks);

public sealed record VideoScriptWithDetails(VideoScript Script, IReadOnlyList<VideoScriptSection> Sections,
    IReadOnlyList<VideoScriptBlock> Blocks, IReadOnlyList<VideoScriptBlockClaim> BlockClaims,
    IReadOnlyList<VideoScriptBlockConflict> BlockConflicts);
