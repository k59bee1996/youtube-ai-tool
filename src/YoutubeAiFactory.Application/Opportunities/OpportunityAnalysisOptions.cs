namespace YoutubeAiFactory.Application.Opportunities;

public sealed class OpportunityAnalysisOptions
{
    public int MaxCompetitors { get; init; } = 8;
    public int MaxEvidenceItems { get; init; } = 60;
    public int MaxCandidates { get; init; } = 8;
    public int MaxStructuredOutputRetries { get; init; } = 1;
    public int MaxJobRetries { get; init; } = 3;
    public int RunningJobLeaseSeconds { get; init; } = 300;
}
