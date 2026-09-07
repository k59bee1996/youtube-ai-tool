using YoutubeAiFactory.Application.Competitors;

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

    private sealed record AddCompetitorRequest(string YoutubeUrl);
}
