using YoutubeAiFactory.Application.Common;

namespace YoutubeAiFactory.Application.Opportunities;

public static class OpportunityAnalysisValidator
{
    public static void Validate(OpportunityAnalysisResult result, OpportunityAnalysisContext context, int maximumCandidates)
    {
        if (result?.Opportunities is null || result.Opportunities.Count == 0) throw new StructuredOutputException("At least one opportunity is required.");
        if (result.Opportunities.Count > maximumCandidates) throw new StructuredOutputException("The response exceeds the candidate limit.");
        var validIds = context.Evidence.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in result.Opportunities)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Topic) || string.IsNullOrWhiteSpace(item.ContentFormat) || string.IsNullOrWhiteSpace(item.Angle) || string.IsNullOrWhiteSpace(item.WhyThisOpportunity))
                throw new StructuredOutputException("Opportunity identity and rationale fields are required.");
            foreach (var score in new[] { item.NoveltySignal, item.AudienceFitSignal, item.TransferabilitySignal, item.StoryPotential, item.ProductionComplexity, item.Confidence })
                if (score is < 0 or > 100) throw new StructuredOutputException("Opportunity score components must be between 0 and 100.");
            if (item.EvidenceIds is null || item.EvidenceIds.Count == 0 || item.EvidenceIds.Any(id => !validIds.Contains(id)))
                throw new StructuredOutputException("Opportunity evidence must reference supplied evidence IDs only.");
            var key = Normalize($"{item.Audience}|{item.Topic}|{item.ContentFormat}|{item.Angle}");
            if (!identities.Add(key)) throw new StructuredOutputException("The response contains duplicate opportunities.");
        }
    }
    private static string Normalize(string value) => string.Concat(value.ToLowerInvariant().Where(char.IsLetterOrDigit));
}
