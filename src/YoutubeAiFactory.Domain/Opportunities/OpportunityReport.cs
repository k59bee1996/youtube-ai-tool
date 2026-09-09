using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Opportunities;

/// <summary>Immutable, evidence-scoped result of one opportunity-generation run.</summary>
public sealed class OpportunityReport
{
    private OpportunityReport() { }

    public OpportunityReport(Guid projectId, int version, Guid aiRunId, string promptKey, int promptVersion,
        string provider, string model, string scoringAlgorithmVersion, int sourceAnalysisCount, string limitationsJson, DateTimeOffset createdAt)
    {
        if (version < 1 || promptVersion < 1 || sourceAnalysisCount < 1)
            throw new DomainException("Opportunity report versions and source count must be positive.");
        Id = Guid.NewGuid();
        ProjectId = Guard.NotEmpty(projectId, nameof(projectId));
        Version = version;
        AiRunId = Guard.NotEmpty(aiRunId, nameof(aiRunId));
        PromptKey = Guard.Required(promptKey, nameof(promptKey), 100);
        PromptVersion = promptVersion;
        Provider = Guard.Required(provider, nameof(provider), 100);
        Model = Guard.Required(model, nameof(model), 100);
        ScoringAlgorithmVersion = Guard.Required(scoringAlgorithmVersion, nameof(scoringAlgorithmVersion), 100);
        SourceAnalysisCount = sourceAnalysisCount;
        LimitationsJson = Guard.Required(limitationsJson, nameof(limitationsJson), 10_000);
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public int Version { get; private set; }
    public Guid AiRunId { get; private set; }
    public string PromptKey { get; private set; } = string.Empty;
    public int PromptVersion { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string ScoringAlgorithmVersion { get; private set; } = string.Empty;
    public int SourceAnalysisCount { get; private set; }
    public string LimitationsJson { get; private set; } = "[]";
    public DateTimeOffset CreatedAt { get; private set; }
}
