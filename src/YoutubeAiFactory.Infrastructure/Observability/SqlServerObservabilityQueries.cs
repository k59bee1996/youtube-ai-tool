using Microsoft.EntityFrameworkCore;
using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Observability;
using YoutubeAiFactory.Domain.AI;
using YoutubeAiFactory.Domain.Jobs;
using YoutubeAiFactory.Infrastructure.Persistence;

namespace YoutubeAiFactory.Infrastructure.Observability;

/// <summary>
/// Read-only operational reporting over the durable Job and AiRun records. It never invokes providers or changes workflow state.
/// </summary>
internal sealed class SqlServerObservabilityQueries(IDbContextFactory<YoutubeAiFactoryDbContext> contextFactory) : IObservabilityQueries
{
    // EstimatedCost predates explicit currency tracking and was always calculated in USD.
    private const string LegacyEstimatedCostCurrency = "USD";

    public async Task<ProjectObservabilityOverviewDto> GetProjectOverviewAsync(Guid projectId, ObservabilityFilter filter, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureProjectAsync(db, projectId, cancellationToken);
        var videos = await db.VideoProjects.AsNoTracking().Where(item => item.ProjectId == projectId).ToListAsync(cancellationToken);
        var jobs = await FilterJobs(db.Jobs.AsNoTracking().Where(item => item.ProjectId == projectId), filter).ToListAsync(cancellationToken);
        var runs = await FilterRuns(db.AiRuns.AsNoTracking().Where(item => item.ProjectId == projectId), filter).ToListAsync(cancellationToken);
        var costs = BuildCosts(runs);

        return new ProjectObservabilityOverviewDto(
            videos.Count,
            videos.GroupBy(item => PipelineStage(item.Status.ToString())).OrderBy(item => item.Key)
                .Select(item => new PipelineStageCountDto(item.Key, item.Count())).ToArray(),
            costs.Total,
            new ProjectJobSummaryDto(
                jobs.Count(item => item.Status is JobStatus.Queued or JobStatus.Running or JobStatus.Retrying),
                jobs.Count(item => item.Status == JobStatus.Failed),
                jobs.Count(item => item.Status == JobStatus.Completed),
                jobs.Count(item => item.Status == JobStatus.Retrying)),
            BuildRecent(jobs, runs, 12));
    }

    public async Task<AiCostBreakdownDto> GetProjectCostsAsync(Guid projectId, ObservabilityFilter filter, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureProjectAsync(db, projectId, cancellationToken);
        var runs = await FilterRuns(db.AiRuns.AsNoTracking().Where(item => item.ProjectId == projectId), filter).ToListAsync(cancellationToken);
        return BuildCosts(runs);
    }

    public async Task<IReadOnlyList<WorkflowHealthDto>> GetWorkflowHealthAsync(Guid projectId, ObservabilityFilter filter, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureProjectAsync(db, projectId, cancellationToken);
        var jobs = await FilterJobs(db.Jobs.AsNoTracking().Where(item => item.ProjectId == projectId), filter).ToListAsync(cancellationToken);
        var runs = await FilterRuns(db.AiRuns.AsNoTracking().Where(item => item.ProjectId == projectId), filter).ToListAsync(cancellationToken);
        var runsByJob = runs.Where(item => item.JobId is not null).GroupBy(item => item.JobId!.Value)
            .ToDictionary(item => item.Key, item => item.Count());

        return jobs.GroupBy(item => item.Type).OrderBy(item => item.Key).Select(group =>
        {
            var values = group.ToArray();
            var terminal = values.Where(item => item.Status is JobStatus.Completed or JobStatus.Failed).ToArray();
            var durations = values.Select(ExecutionMilliseconds).Where(item => item is not null).Select(item => item!.Value).ToArray();
            var retried = values.Count(item => item.RetryCount > 0);
            return new WorkflowHealthDto(
                group.Key,
                values.Count(item => item.Status == JobStatus.Completed),
                values.Count(item => item.Status == JobStatus.Failed),
                values.Count(item => item.Status is JobStatus.Queued or JobStatus.Running or JobStatus.Retrying),
                values.Count(item => item.Status == JobStatus.Retrying),
                values.Sum(item => runsByJob.GetValueOrDefault(item.Id)),
                retried,
                terminal.Length == 0 ? null : Decimal.Divide(values.Count(item => item.Status == JobStatus.Failed), terminal.Length),
                terminal.Length == 0 ? null : Decimal.Divide(retried, terminal.Length),
                durations.Length == 0 ? null : (long)durations.Average());
        }).ToArray();
    }

