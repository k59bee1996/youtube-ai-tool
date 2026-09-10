using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Ideas;

/// <summary>A concrete, structured video hypothesis. It deliberately is not a pilot or video project.</summary>
public sealed class VideoIdea
{
    private VideoIdea() { }
    public VideoIdea(Guid projectId, Guid opportunityId, Guid generationId, string workingTitle, string topic, string angle,
        string contentFormat, string targetAudience, string viewerIntent, string hookConcept, string thumbnailConcept,
        string viewerPromise, string coreQuestion, string whyViewerWouldCare, string hypothesis, int opportunityFit,
        int observedDemandAlignment, int novelty, int titlePotential, int thumbnailPotential, int storyPotential,
        int audienceFit, int evidenceStrength, int productionEase, int competitionRisk, int researchRisk,
        int confidence, decimal overallScore, decimal duplicationPenalty, string scoringAlgorithmVersion, string risksJson,
        DateTimeOffset createdAt)
    {
        foreach (var score in new[] { opportunityFit, observedDemandAlignment, novelty, titlePotential, thumbnailPotential,
            storyPotential, audienceFit, evidenceStrength, productionEase, competitionRisk, researchRisk, confidence })
            if (score is < 0 or > 100) throw new DomainException("Idea score components must be between 0 and 100.");
        if (overallScore is < 0 or > 100 || duplicationPenalty is < 0 or > 100)
            throw new DomainException("Idea score and duplication penalty must be between 0 and 100.");
        Id = Guid.NewGuid(); ProjectId = Guard.NotEmpty(projectId, nameof(projectId)); OpportunityId = Guard.NotEmpty(opportunityId, nameof(opportunityId)); GenerationId = Guard.NotEmpty(generationId, nameof(generationId));
        WorkingTitle = Guard.Required(workingTitle, nameof(workingTitle), 300); Topic = Guard.Required(topic, nameof(topic), 500); Angle = Guard.Required(angle, nameof(angle), 1000); ContentFormat = Guard.Required(contentFormat, nameof(contentFormat), 500);
        TargetAudience = Guard.Required(targetAudience, nameof(targetAudience), 1000); ViewerIntent = Guard.Required(viewerIntent, nameof(viewerIntent), 1000); HookConcept = Guard.Required(hookConcept, nameof(hookConcept), 2000); ThumbnailConcept = Guard.Required(thumbnailConcept, nameof(thumbnailConcept), 2000);
        ViewerPromise = Guard.Required(viewerPromise, nameof(viewerPromise), 2000); CoreQuestion = Guard.Required(coreQuestion, nameof(coreQuestion), 2000); WhyViewerWouldCare = Guard.Required(whyViewerWouldCare, nameof(whyViewerWouldCare), 4000); Hypothesis = Guard.Required(hypothesis, nameof(hypothesis), 4000);
        OpportunityFit = opportunityFit; ObservedDemandAlignment = observedDemandAlignment; Novelty = novelty; TitlePotential = titlePotential; ThumbnailPotential = thumbnailPotential; StoryPotential = storyPotential; AudienceFit = audienceFit; EvidenceStrength = evidenceStrength; ProductionEase = productionEase; CompetitionRisk = competitionRisk; ResearchRisk = researchRisk; Confidence = confidence; OverallScore = overallScore; DuplicationPenalty = duplicationPenalty;
        ScoringAlgorithmVersion = Guard.Required(scoringAlgorithmVersion, nameof(scoringAlgorithmVersion), 100); RisksJson = Guard.Required(risksJson, nameof(risksJson), 10_000); DecisionStatus = IdeaDecisionStatus.Candidate; CreatedAt = createdAt;
    }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid OpportunityId { get; private set; }
    public Guid GenerationId { get; private set; }
    public string WorkingTitle { get; private set; } = string.Empty; public string Topic { get; private set; } = string.Empty; public string Angle { get; private set; } = string.Empty; public string ContentFormat { get; private set; } = string.Empty; public string TargetAudience { get; private set; } = string.Empty; public string ViewerIntent { get; private set; } = string.Empty; public string HookConcept { get; private set; } = string.Empty; public string ThumbnailConcept { get; private set; } = string.Empty; public string ViewerPromise { get; private set; } = string.Empty; public string CoreQuestion { get; private set; } = string.Empty; public string WhyViewerWouldCare { get; private set; } = string.Empty; public string Hypothesis { get; private set; } = string.Empty;
    public int OpportunityFit { get; private set; }
    public int ObservedDemandAlignment { get; private set; }
    public int Novelty { get; private set; }
    public int TitlePotential { get; private set; }
    public int ThumbnailPotential { get; private set; }
    public int StoryPotential { get; private set; }
    public int AudienceFit { get; private set; }
    public int EvidenceStrength { get; private set; }
    public int ProductionEase { get; private set; }
    public int CompetitionRisk { get; private set; }
    public int ResearchRisk { get; private set; }
    public int Confidence { get; private set; }
    public decimal OverallScore { get; private set; }
    public decimal DuplicationPenalty { get; private set; }
    public string ScoringAlgorithmVersion { get; private set; } = string.Empty; public string RisksJson { get; private set; } = "[]"; public IdeaDecisionStatus DecisionStatus { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public void SetDecision(IdeaDecisionStatus decision) => DecisionStatus = decision;
}
