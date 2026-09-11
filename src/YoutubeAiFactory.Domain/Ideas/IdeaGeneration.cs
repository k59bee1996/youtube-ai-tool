using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Ideas;

/// <summary>Immutable metadata for one bounded idea-generation batch.</summary>
public sealed class IdeaGeneration
{
    private IdeaGeneration() { }

    public IdeaGeneration(Guid projectId, Guid opportunityId, Guid opportunityReportId, int opportunityReportVersion,
        int version, Guid aiRunId, string promptKey, int promptVersion, string provider, string model,
        string scoringAlgorithmVersion, DateTimeOffset createdAt)
    {
        if (version < 1 || opportunityReportVersion < 1 || promptVersion < 1)
            throw new DomainException("Idea generation versions must be positive.");
        Id = Guid.NewGuid();
        ProjectId = Guard.NotEmpty(projectId, nameof(projectId));
        OpportunityId = Guard.NotEmpty(opportunityId, nameof(opportunityId));
        OpportunityReportId = Guard.NotEmpty(opportunityReportId, nameof(opportunityReportId));
        OpportunityReportVersion = opportunityReportVersion;
        Version = version; AiRunId = Guard.NotEmpty(aiRunId, nameof(aiRunId));
        PromptKey = Guard.Required(promptKey, nameof(promptKey), 100); PromptVersion = promptVersion;
        Provider = Guard.Required(provider, nameof(provider), 100); Model = Guard.Required(model, nameof(model), 100);
        ScoringAlgorithmVersion = Guard.Required(scoringAlgorithmVersion, nameof(scoringAlgorithmVersion), 100);
        CreatedAt = createdAt;
    }
    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid OpportunityId { get; private set; }
    public Guid OpportunityReportId { get; private set; }
    public int OpportunityReportVersion { get; private set; }
    public int Version { get; private set; }
    public Guid AiRunId { get; private set; }
    public string PromptKey { get; private set; } = string.Empty;
    public int PromptVersion { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string ScoringAlgorithmVersion { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
