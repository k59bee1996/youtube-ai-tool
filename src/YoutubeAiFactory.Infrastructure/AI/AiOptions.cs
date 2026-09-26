namespace YoutubeAiFactory.Infrastructure.AI;

public sealed class AiOptions
{
    public const string SectionName = "AI";
    public string ApiKey { get; init; } = string.Empty;
    public Dictionary<string, AiModelProfileOptions> Models { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<AiModelPricingOptions> Pricing { get; init; } = [];
}

/// <summary>
/// Optional, explicitly configured price metadata. No prices are shipped by the application.
/// Costs remain unavailable until an entry matching the provider, model and execution time exists.
/// </summary>
public sealed class AiModelPricingOptions
{
    public string Provider { get; init; } = "OpenAI";
    public string Model { get; init; } = string.Empty;
    public string Currency { get; init; } = "USD";
    public decimal? InputPricePerMillionTokens { get; init; }
    public decimal? OutputPricePerMillionTokens { get; init; }
    public decimal? CachedInputPricePerMillionTokens { get; init; }
    public DateTimeOffset EffectiveFrom { get; init; }
    public DateTimeOffset? EffectiveUntil { get; init; }
    public string PriceVersion { get; init; } = string.Empty;
}

public sealed class AiModelProfileOptions
{
    public string Provider { get; init; } = "OpenAI";
    public string Model { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 60;
    public int MaxOutputTokens { get; init; } = 5_000;
}
