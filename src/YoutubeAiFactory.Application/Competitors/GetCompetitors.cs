using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Persistence;

namespace YoutubeAiFactory.Application.Competitors;

public sealed class ListCompetitorsHandler(IYoutubeAiFactoryStore store)
{
    public async Task<IReadOnlyList<CompetitorSummaryDto>> HandleAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!await store.ProjectExistsAsync(projectId, cancellationToken))
        {
            throw new ResourceNotFoundException($"Project '{projectId}' was not found.");
        }

        var competitors = await store.ListCompetitorsAsync(projectId, cancellationToken);
        return competitors.Select(CompetitorDetailsDto.ToSummary).ToArray();
    }
}

public sealed class GetCompetitorHandler(IYoutubeAiFactoryStore store)
{
    public async Task<CompetitorDetailsDto> HandleAsync(
        Guid projectId,
        Guid competitorId,
        CancellationToken cancellationToken)
    {
        var competitor = await store.GetCompetitorAsync(
            projectId,
            competitorId,
            forUpdate: false,
            cancellationToken)
            ?? throw new ResourceNotFoundException(
                $"Competitor '{competitorId}' was not found in project '{projectId}'.");

        return CompetitorDetailsDto.FromDomain(competitor);
    }
}
