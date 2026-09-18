using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Scripts;

/// <summary>A versioned, evidence-grounded narration artifact produced from one approved VideoOutline.</summary>
public sealed class VideoScript
{
    private VideoScript() { }

    public VideoScript(Guid projectId, Guid videoProjectId, Guid videoOutlineId, int videoOutlineVersion,
        Guid researchReportId, int researchReportVersion, int version, Guid generationAiRunId,
        Guid groundingAiRunId, string scriptEngineVersion, string promptKey, int promptVersion,
        string inputFingerprint, string provider, string model, string contentLanguage, int totalWordCount,
        int estimatedDurationSeconds, string warningsJson, string groundingIssuesJson, DateTimeOffset createdAt)
    {
        if (videoOutlineVersion < 1) throw new DomainException("Video outline version must be positive.");
        if (researchReportVersion < 1) throw new DomainException("Research report version must be positive.");
        if (version < 1) throw new DomainException("Script version must be positive.");
        if (promptVersion < 1) throw new DomainException("Script prompt version must be positive.");
        ValidateMetrics(totalWordCount, estimatedDurationSeconds);
        Id = Guid.NewGuid();
        ProjectId = Guard.NotEmpty(projectId, nameof(projectId));
        VideoProjectId = Guard.NotEmpty(videoProjectId, nameof(videoProjectId));
        VideoOutlineId = Guard.NotEmpty(videoOutlineId, nameof(videoOutlineId));
        VideoOutlineVersion = videoOutlineVersion;
        ResearchReportId = Guard.NotEmpty(researchReportId, nameof(researchReportId));
        ResearchReportVersion = researchReportVersion;
        Version = version;
        GenerationAiRunId = Guard.NotEmpty(generationAiRunId, nameof(generationAiRunId));
        GroundingAiRunId = Guard.NotEmpty(groundingAiRunId, nameof(groundingAiRunId));
        ScriptEngineVersion = Guard.Required(scriptEngineVersion, nameof(scriptEngineVersion), 100);
        PromptKey = Guard.Required(promptKey, nameof(promptKey), 100);
        PromptVersion = promptVersion;
        InputFingerprint = Guard.Required(inputFingerprint, nameof(inputFingerprint), 128);
        Provider = Guard.Required(provider, nameof(provider), 100);
        Model = Guard.Required(model, nameof(model), 100);
        ContentLanguage = Guard.Required(contentLanguage, nameof(contentLanguage), 50);
        TotalWordCount = totalWordCount;
        EstimatedDurationSeconds = estimatedDurationSeconds;
        WarningsJson = Guard.Required(warningsJson, nameof(warningsJson), 50_000);
        GroundingIssuesJson = Guard.Required(groundingIssuesJson, nameof(groundingIssuesJson), 100_000);
        Status = VideoScriptStatus.Ready;
        GroundingStatus = ScriptGroundingStatus.Passed;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid VideoProjectId { get; private set; }
    public Guid VideoOutlineId { get; private set; }
    public int VideoOutlineVersion { get; private set; }
    public Guid ResearchReportId { get; private set; }
    public int ResearchReportVersion { get; private set; }
    public int Version { get; private set; }
    public VideoScriptStatus Status { get; private set; }
    public ScriptGroundingStatus GroundingStatus { get; private set; }
    public Guid GenerationAiRunId { get; private set; }
    public Guid? GroundingAiRunId { get; private set; }
    public string ScriptEngineVersion { get; private set; } = string.Empty;
    public string PromptKey { get; private set; } = string.Empty;
    public int PromptVersion { get; private set; }
    public string InputFingerprint { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string ContentLanguage { get; private set; } = string.Empty;
    public int TotalWordCount { get; private set; }
    public int EstimatedDurationSeconds { get; private set; }
    public string WarningsJson { get; private set; } = "[]";
    public string GroundingIssuesJson { get; private set; } = "[]";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }

    public void RecordNarrationEdit(int totalWordCount, int estimatedDurationSeconds, string warningsJson,
        DateTimeOffset editedAt)
    {
        EnsureReady();
        ValidateMetrics(totalWordCount, estimatedDurationSeconds);
        TotalWordCount = totalWordCount;
        EstimatedDurationSeconds = estimatedDurationSeconds;
        WarningsJson = Guard.Required(warningsJson, nameof(warningsJson), 50_000);
        GroundingStatus = ScriptGroundingStatus.Pending;
        GroundingAiRunId = null;
        GroundingIssuesJson = "[]";
        UpdatedAt = editedAt;
    }

    public void RecordGroundingResult(ScriptGroundingStatus status, Guid groundingAiRunId,
        string groundingIssuesJson, DateTimeOffset validatedAt)
    {
        EnsureReady();
        if (status == ScriptGroundingStatus.Pending)
            throw new DomainException("A completed grounding audit cannot remain Pending.");
        GroundingStatus = status;
        GroundingAiRunId = Guard.NotEmpty(groundingAiRunId, nameof(groundingAiRunId));
        GroundingIssuesJson = Guard.Required(groundingIssuesJson, nameof(groundingIssuesJson), 100_000);
        UpdatedAt = validatedAt;
    }

    public void Approve(DateTimeOffset approvedAt)
    {
        EnsureReady();
        if (GroundingStatus != ScriptGroundingStatus.Passed)
            throw new DomainException("A script must pass grounding validation before approval.");
        Status = VideoScriptStatus.Approved;
        ApprovedAt = approvedAt;
        UpdatedAt = approvedAt;
    }

    private void EnsureReady()
    {
        if (Status != VideoScriptStatus.Ready)
            throw new DomainException("An approved script is immutable.");
    }

    private static void ValidateMetrics(int totalWordCount, int estimatedDurationSeconds)
    {
        if (totalWordCount < 1) throw new DomainException("Script word count must be positive.");
        if (estimatedDurationSeconds < 1) throw new DomainException("Script duration estimate must be positive.");
    }
}
