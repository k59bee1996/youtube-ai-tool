namespace YoutubeAiFactory.Infrastructure.Research;

public sealed class ResearchFetchOptions
{
    public const string SectionName = "ResearchFetch";
    public int TimeoutSeconds { get; init; } = 20;
    public int MaxRedirects { get; init; } = 3;
    public int MaxResponseBytes { get; init; } = 1_000_000;
    public int MaxExtractedCharacters { get; init; } = 12_000;
}
