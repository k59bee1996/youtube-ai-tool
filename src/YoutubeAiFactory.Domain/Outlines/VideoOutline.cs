using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Pilots;

namespace YoutubeAiFactory.Domain.Outlines;

/// <summary>An evidence-grounded, versioned narrative blueprint for one VideoProject.</summary>
public sealed class VideoOutline
{
    private VideoOutline() { }

    public VideoOutline(Guid projectId, Guid videoProjectId, Guid researchReportId, int researchReportVersion,
        int version, Guid generationAiRunId, string outlineAlgorithmVersion, string promptKey, int promptVersion,
        string inputFingerprint, string provider, string model, OutlineStructureType structureType,
        string coreQuestion, string coreTension, string openingHookConcept, string viewerPromise,
        string narrativeProgression, string payoff, string pacingStrategy, PilotExperimentType experimentType,
        string variableBeingTested, string controlStrategy, string howOutlineImplementsExperiment,
        string experimentRisksJson, string warningsJson, int? totalEstimatedSeconds, DateTimeOffset createdAt)
    {
        if (researchReportVersion < 1) throw new DomainException("Research report version must be positive.");
        if (version < 1) throw new DomainException("Outline version must be positive.");
        if (promptVersion < 1) throw new DomainException("Outline prompt version must be positive.");
        if (totalEstimatedSeconds < 0) throw new DomainException("Outline duration cannot be negative.");
        if (!Enum.IsDefined(structureType)) throw new DomainException("Outline structure type is invalid.");
        if (!Enum.IsDefined(experimentType)) throw new DomainException("Pilot experiment type is invalid.");
        Id = Guid.NewGuid();
        ProjectId = Guard.NotEmpty(projectId, nameof(projectId));
        VideoProjectId = Guard.NotEmpty(videoProjectId, nameof(videoProjectId));
        ResearchReportId = Guard.NotEmpty(researchReportId, nameof(researchReportId));
        ResearchReportVersion = researchReportVersion;
        Version = version;
        GenerationAiRunId = Guard.NotEmpty(generationAiRunId, nameof(generationAiRunId));
        OutlineAlgorithmVersion = Guard.Required(outlineAlgorithmVersion, nameof(outlineAlgorithmVersion), 100);
        PromptKey = Guard.Required(promptKey, nameof(promptKey), 100);
        PromptVersion = promptVersion;
        InputFingerprint = Guard.Required(inputFingerprint, nameof(inputFingerprint), 128);
        Provider = Guard.Required(provider, nameof(provider), 100);
        Model = Guard.Required(model, nameof(model), 100);
        StructureType = structureType;
        CoreQuestion = Guard.Required(coreQuestion, nameof(coreQuestion), 1_000);
        CoreTension = Guard.Required(coreTension, nameof(coreTension), 2_000);
        OpeningHookConcept = Guard.Required(openingHookConcept, nameof(openingHookConcept), 2_000);
        ViewerPromise = Guard.Required(viewerPromise, nameof(viewerPromise), 2_000);
        NarrativeProgression = Guard.Required(narrativeProgression, nameof(narrativeProgression), 4_000);
        Payoff = Guard.Required(payoff, nameof(payoff), 2_000);
        PacingStrategy = Guard.Required(pacingStrategy, nameof(pacingStrategy), 2_000);
        ExperimentType = experimentType;
        VariableBeingTested = Guard.Required(variableBeingTested, nameof(variableBeingTested), 2_000);
        ControlStrategy = Guard.Required(controlStrategy, nameof(controlStrategy), 2_000);
        HowOutlineImplementsExperiment = Guard.Required(howOutlineImplementsExperiment, nameof(howOutlineImplementsExperiment), 4_000);
        ExperimentRisksJson = Guard.Required(experimentRisksJson, nameof(experimentRisksJson), 20_000);
        WarningsJson = Guard.Required(warningsJson, nameof(warningsJson), 20_000);
        TotalEstimatedSeconds = totalEstimatedSeconds;
        Status = VideoOutlineStatus.Ready;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid VideoProjectId { get; private set; }
    public Guid ResearchReportId { get; private set; }
    public int ResearchReportVersion { get; private set; }
    public int Version { get; private set; }
    public VideoOutlineStatus Status { get; private set; }
    public Guid GenerationAiRunId { get; private set; }
    public string OutlineAlgorithmVersion { get; private set; } = string.Empty;
    public string PromptKey { get; private set; } = string.Empty;
    public int PromptVersion { get; private set; }
    public string InputFingerprint { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public OutlineStructureType StructureType { get; private set; }
    public string CoreQuestion { get; private set; } = string.Empty;
    public string CoreTension { get; private set; } = string.Empty;
    public string OpeningHookConcept { get; private set; } = string.Empty;
    public string ViewerPromise { get; private set; } = string.Empty;
    public string NarrativeProgression { get; private set; } = string.Empty;
    public string Payoff { get; private set; } = string.Empty;
    public string PacingStrategy { get; private set; } = string.Empty;
    public PilotExperimentType ExperimentType { get; private set; }
    public string VariableBeingTested { get; private set; } = string.Empty;
    public string ControlStrategy { get; private set; } = string.Empty;
    public string HowOutlineImplementsExperiment { get; private set; } = string.Empty;
    public string ExperimentRisksJson { get; private set; } = "[]";
    public string WarningsJson { get; private set; } = "[]";
    public int? TotalEstimatedSeconds { get; private set; }
    public bool TransitionsRequireReview { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }

    public void RecordEdit(DateTimeOffset editedAt, int? totalEstimatedSeconds)
    {
        EnsureReady();
        if (totalEstimatedSeconds < 0) throw new DomainException("Outline duration cannot be negative.");
        TotalEstimatedSeconds = totalEstimatedSeconds;
        TransitionsRequireReview = false;
        UpdatedAt = editedAt;
    }

    public void RecordReorder(DateTimeOffset reorderedAt)
    {
        EnsureReady();
        TransitionsRequireReview = true;
        UpdatedAt = reorderedAt;
    }

    public void Approve(DateTimeOffset approvedAt)
    {
        EnsureReady();
        if (TransitionsRequireReview)
            throw new DomainException("Review section transitions after reordering before approving the outline.");
        Status = VideoOutlineStatus.Approved;
        ApprovedAt = approvedAt;
        UpdatedAt = approvedAt;
    }

    private void EnsureReady()
    {
        if (Status != VideoOutlineStatus.Ready)
            throw new DomainException("An approved outline is immutable.");
    }
}
