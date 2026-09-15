using YoutubeAiFactory.Application.Localization;
using YoutubeAiFactory.Application.Research;
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
        projects.MapPost("/video-projects/{videoProjectId:guid}/research:run", RunResearchAsync);
        projects.MapGet("/video-projects/{videoProjectId:guid}/research/latest", GetLatestResearchAsync);
        projects.MapGet("/video-projects/{videoProjectId:guid}/research", GetResearchHistoryAsync);
        projects.MapGet("/video-projects/{videoProjectId:guid}/research/{researchReportId:guid}", GetResearchAsync);
        projects.MapPost("/video-projects/{videoProjectId:guid}/research/{researchReportId:guid}/localizations/{locale}", RequestResearchLocalizationAsync);
        projects.MapGet("/video-projects/{videoProjectId:guid}/research/{researchReportId:guid}/localizations/{locale}", GetResearchLocalizationAsync);
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
    private static async Task<IResult> RunResearchAsync(Guid projectId, Guid videoProjectId, RunVideoResearchHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(projectId, videoProjectId, ct);
        return Results.Accepted($"/api/projects/{projectId}/video-projects/{videoProjectId}/research/latest", result);
    }
    private static async Task<IResult> GetLatestResearchAsync(Guid projectId, Guid videoProjectId, GetVideoResearchStatusHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.HandleAsync(projectId, videoProjectId, ct));
    private static async Task<IResult> GetResearchHistoryAsync(Guid projectId, Guid videoProjectId, ListVideoResearchReportsHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.HandleAsync(projectId, videoProjectId, ct));
    private static async Task<IResult> GetResearchAsync(Guid projectId, Guid videoProjectId, Guid researchReportId, GetVideoResearchReportHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.HandleAsync(projectId, videoProjectId, researchReportId, ct));
    private static async Task<IResult> RequestResearchLocalizationAsync(Guid projectId, Guid videoProjectId, Guid researchReportId, string locale, RequestResearchReportLocalizationHandler handler, CancellationToken ct) =>
        Results.Accepted($"/api/projects/{projectId}/video-projects/{videoProjectId}/research/{researchReportId}/localizations/{locale}", await handler.HandleAsync(projectId, videoProjectId, researchReportId, locale, ct));
    private static async Task<IResult> GetResearchLocalizationAsync(Guid projectId, Guid videoProjectId, Guid researchReportId, string locale, GetResearchReportLocalizationHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.HandleAsync(projectId, videoProjectId, researchReportId, locale, ct));
}
