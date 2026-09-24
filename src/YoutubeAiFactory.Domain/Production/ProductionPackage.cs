using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Production;

public sealed class ProductionPackage
{
    private ProductionPackage() { }

    public ProductionPackage(
        Guid projectId,
        Guid videoProjectId,
        Guid videoScriptId,
        int videoScriptVersion,
        Guid videoOutlineId,
        int videoOutlineVersion,
        Guid researchReportId,
        int researchReportVersion,
        int version,
        Guid generationAiRunId,
        Guid groundingAiRunId,
        string engineVersion,
        string promptKey,
        int promptVersion,
        string inputFingerprint,
        string provider,
        string model,
        string contentLanguage,
        int estimatedDurationSeconds,
        string visualDirection,
        string pacingDirection,
        string colorDirection,
        string typographyDirection,
        string audioDirection,
        string experimentProductionNotes,
        string warningsJson,
        string groundingIssuesJson,
        DateTimeOffset createdAt
    )
    {
        if (
            videoScriptVersion < 1
            || videoOutlineVersion < 1
            || researchReportVersion < 1
            || version < 1
            || promptVersion < 1
        )
            throw new DomainException("Production package lineage versions must be positive.");
        if (estimatedDurationSeconds < 1)
            throw new DomainException("Production package duration must be positive.");
        Id = Guid.NewGuid();
        ProjectId = Guard.NotEmpty(projectId, nameof(projectId));
        VideoProjectId = Guard.NotEmpty(videoProjectId, nameof(videoProjectId));
        VideoScriptId = Guard.NotEmpty(videoScriptId, nameof(videoScriptId));
        VideoScriptVersion = videoScriptVersion;
        VideoOutlineId = Guard.NotEmpty(videoOutlineId, nameof(videoOutlineId));
        VideoOutlineVersion = videoOutlineVersion;
        ResearchReportId = Guard.NotEmpty(researchReportId, nameof(researchReportId));
        ResearchReportVersion = researchReportVersion;
        Version = version;
        GenerationAiRunId = Guard.NotEmpty(generationAiRunId, nameof(generationAiRunId));
        GroundingAiRunId = Guard.NotEmpty(groundingAiRunId, nameof(groundingAiRunId));
        EngineVersion = Guard.Required(engineVersion, nameof(engineVersion), 100);
        PromptKey = Guard.Required(promptKey, nameof(promptKey), 100);
        PromptVersion = promptVersion;
        InputFingerprint = Guard.Required(inputFingerprint, nameof(inputFingerprint), 128);
        Provider = Guard.Required(provider, nameof(provider), 100);
        Model = Guard.Required(model, nameof(model), 100);
        ContentLanguage = Guard.Required(contentLanguage, nameof(contentLanguage), 50);
        EstimatedDurationSeconds = estimatedDurationSeconds;
        SetDirection(
            visualDirection,
            pacingDirection,
            colorDirection,
            typographyDirection,
            audioDirection,
            experimentProductionNotes
        );
        WarningsJson = Guard.Required(warningsJson, nameof(warningsJson), 50_000);
        GroundingIssuesJson = Guard.Required(
            groundingIssuesJson,
            nameof(groundingIssuesJson),
            100_000
        );
        Status = ProductionPackageStatus.Ready;
        GroundingStatus = ProductionGroundingStatus.Passed;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid VideoProjectId { get; private set; }
    public Guid VideoScriptId { get; private set; }
    public int VideoScriptVersion { get; private set; }
    public Guid VideoOutlineId { get; private set; }
    public int VideoOutlineVersion { get; private set; }
    public Guid ResearchReportId { get; private set; }
    public int ResearchReportVersion { get; private set; }
    public int Version { get; private set; }
    public ProductionPackageStatus Status { get; private set; }
    public ProductionGroundingStatus GroundingStatus { get; private set; }
    public Guid GenerationAiRunId { get; private set; }
    public Guid? GroundingAiRunId { get; private set; }
    public string EngineVersion { get; private set; } = string.Empty;
    public string PromptKey { get; private set; } = string.Empty;
    public int PromptVersion { get; private set; }
    public string InputFingerprint { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string ContentLanguage { get; private set; } = string.Empty;
    public int EstimatedDurationSeconds { get; private set; }
    public string VisualDirection { get; private set; } = string.Empty;
    public string PacingDirection { get; private set; } = string.Empty;
    public string ColorDirection { get; private set; } = string.Empty;
    public string TypographyDirection { get; private set; } = string.Empty;
    public string AudioDirection { get; private set; } = string.Empty;
    public string ExperimentProductionNotes { get; private set; } = string.Empty;
    public string WarningsJson { get; private set; } = "[]";
    public string GroundingIssuesJson { get; private set; } = "[]";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public void RecordEdit(
        string visualDirection,
        string pacingDirection,
        string colorDirection,
        string typographyDirection,
        string audioDirection,
        string experimentProductionNotes,
        DateTimeOffset editedAt
    )
    {
        EnsureReady();
        SetDirection(
            visualDirection,
            pacingDirection,
            colorDirection,
            typographyDirection,
            audioDirection,
            experimentProductionNotes
        );
        GroundingStatus = ProductionGroundingStatus.Pending;
        GroundingAiRunId = null;
        GroundingIssuesJson = "[]";
        UpdatedAt = editedAt;
    }

    public void RecordGroundingResult(
        ProductionGroundingStatus status,
        Guid runId,
        string issuesJson,
        DateTimeOffset at
    )
    {
        EnsureReady();
        if (status == ProductionGroundingStatus.Pending)
            throw new DomainException("A completed grounding audit cannot remain Pending.");
        GroundingStatus = status;
        GroundingAiRunId = Guard.NotEmpty(runId, nameof(runId));
        GroundingIssuesJson = Guard.Required(issuesJson, nameof(issuesJson), 100_000);
        UpdatedAt = at;
    }

    public void Approve(DateTimeOffset at)
    {
        EnsureReady();
        if (GroundingStatus != ProductionGroundingStatus.Passed)
            throw new DomainException("Production package grounding must pass before approval.");
        Status = ProductionPackageStatus.Approved;
        ApprovedAt = at;
        UpdatedAt = at;
    }

    private void SetDirection(
        string visual,
        string pacing,
        string color,
        string typography,
        string audio,
        string experiment
    )
    {
        VisualDirection = Guard.Required(visual, nameof(visual), 4_000);
        PacingDirection = Guard.Required(pacing, nameof(pacing), 2_000);
        ColorDirection = Guard.Required(color, nameof(color), 2_000);
        TypographyDirection = Guard.Required(typography, nameof(typography), 2_000);
        AudioDirection = Guard.Required(audio, nameof(audio), 2_000);
        ExperimentProductionNotes = Guard.Required(experiment, nameof(experiment), 4_000);
    }

    private void EnsureReady()
    {
        if (Status != ProductionPackageStatus.Ready)
            throw new DomainException("An approved production package is immutable.");
    }
}
