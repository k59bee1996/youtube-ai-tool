using YoutubeAiFactory.Application.Opportunities;

namespace YoutubeAiFactory.Api.Endpoints;

internal static class OpportunityEndpoints
{
    public static IEndpointRouteBuilder MapOpportunityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/projects/{projectId:guid}").WithTags("Opportunities");
        group.MapPost("/opportunities:generate", RunAsync);
        group.MapGet("/opportunities/latest", GetLatestAsync);
        return endpoints;
    }
    private static async Task<IResult> RunAsync(Guid projectId, RunOpportunityAnalysisHandler handler, CancellationToken cancellationToken) =>
        Results.Accepted($"/api/projects/{projectId}/opportunities/latest", await handler.HandleAsync(projectId, cancellationToken));
    private static async Task<IResult> GetLatestAsync(Guid projectId, GetOpportunityStatusHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(await handler.HandleAsync(projectId, cancellationToken));
}
