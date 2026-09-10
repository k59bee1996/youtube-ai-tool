using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Ideas;

public sealed class IdeaEvidence
{
    private IdeaEvidence() { }
    public IdeaEvidence(Guid ideaId, Guid opportunityEvidenceId, string summary)
    {
        Id = Guid.NewGuid(); IdeaId = Guard.NotEmpty(ideaId, nameof(ideaId)); OpportunityEvidenceId = Guard.NotEmpty(opportunityEvidenceId, nameof(opportunityEvidenceId)); Summary = Guard.Required(summary, nameof(summary), 2000);
    }
    public Guid Id { get; private set; }
    public Guid IdeaId { get; private set; }
    public Guid OpportunityEvidenceId { get; private set; }
    public string Summary { get; private set; } = string.Empty;
}
