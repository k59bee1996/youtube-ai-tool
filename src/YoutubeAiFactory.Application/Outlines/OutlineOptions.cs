namespace YoutubeAiFactory.Application.Outlines;

public sealed class OutlineOptions
{
    public const string SectionName = "Outline";
    public int MaxJobRetries { get; init; } = 2;
    public int RunningJobLeaseSeconds { get; init; } = 600;
    public int MaxGenerationRetries { get; init; } = 1;
    public int MaxStructuredRepairAttempts { get; init; } = 1;
    public int MinOutlineSections { get; init; } = 4;
    public int MaxOutlineSections { get; init; } = 12;
    public int MaxClaimsForOutline { get; init; } = 30;
    public int MaxConflictItemsForOutline { get; init; } = 10;
    public int MaxResearchGapsForOutline { get; init; } = 10;
    public int MaxEvidenceExcerptsForOutline { get; init; } = 40;
    public int MaxKeyFindingsForOutline { get; init; } = 20;
}
