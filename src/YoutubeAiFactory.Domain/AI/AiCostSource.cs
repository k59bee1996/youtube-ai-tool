namespace YoutubeAiFactory.Domain.AI;

/// <summary>Describes the provenance of a monetary amount recorded for an AI request.</summary>
public enum AiCostSource
{
    Unavailable,
    ProviderReported,
    PriceCalculated,
}
