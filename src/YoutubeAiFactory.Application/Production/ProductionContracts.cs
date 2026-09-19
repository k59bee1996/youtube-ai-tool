using YoutubeAiFactory.Domain.Production;
using YoutubeAiFactory.Domain.Scripts;

namespace YoutubeAiFactory.Application.Production;

public enum ProductionJobOperation
{
    Generate,
    Validate,
}

public sealed record ProductionJobPayload(
    Guid ProjectId,
    Guid VideoProjectId,
    ProductionJobOperation Operation,
    Guid VideoScriptId,
    int VideoScriptVersion,
    string InputFingerprint,
    Guid? ProductionPackageId,
    string ReturnStatus
);

public sealed record ProductionContextClaim(
    Guid Id,
    string Statement,
    string SupportStatus,
    bool IsCritical,
    IReadOnlyList<string> EvidenceExcerpts,
    IReadOnlyList<string> SourceLocators
);

public sealed record ProductionContextConflict(
    Guid Id,
    Guid ClaimId,
    string Explanation,
    bool IsResolved
);

public sealed record ProductionContextBlock(
    Guid Id,
    int GlobalSequence,
    int SectionSequence,
    int BlockSequence,
    ScriptBlockType Type,
    string Narration,
    int WordCount,
    IReadOnlyList<ProductionContextClaim> Claims,
    IReadOnlyList<ProductionContextConflict> Conflicts
);

public sealed record ProductionGenerationContext(
    Guid ProjectId,
    Guid VideoProjectId,
    Guid VideoScriptId,
    int VideoScriptVersion,
    Guid VideoOutlineId,
    int VideoOutlineVersion,
    Guid ResearchReportId,
    int ResearchReportVersion,
    string InputFingerprint,
    string WorkingTitle,
    string ViewerPromise,
    string ContentFormat,
    string ContentLanguage,
    string TargetGeography,
    string TargetAudience,
    string ProjectAudience,
    string ExperimentType,
    string PilotHypothesis,
    string VariableBeingTested,
    string ControlStrategy,
    string PrimaryMetric,
    string SuccessSignal,
    int EstimatedDurationSeconds,
    IReadOnlyList<ProductionContextBlock> Blocks
);

public sealed record ProductionAssetResult(
    string AssetKey,
    ProductionAssetType AssetType,
    ProductionAcquisitionMode AcquisitionMode,
    string CreativeBrief,
    string GenerationPrompt,
    string SourceSearchBrief,
    bool RightsVerificationRequired,
    ProductionFactualityMode FactualityMode,
    string ReuseKey,
    ProductionComplexity Complexity,
    IReadOnlyList<Guid> ClaimIds
);

public sealed record ProductionShotResult(
    int Sequence,
    ProductionShotType ShotType,
    string VisualDescription,
    string Composition,
    string MotionSuggestion,
    decimal RelativeDurationWeight,
    ProductionFactualityMode FactualityMode,
    string? AssetKey,
    IReadOnlyList<Guid> ClaimIds,
    string Notes
);

public sealed record ProductionOnScreenTextResult(
    int Sequence,
    string Text,
    ProductionOnScreenTextType Type,
    string TimingIntent,
    IReadOnlyList<Guid> ClaimIds
);

public sealed record ProductionSceneResult(
    int Sequence,
    ProductionScenePurpose Purpose,
    string NarrationSummary,
    string VisualStrategy,
    ProductionComplexity Complexity,
    string TransitionIntent,
    string MusicBrief,
    string SoundEffectCue,
    string VoiceDirection,
    IReadOnlyList<Guid> ScriptBlockIds,
    IReadOnlyList<ProductionShotResult> Shots,
    IReadOnlyList<ProductionOnScreenTextResult> OnScreenText
);

public sealed record ProductionPackageResult(
    string VisualDirection,
    string PacingDirection,
    string ColorDirection,
    string TypographyDirection,
    string AudioDirection,
    string ExperimentProductionNotes,
    IReadOnlyList<ProductionSceneResult> Scenes,
    IReadOnlyList<ProductionAssetResult> AssetRequirements,
    IReadOnlyList<string> Warnings
);

public sealed record ProductionGroundingIssueResult(
    int SceneSequence,
    int? ShotSequence,
    string? AssetKey,
    int? OnScreenTextSequence,
    ProductionGroundingIssueType IssueType,
    ProductionGroundingIssueSeverity Severity,
    string ProblematicText,
    IReadOnlyList<Guid> RelevantClaimIds,
    string Explanation
);

public sealed record ProductionGroundingAuditResult(
    ProductionGroundingStatus Status,
    IReadOnlyList<ProductionGroundingIssueResult> Issues
);

public sealed record ProductionPackageWithDetails(
    ProductionPackage Package,
    IReadOnlyList<ProductionScene> Scenes,
    IReadOnlyList<ProductionSceneScriptBlock> SceneBlocks,
    IReadOnlyList<ProductionShot> Shots,
    IReadOnlyList<ProductionShotClaim> ShotClaims,
    IReadOnlyList<ProductionAssetRequirement> Assets,
    IReadOnlyList<ProductionAssetClaim> AssetClaims,
    IReadOnlyList<ProductionOnScreenText> OnScreenText,
    IReadOnlyList<ProductionOnScreenTextClaim> OnScreenTextClaims
);

public sealed record RunProductionPackageResult(Guid JobId, string Status, bool Existing);

