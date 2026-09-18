namespace YoutubeAiFactory.Application.Scripts;

public sealed class ScriptOptions
{
    public const string SectionName = "Script";
    public int MaxJobRetries { get; init; } = 2;
    public int RunningJobLeaseSeconds { get; init; } = 900;
    public int MaxGenerationRetries { get; init; } = 1;
    public int MaxLengthCorrectionAttempts { get; init; } = 1;
    public int MaxGroundingCorrectionAttempts { get; init; } = 1;
    public int MaxStructuredRepairAttempts { get; init; } = 1;
    public int PlanningWordsPerMinute { get; init; } = 150;
    public int DefaultTargetMinutes { get; init; } = 8;
    public decimal MinimumLengthRatio { get; init; } = 0.8m;
    public decimal MaximumLengthRatio { get; init; } = 1.2m;
    public int MaxBlocksPerSection { get; init; } = 12;
    public int MaxNarrationCharactersPerBlock { get; init; } = 20_000;
}
