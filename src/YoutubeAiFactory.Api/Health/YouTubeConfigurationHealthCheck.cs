using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Infrastructure.YouTube;

namespace YoutubeAiFactory.Api.Health;

internal sealed class YouTubeConfigurationHealthCheck(
    IOptions<YouTubeOptions> options,
    CompetitorCollectionOptions collectionOptions)
    : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var result = collectionOptions.VideoLimit is < 1 or > CompetitorCollectionOptions.MaximumVideoLimit
            ? HealthCheckResult.Unhealthy(
                $"CompetitorCollection:VideoLimit must be between 1 and {CompetitorCollectionOptions.MaximumVideoLimit}.")
            : string.IsNullOrWhiteSpace(options.Value.ApiKey)
                ? HealthCheckResult.Degraded("YouTube:ApiKey is not configured.")
                : HealthCheckResult.Healthy("YouTube collection is configured.");

        return Task.FromResult(result);
    }
}
