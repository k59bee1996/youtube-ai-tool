namespace YoutubeAiFactory.Application.Production;

public sealed class ProductionOptions
{
    public const string SectionName = "ProductionPackage";
    public int MaxJobRetries { get; init; } = 2;
    public int RunningJobLeaseSeconds { get; init; } = 900;
    public int MaxGenerationRetries { get; init; } = 1;
    public int MaxGroundingCorrectionAttempts { get; init; } = 1;
    public int MaxStructuredRepairAttempts { get; init; } = 1;
    public int MaxScenes { get; init; } = 80;
    public int MaxShotsPerScene { get; init; } = 12;
    public int MaxAssets { get; init; } = 120;
    public int MaxGeneratedMotionAssets { get; init; } = 12;
    public int MaxCharts { get; init; } = 12;
    public int MaxMaps { get; init; } = 8;
    public int MaxOnScreenTextPerScene { get; init; } = 10;
}
