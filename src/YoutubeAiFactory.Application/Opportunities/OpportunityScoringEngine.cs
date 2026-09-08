namespace YoutubeAiFactory.Application.Opportunities;

public sealed class OpportunityScoringEngine
{
    public const string AlgorithmVersion = "opportunity-score:v1";
    public static OpportunityScoreBreakdown Calculate(OpportunityCandidateResult candidate, OpportunityAnalysisContext context)
    {
        var support = context.Evidence.Where(item => candidate.EvidenceIds.Contains(item.Id, StringComparer.Ordinal)).ToArray();
        var demand = support.Length == 0 ? 0 : (int)Math.Round(support.Average(item => item.ObservedDemandSignal));
        var diversity = support.Select(item => item.CompetitorId).Distinct().Count();
        var strength = support.Length == 0 ? 0 : Clamp((int)Math.Round(support.Average(item => item.Confidence)) + Math.Max(0, diversity - 1) * 8);
        var competition = support.Length == 0 ? 100 : Clamp((int)Math.Round(100d * diversity / Math.Max(1, context.AnalyzedCompetitorCount)));
        var productionEase = 100 - candidate.ProductionComplexity;
        var overall = ClampDecimal(demand * .20m + candidate.NoveltySignal * .17m + candidate.AudienceFitSignal * .16m +
            candidate.TransferabilitySignal * .16m + strength * .16m + candidate.StoryPotential * .10m + productionEase * .05m - competition * .10m);
        return new OpportunityScoreBreakdown(demand, candidate.NoveltySignal, competition, candidate.AudienceFitSignal,
            candidate.TransferabilitySignal, strength, candidate.StoryPotential, candidate.ProductionComplexity, overall);
    }
    private static int Clamp(int score) => Math.Clamp(score, 0, 100);
    private static decimal ClampDecimal(decimal score) => decimal.Round(Math.Clamp(score, 0m, 100m), 2);
}
