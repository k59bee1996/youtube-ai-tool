namespace YoutubeAiFactory.Infrastructure.Research;

public sealed class ResearchSearchOptions
{
    public const string SectionName = "ResearchSearch";
    public string Endpoint { get; init; } = "https://api.bing.microsoft.com/v7.0/search";
    public string? ApiKey { get; init; }
    public int TimeoutSeconds { get; init; } = 20;
}
