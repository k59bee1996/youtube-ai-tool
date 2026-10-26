using System.Text.Json;
using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Jobs;

public sealed class Job
{
    private Job()
    {
    }

    public Job(
        string type,
        string payload,
        DateTimeOffset queuedAt,
        int maxRetries = 3,
        Guid? competitorChannelId = null,
        Guid? projectId = null,
        Guid? opportunityId = null,
        Guid? videoProjectId = null,
        string? artifactType = null,
        Guid? artifactId = null,
        int? artifactVersion = null,
        string? locale = null)
    {
        if (maxRetries < 0)
        {
            throw new DomainException("Maximum retries cannot be negative.");
        }

        ValidateJson(payload);
        Id = Guid.NewGuid();
        Type = Guard.Required(type, nameof(type), 100);
        CompetitorChannelId = competitorChannelId;
        ProjectId = projectId;
        OpportunityId = opportunityId;
        VideoProjectId = videoProjectId;
        ArtifactType = artifactType;
        ArtifactId = artifactId;
        ArtifactVersion = artifactVersion;
        Locale = locale;
        if ((artifactType is null) != (artifactId is null) || (artifactId is null) != (artifactVersion is null) || (artifactVersion is null) != (locale is null))
            throw new DomainException("Artifact localization job identity must include type, ID, version, and locale.");
        Payload = payload;
        Status = JobStatus.Queued;
        MaxRetries = maxRetries;
        CreatedAt = queuedAt;
        AvailableAt = queuedAt;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public Guid? CompetitorChannelId { get; private set; }

    public Guid? ProjectId { get; private set; }

    public Guid? OpportunityId { get; private set; }
    public Guid? VideoProjectId { get; private set; }
    public string? ArtifactType { get; private set; }
    public Guid? ArtifactId { get; private set; }
    public int? ArtifactVersion { get; private set; }
    public string? Locale { get; private set; }

    public string Payload { get; private set; } = string.Empty;

    public JobStatus Status { get; private set; }

    public int RetryCount { get; private set; }

    public int MaxRetries { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset AvailableAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Immutable start of the current attempt; StartedAt remains a lease-heartbeat timestamp for recovery.</summary>
    public DateTimeOffset? ExecutionStartedAt { get; private set; }

    public Guid? LeaseId { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? FailureReason { get; private set; }

    public void Start(DateTimeOffset startedAt, Guid? leaseId = null)
    {
        if (Status is not JobStatus.Queued and not JobStatus.Retrying)
        {
            throw new DomainException($"A {Status} job cannot be started.");
        }

        if (startedAt < AvailableAt)
        {
            throw new DomainException("The job is not available yet.");
        }

        if (leaseId == Guid.Empty)
        {
            throw new DomainException("A job lease ID cannot be empty.");
        }

        Status = JobStatus.Running;
        StartedAt = startedAt;
        ExecutionStartedAt = startedAt;
        LeaseId = leaseId ?? Guid.NewGuid();
        FailureReason = null;
    }

    public void RenewLease(Guid leaseId, DateTimeOffset renewedAt)
    {
        EnsureRunning();
        if (LeaseId != leaseId)
        {
            throw new DomainException("The job lease ID does not match the current owner.");
        }

        if (StartedAt is not null && renewedAt < StartedAt)
        {
            throw new DomainException("A job lease cannot be renewed backwards.");
        }

        StartedAt = renewedAt;
    }

    public void Complete(DateTimeOffset completedAt)
    {
        EnsureRunning();
        Status = JobStatus.Completed;
        CompletedAt = completedAt;
        LeaseId = null;
    }

    public void Requeue(DateTimeOffset availableAt)
    {
        EnsureRunning();
        Status = JobStatus.Queued;
        AvailableAt = availableAt;
        StartedAt = null;
        ExecutionStartedAt = null;
        LeaseId = null;
        FailureReason = "Recovered after worker interruption.";
    }

    /// <summary>Replaces an attempt-specific payload while the same bounded job is being retried.</summary>
    public void ReplacePayloadForRetry(string payload)
    {
        EnsureRunning();
        ValidateJson(payload);
        Payload = payload;
    }

    public void Fail(
        string reason,
        bool retryable,
        DateTimeOffset failedAt,
        DateTimeOffset? retryAt = null)
    {
        EnsureRunning();
        FailureReason = Guard.Required(reason, nameof(reason), 2_000);
        LeaseId = null;

        if (retryable && RetryCount < MaxRetries)
        {
            if (retryAt is null || retryAt <= failedAt)
            {
                throw new DomainException("A retry must be scheduled in the future.");
            }

            RetryCount++;
            Status = JobStatus.Retrying;
            AvailableAt = retryAt.Value;
            return;
        }

        Status = JobStatus.Failed;
        CompletedAt = failedAt;
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        if (Status is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled)
        {
            throw new DomainException($"A {Status} job cannot be cancelled.");
        }

        Status = JobStatus.Cancelled;
        CompletedAt = cancelledAt;
        LeaseId = null;
    }

    private void EnsureRunning()
    {
        if (Status != JobStatus.Running)
        {
            throw new DomainException($"A {Status} job is not running.");
        }
    }

    private static void ValidateJson(string payload)
    {
        try
        {
            using var _ = JsonDocument.Parse(payload);
        }
        catch (JsonException exception)
        {
            throw new DomainException($"Job payload must be valid JSON: {exception.Message}");
        }
    }
}
