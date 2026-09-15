using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Research;

public sealed class ResearchEvidence
{
    private ResearchEvidence() { }

    public ResearchEvidence(Guid researchRunId, Guid researchSourceId, ResearchEvidenceType type, string fact,
        string supportingExcerpt, string sourceLocator, decimal confidence, DateTimeOffset createdAt)
    {
        if (confidence is < 0 or > 100) throw new DomainException("Evidence confidence must be between 0 and 100.");
        Id = Guid.NewGuid(); ResearchRunId = Guard.NotEmpty(researchRunId, nameof(researchRunId));
        ResearchSourceId = Guard.NotEmpty(researchSourceId, nameof(researchSourceId)); Type = type;
        Fact = Guard.Required(fact, nameof(fact), 4_000); SupportingExcerpt = Guard.Required(supportingExcerpt, nameof(supportingExcerpt), 2_000);
        SourceLocator = Guard.Required(sourceLocator, nameof(sourceLocator), 500); Confidence = confidence; CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ResearchRunId { get; private set; }
    public Guid ResearchSourceId { get; private set; }
    public ResearchEvidenceType Type { get; private set; }
    public string Fact { get; private set; } = string.Empty;
    public string SupportingExcerpt { get; private set; } = string.Empty;
    public string SourceLocator { get; private set; } = string.Empty;
    public decimal Confidence { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
