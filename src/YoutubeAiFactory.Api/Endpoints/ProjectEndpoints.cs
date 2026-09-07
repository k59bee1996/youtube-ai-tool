using YoutubeAiFactory.Application.Projects;

namespace YoutubeAiFactory.Api.Endpoints;

internal static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/projects").WithTags("Projects");

        group.MapPost("/", CreateProjectAsync);
        group.MapGet("/", ListProjectsAsync);
        group.MapGet("/{projectId:guid}", GetProjectAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateProjectAsync(
        CreateProjectRequest request,
        CreateProjectHandler handler,
        CancellationToken cancellationToken)
    {
        var project = await handler.HandleAsync(
            new CreateProjectCommand(
                request.Name,
                request.MarketName,
                request.TargetLanguage,
                request.TargetGeography,
                request.AudienceDescription),
            cancellationToken);

        return Results.Created($"/api/projects/{project.Id}", project);
    }

    private static async Task<IResult> ListProjectsAsync(
        ListProjectsHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(await handler.HandleAsync(cancellationToken));

    private static async Task<IResult> GetProjectAsync(
        Guid projectId,
        GetProjectHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(await handler.HandleAsync(projectId, cancellationToken));

    private sealed record CreateProjectRequest(
        string Name,
        string MarketName,
        string TargetLanguage,
        string TargetGeography,
        string AudienceDescription);
}
