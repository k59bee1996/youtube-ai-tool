namespace YoutubeAiFactory.Application.Competitors;

public sealed class CompetitorAnalysisOptions
{
    public const int MaximumVideoLimit = 50;
    public int MaxVideos { get; init; } = 30;
    public int MinimumVideos { get; init; } = 1;
    public int MaxStructuredOutputRetries { get; init; } = 1;
    public int MaxJobRetries { get; init; } = 3;
    public int RunningJobLeaseSeconds { get; init; } = 300;
}
