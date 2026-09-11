using YoutubeAiFactory.Application.Pilots;

namespace YoutubeAiFactory.Api.Endpoints;

internal static class PilotEndpoints
{
    public static IEndpointRouteBuilder MapPilotEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var projects = endpoints.MapGroup("/api/projects/{projectId:guid}").WithTags("Pilots");
        projects.MapPost("/pilots:generate", GenerateAsync);
        projects.MapGet("/pilots/latest", GetLatestAsync);
        projects.MapGet("/pilots", ListAsync);
        projects.MapGet("/pilots/eligible-ideas", ListCandidatesAsync);
        projects.MapGet("/pilots/{pilotId:guid}", GetAsync);
        projects.MapPost("/pilots/{pilotId:guid}:approve", ApproveAsync);
        projects.MapPost("/pilots/{pilotId:guid}/slots/{sequence:int}:replace", ReplaceAsync);
        projects.MapPost("/pilots/{pilotId:guid}/slots/{sequence:int}:move", MoveAsync);
        return endpoints;
    }
    private static async Task<IResult> GenerateAsync(Guid projectId, RunPilotGenerationHandler handler, CancellationToken ct) => Results.Accepted($"/api/projects/{projectId}/pilots/latest", await handler.HandleAsync(projectId, ct));
    private static async Task<IResult> GetLatestAsync(Guid projectId, GetPilotStatusHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, ct));
    private static async Task<IResult> ListAsync(Guid projectId, ListPilotsHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, ct));
    private static async Task<IResult> ListCandidatesAsync(Guid projectId, ListPilotCandidatesHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, ct));
    private static async Task<IResult> GetAsync(Guid projectId, Guid pilotId, GetPilotHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, pilotId, ct));
    private static async Task<IResult> ApproveAsync(Guid projectId, Guid pilotId, ApprovePilotHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, pilotId, ct));
    private static async Task<IResult> ReplaceAsync(Guid projectId, Guid pilotId, int sequence, ReplacePilotSlotRequest request, ReplacePilotSlotHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, pilotId, sequence, request, ct));
    private static async Task<IResult> MoveAsync(Guid projectId, Guid pilotId, int sequence, MovePilotSlotRequest request, MovePilotSlotHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, pilotId, sequence, request, ct));
}
