using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Localization;

namespace YoutubeAiFactory.Api.Endpoints;

internal static class CompetitorEndpoints
{
    public static IEndpointRouteBuilder MapCompetitorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/projects/{projectId:guid}/competitors")
            .WithTags("Competitors");

        group.MapPost("/", AddCompetitorAsync);
        group.MapGet("/", ListCompetitorsAsync);
        group.MapGet("/{competitorId:guid}", GetCompetitorAsync);
        group.MapPost("/{competitorId:guid}/analysis:run", RunAnalysisAsync);
        group.MapGet("/{competitorId:guid}/analysis", GetAnalysisAsync);
        group.MapGet("/{competitorId:guid}/analysis/{analysisId:guid}/localizations/{locale}", GetAnalysisLocalizationAsync);
        group.MapPost("/{competitorId:guid}/analysis/{analysisId:guid}/localizations/{locale}", RequestAnalysisLocalizationAsync);

        return endpoints;
    }

    private static async Task<IResult> AddCompetitorAsync(
        Guid projectId,
        AddCompetitorRequest request,
        AddCompetitorHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new AddCompetitorCommand(projectId, request.YoutubeUrl),
            cancellationToken);

        return result.Created
            ? Results.Created(
                $"/api/projects/{projectId}/competitors/{result.Competitor.Id}",
                result.Competitor)
            : Results.Ok(result.Competitor);
    }

    private static async Task<IResult> ListCompetitorsAsync(
        Guid projectId,
        ListCompetitorsHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(await handler.HandleAsync(projectId, cancellationToken));

    private static async Task<IResult> GetCompetitorAsync(
        Guid projectId,
        Guid competitorId,
        GetCompetitorHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(await handler.HandleAsync(projectId, competitorId, cancellationToken));

    private static async Task<IResult> RunAnalysisAsync(
        Guid projectId,
        Guid competitorId,
        RunCompetitorAnalysisHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, competitorId, cancellationToken);
        return Results.Accepted($"/api/projects/{projectId}/competitors/{competitorId}/analysis", result);
    }

    private static async Task<IResult> GetAnalysisAsync(
        Guid projectId,
        Guid competitorId,
        GetCompetitorAnalysisStatusHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(await handler.HandleAsync(projectId, competitorId, cancellationToken));

    private static async Task<IResult> GetAnalysisLocalizationAsync(Guid projectId, Guid competitorId, Guid analysisId, string locale,
        GetCompetitorAnalysisLocalizationHandler handler, CancellationToken cancellationToken)
    {
        return Results.Ok(await handler.HandleAsync(projectId, competitorId, analysisId, locale, cancellationToken));
    }

    private static async Task<IResult> RequestAnalysisLocalizationAsync(Guid projectId, Guid competitorId, Guid analysisId, string locale,
        RequestCompetitorAnalysisLocalizationHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(projectId, competitorId, analysisId, locale, cancellationToken);
        return result.Existing ? Results.Ok(result) : Results.Accepted(value: result);
    }

    private sealed record AddCompetitorRequest(string YoutubeUrl);
}
