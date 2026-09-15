using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Research;

public sealed class ResearchClaim
{
    private ResearchClaim() { }

    public ResearchClaim(Guid researchReportId, string statement, ResearchClaimType type, decimal confidence, bool isCritical, DateTimeOffset createdAt)
    {
        if (confidence is < 0 or > 100) throw new DomainException("Claim confidence must be between 0 and 100.");
        Id = Guid.NewGuid(); ResearchReportId = Guard.NotEmpty(researchReportId, nameof(researchReportId));
        Statement = Guard.Required(statement, nameof(statement), 4_000); Type = type; Confidence = confidence;
        IsCritical = isCritical; SupportStatus = ResearchClaimSupportStatus.Unsupported; CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ResearchReportId { get; private set; }
    public string Statement { get; private set; } = string.Empty;
    public ResearchClaimType Type { get; private set; }
    public ResearchClaimSupportStatus SupportStatus { get; private set; }
    public decimal Confidence { get; private set; }
    public bool IsCritical { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public void SetSupportStatus(ResearchClaimSupportStatus status) => SupportStatus = status;
}
