using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.AI;

public sealed class AiRun
{
    private AiRun()
    {
    }

    public AiRun(
        string workflow,
        Guid? projectId,
        string provider,
        string model,
        string promptKey,
        int promptVersion,
        DateTimeOffset startedAt)
    {
        if (promptVersion < 1)
        {
            throw new DomainException("Prompt version must be at least 1.");
        }

        Id = Guid.NewGuid();
        Workflow = Guard.Required(workflow, nameof(workflow), 100);
        ProjectId = projectId;
        Provider = Guard.Required(provider, nameof(provider), 100);
        Model = Guard.Required(model, nameof(model), 100);
        PromptKey = Guard.Required(promptKey, nameof(promptKey), 100);
        PromptVersion = promptVersion;
        Status = AiRunStatus.Running;
        StartedAt = startedAt;
    }

    public Guid Id { get; private set; }

    public string Workflow { get; private set; } = string.Empty;

    public Guid? ProjectId { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public string PromptKey { get; private set; } = string.Empty;

    public int PromptVersion { get; private set; }

    public int? InputTokens { get; private set; }

    public int? OutputTokens { get; private set; }

    public decimal? EstimatedCost { get; private set; }

    public long? LatencyMilliseconds { get; private set; }

    public AiRunStatus Status { get; private set; }

    public int RetryCount { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? FailureReason { get; private set; }

    public void RecordRetry()
    {
        EnsureRunning();
        RetryCount++;
    }

    public void Complete(
        int? inputTokens,
        int? outputTokens,
        decimal? estimatedCost,
        DateTimeOffset completedAt)
    {
        EnsureRunning();
        InputTokens = NonNegative(inputTokens, nameof(inputTokens));
        OutputTokens = NonNegative(outputTokens, nameof(outputTokens));
        EstimatedCost = NonNegative(estimatedCost, nameof(estimatedCost));
        Status = AiRunStatus.Succeeded;
        CompletedAt = completedAt;
        LatencyMilliseconds = CalculateLatency(completedAt);
    }

    public void Fail(string reason, DateTimeOffset completedAt)
    {
        EnsureRunning();
        FailureReason = Guard.Required(reason, nameof(reason), 2_000);
        Status = AiRunStatus.Failed;
        CompletedAt = completedAt;
        LatencyMilliseconds = CalculateLatency(completedAt);
    }

    private long CalculateLatency(DateTimeOffset completedAt)
    {
        if (completedAt < StartedAt)
        {
            throw new DomainException("Completion time cannot precede start time.");
        }

        return (long)(completedAt - StartedAt).TotalMilliseconds;
    }

    private void EnsureRunning()
    {
        if (Status != AiRunStatus.Running)
        {
            throw new DomainException($"A {Status} AI run cannot be changed.");
        }
    }

    private static int? NonNegative(int? value, string name)
    {
        if (value < 0)
        {
            throw new DomainException($"{name} cannot be negative.");
        }

        return value;
    }

    private static decimal? NonNegative(decimal? value, string name)
    {
        if (value < 0)
        {
            throw new DomainException($"{name} cannot be negative.");
        }

        return value;
    }
}
