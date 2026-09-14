using YoutubeAiFactory.Application.Opportunities;
using YoutubeAiFactory.Application.Localization;

namespace YoutubeAiFactory.Api.Endpoints;

internal static class OpportunityEndpoints
{
    public static IEndpointRouteBuilder MapOpportunityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/projects/{projectId:guid}").WithTags("Opportunities");
        group.MapPost("/opportunities:generate", RunAsync);
        group.MapGet("/opportunities/latest", GetLatestAsync);
        group.MapGet("/opportunities/{reportId:guid}/localizations/{locale}", GetLocalizationAsync);
        group.MapPost("/opportunities/{reportId:guid}/localizations/{locale}", RequestLocalizationAsync);
        return endpoints;
    }
    private static async Task<IResult> RunAsync(Guid projectId, RunOpportunityAnalysisHandler handler, CancellationToken cancellationToken) =>
        Results.Accepted($"/api/projects/{projectId}/opportunities/latest", await handler.HandleAsync(projectId, cancellationToken));
    private static async Task<IResult> GetLatestAsync(Guid projectId, GetOpportunityStatusHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(await handler.HandleAsync(projectId, cancellationToken));

    private static async Task<IResult> GetLocalizationAsync(Guid projectId, Guid reportId, string locale,
        GetOpportunityReportLocalizationHandler handler, CancellationToken cancellationToken) =>
        Results.Ok(await handler.HandleAsync(projectId, reportId, locale, cancellationToken));

    private static async Task<IResult> RequestLocalizationAsync(Guid projectId, Guid reportId, string locale,
        RequestOpportunityReportLocalizationHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, reportId, locale, cancellationToken);
        return result.Existing ? Results.Ok(result) : Results.Accepted(value: result);
    }
}
