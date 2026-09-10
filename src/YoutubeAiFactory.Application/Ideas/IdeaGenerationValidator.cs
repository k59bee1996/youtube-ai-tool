using YoutubeAiFactory.Application.Common;

namespace YoutubeAiFactory.Application.Ideas;

public static class IdeaGenerationValidator
{
    public static void Validate(VideoIdeaCandidateResult item, IdeaGenerationContext context)
    {
        if (item is null || new[] { item.WorkingTitle, item.Topic, item.Angle, item.ContentFormat, item.TargetAudience, item.ViewerIntent, item.HookConcept, item.ThumbnailConcept, item.ViewerPromise, item.CoreQuestion, item.WhyViewerWouldCare, item.Hypothesis }.Any(string.IsNullOrWhiteSpace))
            throw new StructuredOutputException("Every idea identity, packaging, rationale, and hypothesis field is required.");
        if (item.Features is null || new[] { item.Features.Novelty, item.Features.TitlePotential, item.Features.ThumbnailPotential, item.Features.StoryPotential, item.Features.AudienceFit, item.Features.ProductionComplexity, item.Features.CompetitionRisk, item.Features.ResearchRisk, item.Confidence }.Any(value => value is < 0 or > 100))
            throw new StructuredOutputException("Idea feature and confidence scores must be between 0 and 100.");
        var validEvidence = context.Evidence.Select(x => x.OpportunityEvidenceId).ToHashSet();
        if (item.EvidenceIds is null || item.EvidenceIds.Count == 0 || item.EvidenceIds.Distinct().Count() != item.EvidenceIds.Count || item.EvidenceIds.Any(id => !validEvidence.Contains(id)))
            throw new StructuredOutputException("Idea evidence must reference unique supplied opportunity evidence IDs only.");
        if (item.Risks is null) throw new StructuredOutputException("Idea risks are required.");
    }
}
