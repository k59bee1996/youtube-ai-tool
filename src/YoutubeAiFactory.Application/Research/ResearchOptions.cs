namespace YoutubeAiFactory.Application.Research;

/// <summary>Bounded V1 research budgets. These controls apply before any external source content reaches an AI stage.</summary>
public sealed class ResearchOptions
{
    public const string SectionName = "Research";
    public int MaxJobRetries { get; init; } = 1;
    public int RunningJobLeaseSeconds { get; init; } = 900;
    public int MaxStructuredOutputRetries { get; init; } = 1;
    public int MaxResearchQueries { get; init; } = 6;
    public int MaxSearchResultsPerQuery { get; init; } = 5;
    public int MaxSourcesToFetch { get; init; } = 12;
    public int MaxRelevantSources { get; init; } = 8;
    public int MaxSourceCharacters { get; init; } = 12_000;
    public int MaxEvidenceItemsPerSource { get; init; } = 8;
    public int MaxTotalEvidenceItems { get; init; } = 40;
    public int MaxClaims { get; init; } = 30;
    public int MinUsableSources { get; init; } = 1;
    public int MinEvidenceItems { get; init; } = 1;
    public int MaxConcurrentSourceFetches { get; init; } = 3;
}
