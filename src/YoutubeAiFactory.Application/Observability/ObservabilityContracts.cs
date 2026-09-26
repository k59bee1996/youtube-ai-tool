namespace YoutubeAiFactory.Application.Observability;

public sealed record ObservabilityFilter(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Workflow = null,
    string? ModelProfile = null);

public sealed record CostAmountDto(string Currency, decimal Amount);

public sealed record AiCostCategoryDto(
    int AiRequestCount,
    int RequestsWithCost,
    int UnknownCostRequestCount,
    IReadOnlyList<CostAmountDto> KnownOrEstimatedCost);

public sealed record AiCostBreakdownDto(
    AiCostCategoryDto SharedProject,
    AiCostCategoryDto VideoProjectDirect,
    AiCostCategoryDto Total,
    IReadOnlyList<CostGroupDto> ByWorkflow,
    IReadOnlyList<CostGroupDto> ByModelProfile,
    IReadOnlyList<CostGroupDto> ByModel,
    IReadOnlyList<DailyCostDto> DailyTrend);

public sealed record CostGroupDto(
    string Key,
    int AiRequestCount,
    int UnknownCostRequestCount,
    IReadOnlyList<CostAmountDto> KnownOrEstimatedCost);

public sealed record DailyCostDto(
    DateOnly Date,
    int UnknownCostRequestCount,
    IReadOnlyList<CostAmountDto> KnownOrEstimatedCost);

public sealed record PipelineStageCountDto(string Stage, int Count);

public sealed record JobStatusCountDto(string Status, int Count);

public sealed record ProjectJobSummaryDto(int ActiveJobCount, int FailedJobCount, int CompletedJobCount, int RetryingJobCount);

public sealed record ProjectObservabilityOverviewDto(
    int TotalVideoProjects,
    IReadOnlyList<PipelineStageCountDto> Pipeline,
    AiCostCategoryDto AiCost,
    ProjectJobSummaryDto Jobs,
    IReadOnlyList<RecentExecutionDto> RecentActivity);

public sealed record WorkflowHealthDto(
    string Workflow,
    int CompletedJobs,
    int FailedJobs,
    int ActiveJobs,
    int RetryingJobs,
    int AiRequestCount,
    int RetriedJobCount,
    decimal? FailureRate,
    decimal? RetryRate,
    long? AverageExecutionMilliseconds);

public sealed record RecentExecutionDto(
    string Kind,
    Guid Id,
    string Workflow,
    string Status,
    DateTimeOffset OccurredAt,
    string? FailureReason,
    Guid? VideoProjectId);

public sealed record VideoProjectObservabilityDto(
    Guid VideoProjectId,
    string CurrentStatus,
    AiCostCategoryDto DirectAiCost,
    int CompletedJobCount,
    int FailedJobCount,
    IReadOnlyList<WorkflowExecutionSummaryDto> Workflows,
    IReadOnlyList<RecentExecutionDto> RecentActivity);

public sealed record WorkflowExecutionSummaryDto(
    string Workflow,
    int JobCount,
    int AiRequestCount,
    int UnknownCostRequestCount,
    IReadOnlyList<CostAmountDto> KnownOrEstimatedCost,
    long? AverageExecutionMilliseconds,
    string? LatestFailure);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record JobExecutionDto(
    Guid Id,
    string Type,
    string Status,
    Guid? VideoProjectId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    long? QueueWaitMilliseconds,
    long? ExecutionMilliseconds,
    long? EndToEndMilliseconds,
    int AttemptCount,
    int RetryCount,
    string? FailureReason);

public sealed record AiRunExecutionDto(
    Guid Id,
    Guid? JobId,
    Guid? VideoProjectId,
    string Workflow,
    string? WorkflowStage,
    string Status,
    string ModelProfile,
    string Provider,
    string Model,
    string PromptKey,
    int PromptVersion,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    long? DurationMilliseconds,
    int? InputTokens,
    int? OutputTokens,
    int? CachedInputTokens,
    int? ReasoningTokens,
    decimal? Cost,
    string? Currency,
    string CostSource,
    int RetryCount,
    string? ErrorCategory,
    string? FailureReason);

public interface IObservabilityQueries
{
    Task<ProjectObservabilityOverviewDto> GetProjectOverviewAsync(Guid projectId, ObservabilityFilter filter, CancellationToken cancellationToken);
    Task<AiCostBreakdownDto> GetProjectCostsAsync(Guid projectId, ObservabilityFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowHealthDto>> GetWorkflowHealthAsync(Guid projectId, ObservabilityFilter filter, CancellationToken cancellationToken);
    Task<VideoProjectObservabilityDto> GetVideoProjectAsync(Guid projectId, Guid videoProjectId, ObservabilityFilter filter, CancellationToken cancellationToken);
    Task<PagedResult<JobExecutionDto>> GetJobsAsync(Guid projectId, Guid? videoProjectId, int page, int pageSize, CancellationToken cancellationToken);
    Task<PagedResult<AiRunExecutionDto>> GetAiRunsAsync(Guid projectId, Guid? videoProjectId, int page, int pageSize, CancellationToken cancellationToken);
}
