using YoutubeAiFactory.Application.Localization;
using YoutubeAiFactory.Application.Outlines;
using YoutubeAiFactory.Application.Research;
using YoutubeAiFactory.Application.Scripts;
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
        projects.MapPost("/video-projects/{videoProjectId:guid}/outline:generate", GenerateOutlineAsync);
        projects.MapGet("/video-projects/{videoProjectId:guid}/outline/latest", GetLatestOutlineAsync);
        projects.MapGet("/video-projects/{videoProjectId:guid}/outlines", GetOutlineHistoryAsync);
        projects.MapGet("/video-projects/{videoProjectId:guid}/outlines/{outlineId:guid}", GetOutlineAsync);
        projects.MapPatch("/video-projects/{videoProjectId:guid}/outlines/{outlineId:guid}", UpdateOutlineAsync);
        projects.MapPut("/video-projects/{videoProjectId:guid}/outlines/{outlineId:guid}/sections/order", ReorderOutlineAsync);
        projects.MapPost("/video-projects/{videoProjectId:guid}/outlines/{outlineId:guid}:approve", ApproveOutlineAsync);
        projects.MapPost("/video-projects/{videoProjectId:guid}/outlines/{outlineId:guid}/localizations/{locale}", RequestOutlineLocalizationAsync);
        projects.MapGet("/video-projects/{videoProjectId:guid}/outlines/{outlineId:guid}/localizations/{locale}", GetOutlineLocalizationAsync);
        projects.MapPost("/video-projects/{videoProjectId:guid}/script:generate", GenerateScriptAsync);
        projects.MapGet("/video-projects/{videoProjectId:guid}/script/latest", GetLatestScriptAsync);
        projects.MapGet("/video-projects/{videoProjectId:guid}/scripts", GetScriptHistoryAsync);
        projects.MapGet("/video-projects/{videoProjectId:guid}/scripts/{scriptId:guid}", GetScriptAsync);
        projects.MapPatch("/video-projects/{videoProjectId:guid}/scripts/{scriptId:guid}", UpdateScriptAsync);
        projects.MapPost("/video-projects/{videoProjectId:guid}/scripts/{scriptId:guid}:validate", ValidateScriptAsync);
        projects.MapPost("/video-projects/{videoProjectId:guid}/scripts/{scriptId:guid}:approve", ApproveScriptAsync);
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
    private static async Task<IResult> GenerateOutlineAsync(Guid projectId, Guid videoProjectId, RunVideoOutlineHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(projectId, videoProjectId, ct);
        return Results.Accepted($"/api/projects/{projectId}/video-projects/{videoProjectId}/outline/latest", result);
    }
    private static async Task<IResult> GetLatestOutlineAsync(Guid projectId, Guid videoProjectId,
        GetVideoOutlineStatusHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, videoProjectId, ct));
    private static async Task<IResult> GetOutlineHistoryAsync(Guid projectId, Guid videoProjectId,
        ListVideoOutlinesHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, videoProjectId, ct));
    private static async Task<IResult> GetOutlineAsync(Guid projectId, Guid videoProjectId, Guid outlineId,
        GetVideoOutlineHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, videoProjectId, outlineId, ct));
    private static async Task<IResult> UpdateOutlineAsync(Guid projectId, Guid videoProjectId, Guid outlineId,
        UpdateVideoOutlineRequest request, UpdateVideoOutlineHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.HandleAsync(projectId, videoProjectId, outlineId, request, ct));
    private static async Task<IResult> ReorderOutlineAsync(Guid projectId, Guid videoProjectId, Guid outlineId,
        ReorderVideoOutlineSectionsRequest request, ReorderVideoOutlineSectionsHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.HandleAsync(projectId, videoProjectId, outlineId, request, ct));
    private static async Task<IResult> ApproveOutlineAsync(Guid projectId, Guid videoProjectId, Guid outlineId,
        ApproveVideoOutlineHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.HandleAsync(projectId, videoProjectId, outlineId, ct));
    private static async Task<IResult> RequestOutlineLocalizationAsync(Guid projectId, Guid videoProjectId, Guid outlineId,
        string locale, RequestVideoOutlineLocalizationHandler handler, CancellationToken ct) =>
        Results.Accepted($"/api/projects/{projectId}/video-projects/{videoProjectId}/outlines/{outlineId}/localizations/{locale}",
            await handler.HandleAsync(projectId, videoProjectId, outlineId, locale, ct));
    private static async Task<IResult> GetOutlineLocalizationAsync(Guid projectId, Guid videoProjectId, Guid outlineId,
        string locale, GetVideoOutlineLocalizationHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.HandleAsync(projectId, videoProjectId, outlineId, locale, ct));
    private static async Task<IResult> GenerateScriptAsync(Guid projectId, Guid videoProjectId,
        RunVideoScriptHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(projectId, videoProjectId, ct);
        return Results.Accepted($"/api/projects/{projectId}/video-projects/{videoProjectId}/script/latest", result);
    }
    private static async Task<IResult> GetLatestScriptAsync(Guid projectId, Guid videoProjectId,
        GetVideoScriptStatusHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.HandleAsync(projectId, videoProjectId, ct));
    private static async Task<IResult> GetScriptHistoryAsync(Guid projectId, Guid videoProjectId,
        ListVideoScriptsHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.HandleAsync(projectId, videoProjectId, ct));
    private static async Task<IResult> GetScriptAsync(Guid projectId, Guid videoProjectId, Guid scriptId,
        GetVideoScriptHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.HandleAsync(projectId, videoProjectId, scriptId, ct));
    private static async Task<IResult> UpdateScriptAsync(Guid projectId, Guid videoProjectId, Guid scriptId,
        UpdateVideoScriptRequest request, UpdateVideoScriptHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.HandleAsync(projectId, videoProjectId, scriptId, request, ct));
    private static async Task<IResult> ValidateScriptAsync(Guid projectId, Guid videoProjectId, Guid scriptId,
        ValidateVideoScriptHandler handler, CancellationToken ct)
    {
        var result = await handler.HandleAsync(projectId, videoProjectId, scriptId, ct);
        return Results.Accepted($"/api/projects/{projectId}/video-projects/{videoProjectId}/script/latest", result);
    }
    private static async Task<IResult> ApproveScriptAsync(Guid projectId, Guid videoProjectId, Guid scriptId,
        ApproveVideoScriptHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.HandleAsync(projectId, videoProjectId, scriptId, ct));
}
