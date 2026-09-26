using YoutubeAiFactory.Domain.AI;

namespace YoutubeAiFactory.Application.AI;

/// <summary>Maps one provider result to one immutable provider-request accounting record.</summary>
public static class AiRunTelemetryExtensions
{
    public static void CompleteFrom<T>(this AiRun run, LlmResult<T> result, DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(result);

        run.Complete(
            result.InputTokens,
            result.OutputTokens,
            result.CalculatedEstimatedCost,
            completedAt,
            result.CachedInputTokens,
            result.ReasoningTokens,
            result.ProviderReportedCost,
            result.Currency,
            result.PricingVersion,
            result.PricingEffectiveFrom);
    }
}
