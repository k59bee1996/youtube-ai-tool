using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Opportunities;

public sealed class OpportunityCandidate
{
    private OpportunityCandidate() { }
    public OpportunityCandidate(Guid reportId, string name, string description, string audience, string topic,
        string contentFormat, string angle, string whyThisOpportunity, int observedDemandSignal, int noveltySignal,
        int competitionRiskSignal, int audienceFitSignal, int transferabilitySignal, int evidenceStrength,
        int storyPotential, int productionComplexity, int confidence, decimal overallScore, string risksJson,
        string limitationsJson, DateTimeOffset createdAt)
    {
        foreach (var score in new[] { observedDemandSignal, noveltySignal, competitionRiskSignal, audienceFitSignal, transferabilitySignal, evidenceStrength, storyPotential, productionComplexity, confidence })
            if (score is < 0 or > 100) throw new DomainException("Opportunity scores must be between 0 and 100.");
        if (overallScore is < 0 or > 100) throw new DomainException("Overall opportunity score must be between 0 and 100.");
        Id = Guid.NewGuid(); ReportId = Guard.NotEmpty(reportId, nameof(reportId));
        Name = Guard.Required(name, nameof(name), 200); Description = Guard.Required(description, nameof(description), 4000);
        Audience = Guard.Required(audience, nameof(audience), 1000); Topic = Guard.Required(topic, nameof(topic), 500);
        ContentFormat = Guard.Required(contentFormat, nameof(contentFormat), 500); Angle = Guard.Required(angle, nameof(angle), 1000);
        WhyThisOpportunity = Guard.Required(whyThisOpportunity, nameof(whyThisOpportunity), 4000);
        ObservedDemandSignal = observedDemandSignal; NoveltySignal = noveltySignal; CompetitionRiskSignal = competitionRiskSignal;
        AudienceFitSignal = audienceFitSignal; TransferabilitySignal = transferabilitySignal; EvidenceStrength = evidenceStrength;
        StoryPotential = storyPotential; ProductionComplexity = productionComplexity; Confidence = confidence; OverallScore = overallScore;
        RisksJson = Guard.Required(risksJson, nameof(risksJson), 10000); LimitationsJson = Guard.Required(limitationsJson, nameof(limitationsJson), 10000);
        DecisionStatus = OpportunityDecisionStatus.Candidate; CreatedAt = createdAt;
    }
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Audience { get; private set; } = string.Empty;
    public string Topic { get; private set; } = string.Empty;
    public string ContentFormat { get; private set; } = string.Empty;
    public string Angle { get; private set; } = string.Empty;
    public string WhyThisOpportunity { get; private set; } = string.Empty;
    public int ObservedDemandSignal { get; private set; }
    public int NoveltySignal { get; private set; }
    public int CompetitionRiskSignal { get; private set; }
    public int AudienceFitSignal { get; private set; }
    public int TransferabilitySignal { get; private set; }
    public int EvidenceStrength { get; private set; }
    public int StoryPotential { get; private set; }
    public int ProductionComplexity { get; private set; }
    public int Confidence { get; private set; }
    public decimal OverallScore { get; private set; }
    public string RisksJson { get; private set; } = "[]";
    public string LimitationsJson { get; private set; } = "[]";
    public OpportunityDecisionStatus DecisionStatus { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public void SetDecision(OpportunityDecisionStatus decision) => DecisionStatus = decision;
}
