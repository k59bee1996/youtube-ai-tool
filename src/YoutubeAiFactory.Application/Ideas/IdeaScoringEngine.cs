namespace YoutubeAiFactory.Application.Ideas;

public static class IdeaScoringEngine
{
    public const string AlgorithmVersion = "idea-score:v1";
    public static IdeaScoreBreakdown Calculate(VideoIdeaCandidateResult candidate, IdeaGenerationContext context, decimal duplicationPenalty)
    {
        var evidenceStrength = Math.Clamp(context.OpportunityEvidenceStrength + Math.Min(20, candidate.EvidenceIds.Distinct().Count() * 7), 0, 100);
        var productionEase = 100 - candidate.Features.ProductionComplexity;
        var overall = Clamp(context.OpportunityScore * .14m + context.ObservedDemandAlignment * .13m + candidate.Features.Novelty * .13m + candidate.Features.TitlePotential * .11m + candidate.Features.ThumbnailPotential * .11m + candidate.Features.StoryPotential * .11m + candidate.Features.AudienceFit * .10m + evidenceStrength * .09m + productionEase * .08m - candidate.Features.CompetitionRisk * .06m - candidate.Features.ResearchRisk * .04m - duplicationPenalty);
        return new(context.OpportunityScore, context.ObservedDemandAlignment, candidate.Features.Novelty, candidate.Features.TitlePotential, candidate.Features.ThumbnailPotential, candidate.Features.StoryPotential, candidate.Features.AudienceFit, evidenceStrength, productionEase, candidate.Features.CompetitionRisk, candidate.Features.ResearchRisk, duplicationPenalty, overall);
    }
    private static decimal Clamp(decimal value) => decimal.Round(Math.Clamp(value, 0m, 100m), 2);
}
