using YoutubeAiFactory.Domain.Common;
using YoutubeAiFactory.Domain.Pilots;

namespace YoutubeAiFactory.Domain.Videos;

/// <summary>Durable execution context created from one approved pilot experiment.</summary>
public sealed class VideoProject
{
    private VideoProject() { }

    public VideoProject(Guid projectId, Guid pilotId, int pilotVersion, Guid pilotVideoId, Guid videoIdeaId,
        Guid opportunityId, string workingTitle, string topic, string angle, string contentFormat,
        string targetAudience, string hookConcept, string thumbnailConcept, string viewerPromise,
        PilotExperimentType experimentType, string pilotHypothesis, string variableBeingTested,
        string primaryMetric, string successSignal, string sourceWarningsJson, DateTimeOffset createdAt)
    {
        if (pilotVersion < 1) throw new DomainException("Pilot version must be positive.");
        Id = Guid.NewGuid(); ProjectId = Guard.NotEmpty(projectId, nameof(projectId));
        PilotId = Guard.NotEmpty(pilotId, nameof(pilotId)); PilotVersion = pilotVersion;
        PilotVideoId = Guard.NotEmpty(pilotVideoId, nameof(pilotVideoId)); VideoIdeaId = Guard.NotEmpty(videoIdeaId, nameof(videoIdeaId));
        OpportunityId = Guard.NotEmpty(opportunityId, nameof(opportunityId));
        WorkingTitle = Guard.Required(workingTitle, nameof(workingTitle), 300); Topic = Guard.Required(topic, nameof(topic), 500);
        Angle = Guard.Required(angle, nameof(angle), 1000); ContentFormat = Guard.Required(contentFormat, nameof(contentFormat), 500);
        TargetAudience = Guard.Required(targetAudience, nameof(targetAudience), 1000); HookConcept = Guard.Required(hookConcept, nameof(hookConcept), 2000);
        ThumbnailConcept = Guard.Required(thumbnailConcept, nameof(thumbnailConcept), 2000); ViewerPromise = Guard.Required(viewerPromise, nameof(viewerPromise), 2000);
        ExperimentType = experimentType; PilotHypothesis = Guard.Required(pilotHypothesis, nameof(pilotHypothesis), 4000);
        VariableBeingTested = Guard.Required(variableBeingTested, nameof(variableBeingTested), 2000);
        PrimaryMetric = Guard.Required(primaryMetric, nameof(primaryMetric), 500); SuccessSignal = Guard.Required(successSignal, nameof(successSignal), 2000);
        SourceWarningsJson = Guard.Required(sourceWarningsJson, nameof(sourceWarningsJson), 20_000);
        Status = VideoProjectStatus.Draft; CreatedAt = createdAt; UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid PilotId { get; private set; }
    public int PilotVersion { get; private set; }
    public Guid PilotVideoId { get; private set; }
    public Guid VideoIdeaId { get; private set; }
    public Guid OpportunityId { get; private set; }
    public string WorkingTitle { get; private set; } = string.Empty;
    public string Topic { get; private set; } = string.Empty;
    public string Angle { get; private set; } = string.Empty;
    public string ContentFormat { get; private set; } = string.Empty;
    public string TargetAudience { get; private set; } = string.Empty;
    public string HookConcept { get; private set; } = string.Empty;
    public string ThumbnailConcept { get; private set; } = string.Empty;
    public string ViewerPromise { get; private set; } = string.Empty;
    public PilotExperimentType ExperimentType { get; private set; }
    public string PilotHypothesis { get; private set; } = string.Empty;
    public string VariableBeingTested { get; private set; } = string.Empty;
    public string PrimaryMetric { get; private set; } = string.Empty;
    public string SuccessSignal { get; private set; } = string.Empty;
    public string SourceWarningsJson { get; private set; } = "[]";
    public string? ExecutionNotes { get; private set; }
    public VideoProjectStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateExecutionDetails(string workingTitle, string? executionNotes, DateTimeOffset updatedAt)
    {
        WorkingTitle = Guard.Required(workingTitle, nameof(workingTitle), 300);
        ExecutionNotes = string.IsNullOrWhiteSpace(executionNotes) ? null : Guard.Required(executionNotes, nameof(executionNotes), 10_000);
        UpdatedAt = updatedAt;
    }

    public void TransitionTo(VideoProjectStatus target, DateTimeOffset updatedAt)
    {
        if (target == Status) throw new DomainException("Video project is already in this workflow state.");
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(target))
            throw new DomainException($"Video project cannot transition from {Status} to {target}.");
        Status = target; UpdatedAt = updatedAt;
    }

    private static readonly Dictionary<VideoProjectStatus, VideoProjectStatus[]> AllowedTransitions =
        new Dictionary<VideoProjectStatus, VideoProjectStatus[]>
        {
            [VideoProjectStatus.Draft] = [VideoProjectStatus.ResearchQueued],
            [VideoProjectStatus.ResearchQueued] = [VideoProjectStatus.Researching, VideoProjectStatus.Draft],
            [VideoProjectStatus.Researching] = [VideoProjectStatus.ResearchReady],
            [VideoProjectStatus.ResearchReady] = [VideoProjectStatus.ResearchQueued, VideoProjectStatus.OutlineGenerating],
            [VideoProjectStatus.OutlineGenerating] = [VideoProjectStatus.OutlineReady],
            [VideoProjectStatus.OutlineReady] = [VideoProjectStatus.OutlineGenerating, VideoProjectStatus.OutlineApproved],
            [VideoProjectStatus.OutlineApproved] = [VideoProjectStatus.ScriptGenerating],
            [VideoProjectStatus.ScriptGenerating] = [VideoProjectStatus.ScriptReady],
            [VideoProjectStatus.ScriptReady] = [VideoProjectStatus.ScriptGenerating, VideoProjectStatus.ScriptApproved],
            [VideoProjectStatus.ScriptApproved] = [VideoProjectStatus.Packaging],
            [VideoProjectStatus.Packaging] = [VideoProjectStatus.ProductionReady],
            [VideoProjectStatus.ProductionReady] = [],
        };
}
