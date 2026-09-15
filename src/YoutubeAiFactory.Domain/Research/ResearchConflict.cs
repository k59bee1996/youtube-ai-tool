using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Research;

public sealed class ResearchConflict
{
    private ResearchConflict() { }

    public ResearchConflict(Guid researchReportId, Guid researchClaimId, Guid supportingEvidenceId,
        Guid contradictingEvidenceId, string explanation, bool isResolved, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid(); ResearchReportId = Guard.NotEmpty(researchReportId, nameof(researchReportId));
        ResearchClaimId = Guard.NotEmpty(researchClaimId, nameof(researchClaimId));
        SupportingEvidenceId = Guard.NotEmpty(supportingEvidenceId, nameof(supportingEvidenceId));
        ContradictingEvidenceId = Guard.NotEmpty(contradictingEvidenceId, nameof(contradictingEvidenceId));
        if (SupportingEvidenceId == ContradictingEvidenceId) throw new DomainException("A conflict needs two different evidence items.");
        Explanation = Guard.Required(explanation, nameof(explanation), 2_000); IsResolved = isResolved; CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ResearchReportId { get; private set; }
    public Guid ResearchClaimId { get; private set; }
    public Guid SupportingEvidenceId { get; private set; }
    public Guid ContradictingEvidenceId { get; private set; }
    public string Explanation { get; private set; } = string.Empty;
    public bool IsResolved { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
