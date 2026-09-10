using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Opportunities;

public sealed class OpportunityEvidence
{
    private OpportunityEvidence() { }
    public OpportunityEvidence(Guid candidateId, Guid competitorChannelId, Guid competitorAnalysisId, Guid? competitorVideoId, string evidenceId, string summary)
    {
        Id = Guid.NewGuid(); CandidateId = Guard.NotEmpty(candidateId, nameof(candidateId));
        CompetitorChannelId = Guard.NotEmpty(competitorChannelId, nameof(competitorChannelId));
        CompetitorAnalysisId = Guard.NotEmpty(competitorAnalysisId, nameof(competitorAnalysisId));
        CompetitorVideoId = competitorVideoId; EvidenceId = Guard.Required(evidenceId, nameof(evidenceId), 200);
        Summary = Guard.Required(summary, nameof(summary), 2000);
    }
    public Guid Id { get; private set; }
    public Guid CandidateId { get; private set; }
    public Guid CompetitorChannelId { get; private set; }
    public Guid CompetitorAnalysisId { get; private set; }
    public Guid? CompetitorVideoId { get; private set; }
    public string EvidenceId { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
}