public sealed record ProductionJobDto(
    Guid Id,
    string Status,
    string Operation,
    string? FailureReason
);

public sealed record ProductionClaimDto(
    Guid Id,
    string Statement,
    string SupportStatus,
    IReadOnlyList<string> SourceLocators
);

public sealed record ProductionShotDto(
    Guid Id,
    int Sequence,
    string ShotType,
    string VisualDescription,
    string Composition,
    string MotionSuggestion,
    int EstimatedDurationSeconds,
    string FactualityMode,
    Guid? AssetRequirementId,
    IReadOnlyList<ProductionClaimDto> Claims,
    string Notes
);

public sealed record ProductionOnScreenTextDto(
    Guid Id,
    int Sequence,
    string Text,
    string Type,
    string TimingIntent,
    IReadOnlyList<ProductionClaimDto> Claims
);

public sealed record ProductionSceneDto(
    Guid Id,
    int Sequence,
    string Purpose,
    string Narration,
    string NarrationSummary,
    string VisualStrategy,
    int EstimatedDurationSeconds,
    string Complexity,
    string TransitionIntent,
    string MusicBrief,
    string SoundEffectCue,
    string VoiceDirection,
    IReadOnlyList<Guid> ScriptBlockIds,
    IReadOnlyList<ProductionShotDto> Shots,
    IReadOnlyList<ProductionOnScreenTextDto> OnScreenText
);

public sealed record ProductionAssetDto(
    Guid Id,
    string AssetKey,
    string AssetType,
    string AcquisitionMode,
    string CreativeBrief,
    string GenerationPrompt,
    string SourceSearchBrief,
    bool RightsVerificationRequired,
    string FactualityMode,
    string ReuseKey,
    string Complexity,
    IReadOnlyList<ProductionClaimDto> Claims
);

public sealed record ProductionGroundingIssueDto(
    int SceneSequence,
    int? ShotSequence,
    string? AssetKey,
    int? OnScreenTextSequence,
    string IssueType,
    string Severity,
    string ProblematicText,
    IReadOnlyList<Guid> RelevantClaimIds,
    string Explanation
);

public sealed record ProductionPackageDto(
    Guid Id,
    Guid ProjectId,
    Guid VideoProjectId,
    Guid VideoScriptId,
    int VideoScriptVersion,
    Guid VideoOutlineId,
    int VideoOutlineVersion,
    Guid ResearchReportId,
    int ResearchReportVersion,
    int Version,
    string Status,
    string GroundingStatus,
    string EngineVersion,
    string PromptKey,
    int PromptVersion,
    string Provider,
    string Model,
    string ContentLanguage,
    int EstimatedDurationSeconds,
    string VisualDirection,
    string PacingDirection,
    string ColorDirection,
    string TypographyDirection,
    string AudioDirection,
    string ExperimentProductionNotes,
    bool IsStale,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<ProductionGroundingIssueDto> GroundingIssues,
    IReadOnlyList<ProductionSceneDto> Scenes,
    IReadOnlyList<ProductionAssetDto> AssetRequirements,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ApprovedAt
);

public sealed record ProductionPackageStatusDto(
    ProductionPackageDto? LatestPackage,
    ProductionJobDto? ActiveJob,
    ProductionJobDto? LatestJob,
    bool CanGenerate,
    string? BlockReason
);

public sealed record ProductionPackageHistoryItemDto(
    Guid Id,
    int Version,
    string Status,
    string GroundingStatus,
    int VideoScriptVersion,
    bool IsStale,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt
);

public sealed record UpdateProductionSceneRequest(
    Guid SceneId,
    string NarrationSummary,
    string VisualStrategy,
    string TransitionIntent,
    string MusicBrief,
    string SoundEffectCue,
    string VoiceDirection
);

public sealed record UpdateProductionShotRequest(
    Guid ShotId,
    string VisualDescription,
    string Composition,
    string MotionSuggestion,
    string Notes
);

public sealed record UpdateProductionAssetRequest(
    Guid AssetId,
    string CreativeBrief,
    string GenerationPrompt,
    string SourceSearchBrief
);

public sealed record UpdateProductionOnScreenTextRequest(
    Guid OnScreenTextId,
    string Text,
    string TimingIntent
);

public sealed record UpdateProductionPackageRequest(
    string VisualDirection,
    string PacingDirection,
    string ColorDirection,
    string TypographyDirection,
    string AudioDirection,
    string ExperimentProductionNotes,
    IReadOnlyList<UpdateProductionSceneRequest> Scenes,
    IReadOnlyList<UpdateProductionShotRequest> Shots,
    IReadOnlyList<UpdateProductionAssetRequest> Assets,
    IReadOnlyList<UpdateProductionOnScreenTextRequest> OnScreenText
);

public sealed record ProductionExport(
    string SchemaVersion,
    DateTimeOffset ExportedAt,
    Guid ProjectId,
    Guid VideoProjectId,
    Guid ProductionPackageId,
    int ProductionPackageVersion,
    Guid VideoScriptId,
    int VideoScriptVersion,
    string ContentLanguage,
    int EstimatedDurationSeconds,
    string VisualDirection,
    string PacingDirection,
    string ColorDirection,
    string TypographyDirection,
    string AudioDirection,
    string ExperimentProductionNotes,
    IReadOnlyList<ProductionSceneDto> Scenes,
    IReadOnlyList<ProductionAssetDto> AssetRequirements
);
