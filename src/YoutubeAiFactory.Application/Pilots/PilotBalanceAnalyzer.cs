namespace YoutubeAiFactory.Application.Pilots;

public static class PilotBalanceAnalyzer
{
    public static IReadOnlyList<string> Analyze(PilotPlanResult plan, PilotGenerationContext context)
    {
        var selected = plan.Videos.Select(v => context.Ideas.Single(i => i.VideoIdeaId == v.VideoIdeaId)).ToArray();
        var warnings = new List<string>();
        if (selected.Take(4).Select(x => x.Topic).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1) warnings.Add("All Topic tests use the same subject; learning diversity is limited.");
        if (selected.Skip(4).Take(4).Select(x => x.HookConcept).Distinct(StringComparer.OrdinalIgnoreCase).Count() < 2) warnings.Add("Packaging experiments use nearly identical hook mechanisms.");
        if (selected.Take(4).Count(x => x.ProductionEase < 35) == 4) warnings.Add("All first-block videos have low production ease.");
        if (selected.Select(x => x.OpportunityId).Distinct().Count() == 1) warnings.Add("All pilot videos originate from one opportunity.");
        return warnings;
    }
}
