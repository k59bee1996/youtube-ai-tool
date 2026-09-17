using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Outlines;

public sealed class VideoOutlineSectionClaim
{
    private VideoOutlineSectionClaim() { }

    public VideoOutlineSectionClaim(Guid outlineSectionId, Guid researchClaimId, OutlineClaimUsageRole usageRole)
    {
        OutlineSectionId = Guard.NotEmpty(outlineSectionId, nameof(outlineSectionId));
        ResearchClaimId = Guard.NotEmpty(researchClaimId, nameof(researchClaimId));
        if (!Enum.IsDefined(usageRole)) throw new DomainException("Outline claim usage role is invalid.");
        UsageRole = usageRole;
    }

    public Guid OutlineSectionId { get; private set; }
    public Guid ResearchClaimId { get; private set; }
    public OutlineClaimUsageRole UsageRole { get; private set; }
}
