namespace YoutubeAiFactory.Application.Pilots;

public sealed class PilotGenerationOptions
{
    public const int RequiredIdeaCount = 12;
    public int MaxCandidateIdeas { get; init; } = 40;
    public int MaxStructuredOutputRetries { get; init; } = 1;
    public int RunningJobLeaseSeconds { get; init; } = 300;

    public void Validate()
    {
        if (MaxCandidateIdeas is < RequiredIdeaCount or > 40)
            throw new ArgumentOutOfRangeException(nameof(MaxCandidateIdeas), $"PilotGeneration:MaxCandidateIdeas must be between {RequiredIdeaCount} and 40.");
        if (MaxStructuredOutputRetries is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(MaxStructuredOutputRetries), "PilotGeneration:MaxStructuredOutputRetries must be between 0 and 3.");
        if (RunningJobLeaseSeconds is < 30 or > 3_600)
            throw new ArgumentOutOfRangeException(nameof(RunningJobLeaseSeconds), "PilotGeneration:RunningJobLeaseSeconds must be between 30 and 3600.");
    }
}
