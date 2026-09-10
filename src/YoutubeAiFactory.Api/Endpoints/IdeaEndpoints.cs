using YoutubeAiFactory.Application.Ideas;
using YoutubeAiFactory.Application.Opportunities;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;

namespace YoutubeAiFactory.Api.Endpoints;

internal static class IdeaEndpoints
{
    public static IEndpointRouteBuilder MapIdeaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var projects = endpoints.MapGroup("/api/projects/{projectId:guid}").WithTags("Ideas");
        projects.MapPost("/opportunities/{opportunityId:guid}/ideas:generate", GenerateAsync);
        projects.MapGet("/opportunities/{opportunityId:guid}/ideas", GetBankAsync);
        projects.MapGet("/opportunities/{opportunityId:guid}/idea-generations", GetGenerationHistoryAsync);
        projects.MapGet("/idea-generations/{generationId:guid}", GetGenerationAsync);
        projects.MapPost("/opportunities/{opportunityId:guid}:approve", (Guid projectId, Guid opportunityId, SetOpportunityDecisionHandler handler, CancellationToken ct) => SetOpportunityAsync(projectId, opportunityId, OpportunityDecisionStatus.Approved, handler, ct));
        projects.MapPost("/opportunities/{opportunityId:guid}:reject", (Guid projectId, Guid opportunityId, SetOpportunityDecisionHandler handler, CancellationToken ct) => SetOpportunityAsync(projectId, opportunityId, OpportunityDecisionStatus.Rejected, handler, ct));
        projects.MapPost("/ideas/{ideaId:guid}:approve", (Guid projectId, Guid ideaId, SetIdeaDecisionHandler handler, CancellationToken ct) => SetIdeaAsync(projectId, ideaId, IdeaDecisionStatus.Approved, handler, ct));
        projects.MapPost("/ideas/{ideaId:guid}:reject", (Guid projectId, Guid ideaId, SetIdeaDecisionHandler handler, CancellationToken ct) => SetIdeaAsync(projectId, ideaId, IdeaDecisionStatus.Rejected, handler, ct));
        return endpoints;
    }
    private static async Task<IResult> GenerateAsync(Guid projectId, Guid opportunityId, RunIdeaGenerationHandler handler, CancellationToken cancellationToken) => Results.Accepted($"/api/projects/{projectId}/opportunities/{opportunityId}/ideas", await handler.HandleAsync(projectId, opportunityId, cancellationToken));
    private static async Task<IResult> GetBankAsync(Guid projectId, Guid opportunityId, GetIdeaBankHandler handler, CancellationToken cancellationToken) => Results.Ok(await handler.HandleAsync(projectId, opportunityId, cancellationToken));
    private static async Task<IResult> GetGenerationHistoryAsync(Guid projectId, Guid opportunityId, GetIdeaBankHandler handler, CancellationToken cancellationToken) => Results.Ok((await handler.HandleAsync(projectId, opportunityId, cancellationToken)).Generations);
    private static async Task<IResult> GetGenerationAsync(Guid projectId, Guid generationId, GetIdeaGenerationHandler handler, CancellationToken cancellationToken) => Results.Ok(await handler.HandleAsync(projectId, generationId, cancellationToken));
    private static async Task<IResult> SetOpportunityAsync(Guid projectId, Guid opportunityId, OpportunityDecisionStatus decision, SetOpportunityDecisionHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, opportunityId, decision, ct));
    private static async Task<IResult> SetIdeaAsync(Guid projectId, Guid ideaId, IdeaDecisionStatus decision, SetIdeaDecisionHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(projectId, ideaId, decision, ct));
}
