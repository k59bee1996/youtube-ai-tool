namespace YoutubeAiFactory.Application.Ideas;

public sealed class IdeaScoringEngine
{
    public const string AlgorithmVersion = "idea-score:v1";
    private readonly IdeaGenerationOptions options;

    public IdeaScoringEngine(IdeaGenerationOptions options)
    {
        this.options = options;
        var weights = new[] { options.OpportunityFitWeight, options.ObservedDemandAlignmentWeight, options.NoveltyWeight,
            options.TitlePotentialWeight, options.ThumbnailPotentialWeight, options.StoryPotentialWeight,
            options.AudienceFitWeight, options.EvidenceStrengthWeight, options.ProductionEaseWeight,
            options.CompetitionRiskPenaltyWeight, options.ResearchRiskPenaltyWeight };
        if (weights.Any(weight => weight < 0m) || options.EvidenceSupportBonusPerItem is < 0 or > 100 || options.MaxEvidenceSupportBonus is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(options), "Idea scoring configuration cannot contain negative weights or invalid evidence bonuses.");
    }

    public IdeaScoreBreakdown Calculate(VideoIdeaCandidateResult candidate, IdeaGenerationContext context, decimal duplicationPenalty)
    {
        var evidenceStrength = Math.Clamp(context.OpportunityEvidenceStrength + Math.Min(options.MaxEvidenceSupportBonus, candidate.EvidenceIds.Distinct().Count() * options.EvidenceSupportBonusPerItem), 0, 100);
        var productionEase = 100 - candidate.Features.ProductionComplexity;
        var overall = Clamp(context.OpportunityScore * options.OpportunityFitWeight + context.ObservedDemandAlignment * options.ObservedDemandAlignmentWeight + candidate.Features.Novelty * options.NoveltyWeight + candidate.Features.TitlePotential * options.TitlePotentialWeight + candidate.Features.ThumbnailPotential * options.ThumbnailPotentialWeight + candidate.Features.StoryPotential * options.StoryPotentialWeight + candidate.Features.AudienceFit * options.AudienceFitWeight + evidenceStrength * options.EvidenceStrengthWeight + productionEase * options.ProductionEaseWeight - candidate.Features.CompetitionRisk * options.CompetitionRiskPenaltyWeight - candidate.Features.ResearchRisk * options.ResearchRiskPenaltyWeight - duplicationPenalty);
        return new(context.OpportunityScore, context.ObservedDemandAlignment, candidate.Features.Novelty, candidate.Features.TitlePotential, candidate.Features.ThumbnailPotential, candidate.Features.StoryPotential, candidate.Features.AudienceFit, evidenceStrength, productionEase, candidate.Features.CompetitionRisk, candidate.Features.ResearchRisk, duplicationPenalty, overall);
    }
    private static decimal Clamp(decimal value) => decimal.Round(Math.Clamp(value, 0m, 100m), 2);
}