    public async Task<VideoProjectObservabilityDto> GetVideoProjectAsync(Guid projectId, Guid videoProjectId, ObservabilityFilter filter, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var video = await db.VideoProjects.AsNoTracking().SingleOrDefaultAsync(item => item.Id == videoProjectId && item.ProjectId == projectId, cancellationToken)
            ?? throw new ResourceNotFoundException("Video project was not found.");
        var jobs = await FilterJobs(db.Jobs.AsNoTracking().Where(item => item.ProjectId == projectId && item.VideoProjectId == videoProjectId), filter).ToListAsync(cancellationToken);
        var runs = await FilterRuns(db.AiRuns.AsNoTracking().Where(item => item.ProjectId == projectId && item.VideoProjectId == videoProjectId), filter).ToListAsync(cancellationToken);
        var workflowNames = jobs.Select(item => item.Type).Concat(runs.Select(item => item.Workflow)).Distinct(StringComparer.Ordinal).OrderBy(item => item).ToArray();
        var summaries = workflowNames.Select(workflow =>
        {
            var workflowJobs = jobs.Where(item => item.Type == workflow).ToArray();
            var workflowRuns = runs.Where(item => item.Workflow == workflow || item.WorkflowStage == workflow).ToArray();
            var category = BuildCategory(workflowRuns);
            var durations = workflowJobs.Select(ExecutionMilliseconds).Where(item => item is not null).Select(item => item!.Value).ToArray();
            return new WorkflowExecutionSummaryDto(
                workflow,
                workflowJobs.Length,
                workflowRuns.Length,
                category.UnknownCostRequestCount,
                category.KnownOrEstimatedCost,
                durations.Length == 0 ? null : (long)durations.Average(),
                workflowJobs.OrderByDescending(item => item.CompletedAt ?? item.CreatedAt).Select(item => item.FailureReason).FirstOrDefault(item => item is not null));
        }).ToArray();
        var cost = BuildCategory(runs);
        return new VideoProjectObservabilityDto(
            video.Id,
            video.Status.ToString(),
            cost,
            jobs.Count(item => item.Status == JobStatus.Completed),
            jobs.Count(item => item.Status == JobStatus.Failed),
            summaries,
            BuildRecent(jobs, runs, 12));
    }

