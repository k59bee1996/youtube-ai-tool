using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Application.Ideas;

public sealed class IdeaGenerationContextBuilder(IdeaGenerationOptions options)
{
    public IdeaGenerationContext Build(Project project, ApprovedOpportunityWithEvidence opportunity, IReadOnlyList<ExistingIdeaContext> existingIdeas, IReadOnlyList<string> competitorTitles)
    {
        if (opportunity.Candidate.DecisionStatus != OpportunityDecisionStatus.Approved) throw new ApplicationValidationException("Ideas can be generated only from an approved opportunity.");
        if (options.MinIdeaCount is < 1 or > 30 || options.TargetIdeaCount < options.MinIdeaCount || options.TargetIdeaCount > options.MaxGeneratedCandidates || options.MaxGeneratedCandidates > 30 || options.MaxEvidenceItemsForIdeaGeneration is < 1 or > 100 || options.MaxExistingIdeasForDedupContext is < 0 or > 200)
            throw new ApplicationValidationException("Idea generation limits are outside the supported range.");
        var evidence = opportunity.Evidence.OrderBy(x => x.Id).Take(options.MaxEvidenceItemsForIdeaGeneration).Select(x => new IdeaEvidenceContext(x.Id, x.Summary)).ToArray();
        if (evidence.Length == 0) throw new ApplicationValidationException("The approved opportunity has no supporting evidence.");
        var candidate = opportunity.Candidate;
        return new(opportunity.Report.ProjectId, candidate.Id, candidate.ReportId, opportunity.Report.Version, project.Market.Name, project.Market.TargetLanguage, project.Market.TargetGeography, project.Audience.Description, candidate.Name, candidate.Description, candidate.Audience, candidate.Topic, candidate.ContentFormat, candidate.Angle, candidate.WhyThisOpportunity, (int)Math.Round(candidate.OverallScore), candidate.ObservedDemandSignal, candidate.EvidenceStrength, evidence,
            existingIdeas.Take(options.MaxExistingIdeasForDedupContext).ToArray(), competitorTitles.Take(100).ToArray(), ["Ideas are proposals based on the approved opportunity and its analyzed competitor dataset, not verified market forecasts."]);
    }
}
