namespace YoutubeAiFactory.Application.AI;

public sealed record LlmResult<T>(
    T Value,
    string Provider,
    string Model,
    int? InputTokens,
    int? OutputTokens,
    string? RawOutput,
    int? CachedInputTokens = null,
    int? ReasoningTokens = null,
    decimal? ProviderReportedCost = null,
    decimal? CalculatedEstimatedCost = null,
    string? Currency = null,
    string? PricingVersion = null,
    DateTimeOffset? PricingEffectiveFrom = null);
