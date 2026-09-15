namespace YoutubeAiFactory.Infrastructure.Research;

public sealed class ResearchSearchOptions
{
    public const string SectionName = "ResearchSearch";
    public string Endpoint { get; init; } = "https://api.tavily.com/search";
    public string? ApiKey { get; init; }
    public int TimeoutSeconds { get; init; } = 20;
}
