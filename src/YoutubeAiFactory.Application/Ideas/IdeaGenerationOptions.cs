namespace YoutubeAiFactory.Application.Ideas;

public sealed class IdeaGenerationOptions
{
    public int TargetIdeaCount { get; init; } = 25;
    public int MinIdeaCount { get; init; } = 20;
    public int MaxGeneratedCandidates { get; init; } = 30;
    public int MaxEvidenceItemsForIdeaGeneration { get; init; } = 20;
    public int MaxExistingIdeasForDedupContext { get; init; } = 50;
    public int MaxStructuredOutputRetries { get; init; } = 1;
    public int MaxReplacementAttempts { get; init; } = 2;
    public int MaxJobRetries { get; init; } = 3;
    public int RunningJobLeaseSeconds { get; init; } = 300;
    public decimal NearDuplicateThreshold { get; init; } = .72m;
    public decimal CompetitorTitleThreshold { get; init; } = .72m;
}
