using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Domain.Projects;

namespace YoutubeAiFactory.Application.Pilots;

public sealed class PilotGenerationContextBuilder(PilotGenerationOptions options)
{
    public PilotGenerationContext Build(Project project, IReadOnlyList<PilotIdeaContext> approvedIdeas)
    {
        if (approvedIdeas.Count < PilotGenerationOptions.RequiredIdeaCount)
            throw new ApplicationValidationException($"Only {approvedIdeas.Count} approved ideas are available. At least 12 are required to generate a full pilot.");
        var bounded = approvedIdeas.OrderBy(x => x.TopicFrequency).ThenBy(x => x.OpportunityFrequency)
            .ThenByDescending(x => x.OverallScore).ThenByDescending(x => x.EvidenceStrength)
            .Take(options.MaxCandidateIdeas).ToArray();
        return new PilotGenerationContext(project.Id, project.Market.Name, project.Audience.Description, approvedIdeas.Count, bounded);
    }
}
