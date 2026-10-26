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
        DateTimeOffset startedAt,
        string modelProfile = "Reasoning",
        Guid? videoProjectId = null,
        Guid? researchRunId = null,
        Guid? researchReportId = null,
        Guid? jobId = null,
        string? workflowStage = null)
        : this(workflow, projectId, null, provider, model, promptKey, promptVersion, startedAt, modelProfile, videoProjectId, researchRunId, researchReportId, jobId, workflowStage)
    {
    }

    public AiRun(
        string workflow,
        Guid? projectId,
        Guid? competitorId,
        string provider,
        string model,
        string promptKey,
        int promptVersion,
        DateTimeOffset startedAt,
        string modelProfile = "Reasoning",
        Guid? videoProjectId = null,
        Guid? researchRunId = null,
        Guid? researchReportId = null,
        Guid? jobId = null,
        string? workflowStage = null)
    {
        if (promptVersion < 1)
        {
            throw new DomainException("Prompt version must be at least 1.");
        }

        Id = Guid.NewGuid();
        Workflow = Guard.Required(workflow, nameof(workflow), 100);
        ProjectId = projectId;
        CompetitorId = competitorId;
        VideoProjectId = videoProjectId;
        ResearchRunId = researchRunId;
        ResearchReportId = researchReportId;
        JobId = jobId;
        WorkflowStage = workflowStage is null ? null : Guard.Required(workflowStage, nameof(workflowStage), 100);
        Provider = Guard.Required(provider, nameof(provider), 100);
        Model = Guard.Required(model, nameof(model), 100);
        ModelProfile = Guard.Required(modelProfile, nameof(modelProfile), 30);
        PromptKey = Guard.Required(promptKey, nameof(promptKey), 100);
        PromptVersion = promptVersion;
        Status = AiRunStatus.Running;
        StartedAt = startedAt;
    }

    public Guid Id { get; private set; }

    public string Workflow { get; private set; } = string.Empty;

    public Guid? ProjectId { get; private set; }

    public Guid? CompetitorId { get; private set; }

    public Guid? VideoProjectId { get; private set; }

    public Guid? ResearchRunId { get; private set; }

    public Guid? ResearchReportId { get; private set; }

    /// <summary>The durable logical job that owns this provider request, when one exists.</summary>
    public Guid? JobId { get; private set; }

    /// <summary>A real recorded sub-stage such as Generation, GroundingAudit, or Correction.</summary>
    public string? WorkflowStage { get; private set; }

    public string Provider { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    /// <summary>Logical quality class requested by the workflow at execution time.</summary>
    public string ModelProfile { get; private set; } = string.Empty;

    public string PromptKey { get; private set; } = string.Empty;

    public int PromptVersion { get; private set; }

    public int? InputTokens { get; private set; }

    public int? OutputTokens { get; private set; }

    /// <summary>Provider-reported cached input tokens. This is a subset of InputTokens and is never added to it.</summary>
    public int? CachedInputTokens { get; private set; }

    /// <summary>Provider-reported reasoning tokens. This is informational and is never added to OutputTokens.</summary>
    public int? ReasoningTokens { get; private set; }

    public decimal? EstimatedCost { get; private set; }

    public decimal? ProviderReportedCost { get; private set; }

    public decimal? CalculatedEstimatedCost { get; private set; }

    public string? Currency { get; private set; }

    public AiCostSource CostSource { get; private set; }

    public string? PricingVersion { get; private set; }

    public DateTimeOffset? PricingEffectiveFrom { get; private set; }

    public string? ErrorCategory { get; private set; }

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

    public void RecordProvider(string provider, string model)
    {
        EnsureRunning();
        Provider = Guard.Required(provider, nameof(provider), 100);
        Model = Guard.Required(model, nameof(model), 100);
    }

    public void Complete(
        int? inputTokens,
        int? outputTokens,
        decimal? estimatedCost,
        DateTimeOffset completedAt,
        int? cachedInputTokens = null,
        int? reasoningTokens = null,
        decimal? providerReportedCost = null,
        string? currency = null,
        string? pricingVersion = null,
        DateTimeOffset? pricingEffectiveFrom = null)
    {
        EnsureRunning();
        InputTokens = NonNegative(inputTokens, nameof(inputTokens));
        OutputTokens = NonNegative(outputTokens, nameof(outputTokens));
        EstimatedCost = NonNegative(estimatedCost, nameof(estimatedCost));
        CachedInputTokens = NonNegative(cachedInputTokens, nameof(cachedInputTokens));
        ReasoningTokens = NonNegative(reasoningTokens, nameof(reasoningTokens));
        ProviderReportedCost = NonNegative(providerReportedCost, nameof(providerReportedCost));
        CalculatedEstimatedCost = EstimatedCost;
        Currency = NormalizeCurrency(currency, providerReportedCost ?? estimatedCost);
        CostSource = ProviderReportedCost is not null && Currency is not null
            ? AiCostSource.ProviderReported
            : CalculatedEstimatedCost is not null && Currency is not null ? AiCostSource.PriceCalculated : AiCostSource.Unavailable;
        PricingVersion = CostSource == AiCostSource.PriceCalculated && !string.IsNullOrWhiteSpace(pricingVersion)
            ? Guard.Required(pricingVersion, nameof(pricingVersion), 100)
            : null;
        PricingEffectiveFrom = CostSource == AiCostSource.PriceCalculated ? pricingEffectiveFrom : null;
        Status = AiRunStatus.Succeeded;
        CompletedAt = completedAt;
        LatencyMilliseconds = CalculateLatency(completedAt);
    }

    public void Fail(string reason, DateTimeOffset completedAt, string? errorCategory = null)
    {
        EnsureRunning();
        FailureReason = Guard.Required(reason, nameof(reason), 2_000);
        ErrorCategory = errorCategory is null ? ClassifyFailure(reason) : Guard.Required(errorCategory, nameof(errorCategory), 100);
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

    private static string? NormalizeCurrency(string? currency, decimal? cost)
    {
        if (cost is null)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(currency)
            ? null
            : Guard.Required(currency.Trim().ToUpperInvariant(), nameof(currency), 3);
    }

    private static string? ClassifyFailure(string reason)
    {
        if (reason.Contains("rate", StringComparison.OrdinalIgnoreCase) || reason.Contains("quota", StringComparison.OrdinalIgnoreCase)) return "RateLimit";
        if (reason.Contains("timed out", StringComparison.OrdinalIgnoreCase) || reason.Contains("timeout", StringComparison.OrdinalIgnoreCase)) return "Timeout";
        if (reason.Contains("structured", StringComparison.OrdinalIgnoreCase) || reason.Contains("JSON", StringComparison.OrdinalIgnoreCase)) return "InvalidStructuredOutput";
        if (reason.Contains("grounding", StringComparison.OrdinalIgnoreCase)) return "GroundingValidationFailure";
        if (reason.Contains("persist", StringComparison.OrdinalIgnoreCase) || reason.Contains("database", StringComparison.OrdinalIgnoreCase)) return "PersistenceFailure";
        return "WorkflowFailure";
    }
}
