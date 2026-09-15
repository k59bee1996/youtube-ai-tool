using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Research;

/// <summary>One bounded evidence-research execution attempt. A completed report is created separately.</summary>
public sealed class ResearchRun
{
    private ResearchRun() { }

    public ResearchRun(Guid projectId, Guid videoProjectId, string algorithmVersion, string inputFingerprint, DateTimeOffset queuedAt)
    {
        Id = Guid.NewGuid();
        ProjectId = Guard.NotEmpty(projectId, nameof(projectId));
        VideoProjectId = Guard.NotEmpty(videoProjectId, nameof(videoProjectId));
        ResearchAlgorithmVersion = Guard.Required(algorithmVersion, nameof(algorithmVersion), 100);
        InputFingerprint = Guard.Required(inputFingerprint, nameof(inputFingerprint), 128);
        Status = ResearchRunStatus.Queued;
        QueuedAt = queuedAt;
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid VideoProjectId { get; private set; }
    public ResearchRunStatus Status { get; private set; }
    public string ResearchAlgorithmVersion { get; private set; } = string.Empty;
    public string InputFingerprint { get; private set; } = string.Empty;
    public DateTimeOffset QueuedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int SearchQueryCount { get; private set; }
    public int SearchResultCount { get; private set; }
    public int FetchedSourceCount { get; private set; }
    public int RelevantSourceCount { get; private set; }
    public int EvidenceCount { get; private set; }
    public int ClaimCount { get; private set; }
    public int ConflictCount { get; private set; }
    public int SearchFailureCount { get; private set; }
    public int FetchFailureCount { get; private set; }
    public Guid? ResearchReportId { get; private set; }
    public string? FailureReason { get; private set; }

    public void Start(DateTimeOffset startedAt)
    {
        if (Status != ResearchRunStatus.Queued) throw new DomainException("Only a queued research run can start.");
        Status = ResearchRunStatus.Running;
        StartedAt = startedAt;
    }

    public void Complete(Guid reportId, ResearchRunMetrics metrics, DateTimeOffset completedAt)
    {
        if (Status != ResearchRunStatus.Running) throw new DomainException("Only a running research run can complete.");
        if (completedAt < StartedAt) throw new DomainException("Research completion cannot precede start.");
        ResearchReportId = Guard.NotEmpty(reportId, nameof(reportId));
        SetMetrics(metrics);
        Status = ResearchRunStatus.Completed;
        CompletedAt = completedAt;
    }

    public void RecordMetrics(ResearchRunMetrics metrics)
    {
        if (Status is not (ResearchRunStatus.Queued or ResearchRunStatus.Running))
            throw new DomainException("Only an active research run can record metrics.");
        SetMetrics(metrics);
    }

    public void Fail(string reason, ResearchRunMetrics metrics, DateTimeOffset failedAt)
    {
        if (Status is ResearchRunStatus.Completed or ResearchRunStatus.Failed) throw new DomainException("A completed research run cannot fail.");
        SetMetrics(metrics);
        FailureReason = Guard.Required(reason, nameof(reason), 2_000);
        Status = ResearchRunStatus.Failed;
        CompletedAt = failedAt;
    }

    private void SetMetrics(ResearchRunMetrics metrics)
    {
        if (metrics.AnyNegative) throw new DomainException("Research metrics cannot be negative.");
        SearchQueryCount = metrics.SearchQueryCount;
        SearchResultCount = metrics.SearchResultCount;
        FetchedSourceCount = metrics.FetchedSourceCount;
        RelevantSourceCount = metrics.RelevantSourceCount;
        EvidenceCount = metrics.EvidenceCount;
        ClaimCount = metrics.ClaimCount;
        ConflictCount = metrics.ConflictCount;
        SearchFailureCount = metrics.SearchFailureCount;
        FetchFailureCount = metrics.FetchFailureCount;
    }
}

public sealed record ResearchRunMetrics(int SearchQueryCount, int SearchResultCount, int FetchedSourceCount,
    int RelevantSourceCount, int EvidenceCount, int ClaimCount, int ConflictCount, int SearchFailureCount, int FetchFailureCount)
{
    public bool AnyNegative => SearchQueryCount < 0 || SearchResultCount < 0 || FetchedSourceCount < 0 ||
        RelevantSourceCount < 0 || EvidenceCount < 0 || ClaimCount < 0 || ConflictCount < 0 ||
        SearchFailureCount < 0 || FetchFailureCount < 0;
}
