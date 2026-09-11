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
    public decimal OpportunityFitWeight { get; init; } = .14m;
    public decimal ObservedDemandAlignmentWeight { get; init; } = .13m;
    public decimal NoveltyWeight { get; init; } = .13m;
    public decimal TitlePotentialWeight { get; init; } = .11m;
    public decimal ThumbnailPotentialWeight { get; init; } = .11m;
    public decimal StoryPotentialWeight { get; init; } = .11m;
    public decimal AudienceFitWeight { get; init; } = .10m;
    public decimal EvidenceStrengthWeight { get; init; } = .09m;
    public decimal ProductionEaseWeight { get; init; } = .08m;
    public decimal CompetitionRiskPenaltyWeight { get; init; } = .06m;
    public decimal ResearchRiskPenaltyWeight { get; init; } = .04m;
    public int EvidenceSupportBonusPerItem { get; init; } = 7;
    public int MaxEvidenceSupportBonus { get; init; } = 20;
}
