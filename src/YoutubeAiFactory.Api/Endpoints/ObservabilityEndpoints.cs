using YoutubeAiFactory.Application.Observability;

namespace YoutubeAiFactory.Api.Endpoints;

internal static class ObservabilityEndpoints
{
    public static IEndpointRouteBuilder MapObservabilityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var projects = endpoints.MapGroup("/api/projects/{projectId:guid}").WithTags("Observability");
        projects.MapGet("/observability/overview", GetOverviewAsync);
        projects.MapGet("/observability/costs", GetCostsAsync);
        projects.MapGet("/observability/workflows", GetWorkflowsAsync);
        projects.MapGet("/observability/jobs", GetJobsAsync);
        projects.MapGet("/observability/ai-runs", GetAiRunsAsync);
        projects.MapGet("/video-projects/{videoProjectId:guid}/observability", GetVideoProjectAsync);
        return endpoints;
    }

    private static Task<ProjectObservabilityOverviewDto> GetOverviewAsync(Guid projectId, DateTimeOffset? from, DateTimeOffset? to,
        string? workflow, string? modelProfile, IObservabilityQueries queries, CancellationToken cancellationToken) =>
        queries.GetProjectOverviewAsync(projectId, Filter(from, to, workflow, modelProfile), cancellationToken);

    private static Task<AiCostBreakdownDto> GetCostsAsync(Guid projectId, DateTimeOffset? from, DateTimeOffset? to,
        string? workflow, string? modelProfile, IObservabilityQueries queries, CancellationToken cancellationToken) =>
        queries.GetProjectCostsAsync(projectId, Filter(from, to, workflow, modelProfile), cancellationToken);

    private static Task<IReadOnlyList<WorkflowHealthDto>> GetWorkflowsAsync(Guid projectId, DateTimeOffset? from, DateTimeOffset? to,
        string? workflow, string? modelProfile, IObservabilityQueries queries, CancellationToken cancellationToken) =>
        queries.GetWorkflowHealthAsync(projectId, Filter(from, to, workflow, modelProfile), cancellationToken);

    private static Task<VideoProjectObservabilityDto> GetVideoProjectAsync(Guid projectId, Guid videoProjectId, DateTimeOffset? from,
        DateTimeOffset? to, string? workflow, string? modelProfile, IObservabilityQueries queries, CancellationToken cancellationToken) =>
        queries.GetVideoProjectAsync(projectId, videoProjectId, Filter(from, to, workflow, modelProfile), cancellationToken);

    private static Task<PagedResult<JobExecutionDto>> GetJobsAsync(Guid projectId, Guid? videoProjectId, int? page,
        int? pageSize, IObservabilityQueries queries, CancellationToken cancellationToken) =>
        queries.GetJobsAsync(projectId, videoProjectId, NormalizePage(page), NormalizePageSize(pageSize), cancellationToken);

    private static Task<PagedResult<AiRunExecutionDto>> GetAiRunsAsync(Guid projectId, Guid? videoProjectId, int? page,
        int? pageSize, IObservabilityQueries queries, CancellationToken cancellationToken) =>
        queries.GetAiRunsAsync(projectId, videoProjectId, NormalizePage(page), NormalizePageSize(pageSize), cancellationToken);

    private static ObservabilityFilter Filter(DateTimeOffset? from, DateTimeOffset? to, string? workflow, string? modelProfile)
    {
        if (from is not null && to is not null && from > to)
            throw new BadHttpRequestException("The 'from' value must be before or equal to 'to'.");
        return new ObservabilityFilter(from, to, workflow, modelProfile);
    }

    private static int NormalizePage(int? value) => value is null or < 1 ? 1 : value.Value;

    private static int NormalizePageSize(int? value)
    {
        if (value is null) return 25;
        if (value is < 1 or > 100) throw new BadHttpRequestException("pageSize must be between 1 and 100.");
        return value.Value;
    }
}