    public async Task<PagedResult<JobExecutionDto>> GetJobsAsync(Guid projectId, Guid? videoProjectId, int page, int pageSize, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureProjectAsync(db, projectId, cancellationToken);
        var query = db.Jobs.AsNoTracking().Where(item => item.ProjectId == projectId);
        if (videoProjectId is not null) query = query.Where(item => item.VideoProjectId == videoProjectId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<JobExecutionDto>(items.Select(ToDto).ToArray(), page, pageSize, total);
    }

    public async Task<PagedResult<AiRunExecutionDto>> GetAiRunsAsync(Guid projectId, Guid? videoProjectId, int page, int pageSize, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureProjectAsync(db, projectId, cancellationToken);
        var query = db.AiRuns.AsNoTracking().Where(item => item.ProjectId == projectId);
        if (videoProjectId is not null) query = query.Where(item => item.VideoProjectId == videoProjectId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.StartedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<AiRunExecutionDto>(items.Select(ToDto).ToArray(), page, pageSize, total);
    }

    private static IQueryable<AiRun> FilterRuns(IQueryable<AiRun> query, ObservabilityFilter filter)
    {
        if (filter.From is not null) query = query.Where(item => item.StartedAt >= filter.From);
        if (filter.To is not null) query = query.Where(item => item.StartedAt <= filter.To);
        if (!string.IsNullOrWhiteSpace(filter.Workflow)) query = query.Where(item => item.Workflow == filter.Workflow);
        if (!string.IsNullOrWhiteSpace(filter.ModelProfile)) query = query.Where(item => item.ModelProfile == filter.ModelProfile);
        return query;
    }

    private static IQueryable<Job> FilterJobs(IQueryable<Job> query, ObservabilityFilter filter)
    {
        if (filter.From is not null) query = query.Where(item => item.CreatedAt >= filter.From);
        if (filter.To is not null) query = query.Where(item => item.CreatedAt <= filter.To);
        return query;
    }

    private static AiCostBreakdownDto BuildCosts(IReadOnlyList<AiRun> runs) => new(
        BuildCategory(runs.Where(item => item.VideoProjectId is null)),
        BuildCategory(runs.Where(item => item.VideoProjectId is not null)),
        BuildCategory(runs),
        runs.GroupBy(item => item.WorkflowStage ?? item.Workflow).OrderBy(item => item.Key)
            .Select(item => ToCostGroup(item.Key, item)).ToArray(),
        runs.GroupBy(item => string.IsNullOrWhiteSpace(item.ModelProfile) ? "Unknown" : item.ModelProfile).OrderBy(item => item.Key)
            .Select(item => ToCostGroup(item.Key, item)).ToArray(),
        runs.GroupBy(item => $"{item.Provider} / {item.Model}").OrderBy(item => item.Key)
            .Select(item => ToCostGroup(item.Key, item)).ToArray(),
        runs.GroupBy(item => DateOnly.FromDateTime(item.StartedAt.UtcDateTime)).OrderBy(item => item.Key)
            .Select(item =>
            {
                var category = BuildCategory(item);
                return new DailyCostDto(item.Key, category.UnknownCostRequestCount, category.KnownOrEstimatedCost);
            }).ToArray());

    private static CostGroupDto ToCostGroup(string key, IEnumerable<AiRun> runs)
    {
        var category = BuildCategory(runs);
        return new CostGroupDto(key, category.AiRequestCount, category.UnknownCostRequestCount, category.KnownOrEstimatedCost);
    }

    private static AiCostCategoryDto BuildCategory(IEnumerable<AiRun> source)
    {
        var runs = source.ToArray();
        var known = runs.Select(GetKnownCost).Where(item => item is not null).Select(item => item!.Value).ToArray();
        return new AiCostCategoryDto(
            runs.Length,
            known.Length,
            runs.Length - known.Length,
            known.GroupBy(item => item.Currency).OrderBy(item => item.Key)
                .Select(item => new CostAmountDto(item.Key, item.Sum(value => value.Amount))).ToArray());
    }

    private static (string Currency, decimal Amount)? GetKnownCost(AiRun run)
    {
        var amount = run.ProviderReportedCost ?? run.CalculatedEstimatedCost ?? run.EstimatedCost;
        var currency = run.Currency ?? (run.EstimatedCost is not null ? LegacyEstimatedCostCurrency : null);
        return amount is not null && !string.IsNullOrWhiteSpace(currency)
            ? (currency, amount.Value)
            : null;
    }

    private static RecentExecutionDto[] BuildRecent(IEnumerable<Job> jobs, IEnumerable<AiRun> runs, int count) => jobs
        .Select(item => new RecentExecutionDto("Job", item.Id, item.Type, item.Status.ToString(), item.CompletedAt ?? item.StartedAt ?? item.CreatedAt, item.FailureReason, item.VideoProjectId))
        .Concat(runs.Select(item => new RecentExecutionDto("AiRun", item.Id, item.WorkflowStage ?? item.Workflow, item.Status.ToString(), item.CompletedAt ?? item.StartedAt, item.FailureReason, item.VideoProjectId)))
        .OrderByDescending(item => item.OccurredAt).Take(count).ToArray();

    private static JobExecutionDto ToDto(Job job) => new(job.Id, job.Type, job.Status.ToString(), job.VideoProjectId, job.CreatedAt,
        job.ExecutionStartedAt, job.CompletedAt, job.ExecutionStartedAt is null ? null : (long)(job.ExecutionStartedAt.Value - job.CreatedAt).TotalMilliseconds,
        ExecutionMilliseconds(job), job.CompletedAt is null || job.ExecutionStartedAt is null ? null : (long)(job.CompletedAt.Value - job.CreatedAt).TotalMilliseconds,
        job.RetryCount + (job.ExecutionStartedAt is null ? 0 : 1), job.RetryCount, job.FailureReason);

    private static AiRunExecutionDto ToDto(AiRun run)
    {
        var cost = GetKnownCost(run);
        return new AiRunExecutionDto(run.Id, run.JobId, run.VideoProjectId, run.Workflow, run.WorkflowStage, run.Status.ToString(),
            run.ModelProfile, run.Provider, run.Model, run.PromptKey, run.PromptVersion, run.StartedAt, run.CompletedAt,
            run.LatencyMilliseconds, run.InputTokens, run.OutputTokens, run.CachedInputTokens, run.ReasoningTokens,
            cost?.Amount, cost?.Currency, run.CostSource.ToString(), run.RetryCount, run.ErrorCategory, run.FailureReason);
    }

    private static long? ExecutionMilliseconds(Job job) => job.ExecutionStartedAt is not null && job.CompletedAt is not null
        ? (long)(job.CompletedAt.Value - job.ExecutionStartedAt.Value).TotalMilliseconds : null;

    private static string PipelineStage(string status) => status switch
    {
        "ResearchQueued" or "Researching" => "Research",
        "OutlineGenerating" or "OutlineReady" or "OutlineApproved" => "Outline",
        "ScriptGenerating" or "ScriptReady" or "ScriptApproved" => "Script",
        "Packaging" => "Production Package",
        "ProductionReady" => "Production Ready",
        _ => "Draft",
    };

    private static async Task EnsureProjectAsync(YoutubeAiFactoryDbContext db, Guid projectId, CancellationToken cancellationToken)
    {
        if (!await db.Projects.AsNoTracking().AnyAsync(item => item.Id == projectId, cancellationToken))
            throw new ResourceNotFoundException("Project was not found.");
    }
}
