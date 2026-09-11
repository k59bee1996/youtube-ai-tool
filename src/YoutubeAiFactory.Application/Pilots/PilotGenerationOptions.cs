namespace YoutubeAiFactory.Application.Pilots;

public sealed class PilotGenerationOptions
{
    public const int RequiredIdeaCount = 12;
    public int MaxCandidateIdeas { get; init; } = 40;
    public int MaxStructuredOutputRetries { get; init; } = 1;
    public int RunningJobLeaseSeconds { get; init; } = 300;
}
