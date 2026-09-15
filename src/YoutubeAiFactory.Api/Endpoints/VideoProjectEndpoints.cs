using YoutubeAiFactory.Application.Videos;

namespace YoutubeAiFactory.Api.Endpoints;

internal static class VideoProjectEndpoints
{
    public static IEndpointRouteBuilder MapVideoProjectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var projects = endpoints.MapGroup("/api/projects/{projectId:guid}").WithTags("Video Projects");
        projects.MapPost("/pilots/{pilotId:guid}/video-projects", CreateAsync);
        projects.MapGet("/video-projects", ListAsync);
        projects.MapGet("/video-projects/{videoProjectId:guid}", GetAsync);
        projects.MapPatch("/video-projects/{videoProjectId:guid}", UpdateAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(Guid projectId, Guid pilotId, CreateVideoProjectRequest request, CreateVideoProjectHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(projectId, pilotId, request, ct);
        return Results.Created($"/api/projects/{projectId}/video-projects/{result.Id}", result);
    }
    private static async Task<IResult> ListAsync(Guid projectId, ListVideoProjectsHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, ct));
    private static async Task<IResult> GetAsync(Guid projectId, Guid videoProjectId, GetVideoProjectHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, videoProjectId, ct));
    private static async Task<IResult> UpdateAsync(Guid projectId, Guid videoProjectId, UpdateVideoProjectRequest request, UpdateVideoProjectHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, videoProjectId, request, ct));
}
