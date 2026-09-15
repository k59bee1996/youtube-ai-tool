using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Research;

public sealed class ResearchClaimEvidence
{
    private ResearchClaimEvidence() { }

    public ResearchClaimEvidence(Guid researchClaimId, Guid researchEvidenceId, ResearchEvidenceStance stance)
    {
        ResearchClaimId = Guard.NotEmpty(researchClaimId, nameof(researchClaimId));
        ResearchEvidenceId = Guard.NotEmpty(researchEvidenceId, nameof(researchEvidenceId));
        Stance = stance;
    }

    public Guid ResearchClaimId { get; private set; }
    public Guid ResearchEvidenceId { get; private set; }
    public ResearchEvidenceStance Stance { get; private set; }
}
