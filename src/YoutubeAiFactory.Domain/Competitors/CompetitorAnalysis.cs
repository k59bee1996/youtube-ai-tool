using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Competitors;

/// <summary>One immutable, completed structured intelligence report for a competitor.</summary>
public sealed class CompetitorAnalysis
{
    private CompetitorAnalysis()
    {
    }

    public CompetitorAnalysis(
        Guid competitorChannelId,
        int version,
        Guid aiRunId,
        string promptKey,
        int promptVersion,
        string provider,
        string model,
        DateTimeOffset sourceDataAsOf,
        int analyzedVideoCount,
        string resultJson,
        DateTimeOffset createdAt)
    {
        if (version < 1) throw new DomainException("Analysis version must be at least 1.");
        if (promptVersion < 1) throw new DomainException("Prompt version must be at least 1.");
        if (analyzedVideoCount < 1) throw new DomainException("An analysis must use at least one video.");
        Id = Guid.NewGuid();
        CompetitorChannelId = Guard.NotEmpty(competitorChannelId, nameof(competitorChannelId));
        Version = version;
        AiRunId = Guard.NotEmpty(aiRunId, nameof(aiRunId));
        PromptKey = Guard.Required(promptKey, nameof(promptKey), 100);
        PromptVersion = promptVersion;
        Provider = Guard.Required(provider, nameof(provider), 100);
        Model = Guard.Required(model, nameof(model), 100);
        SourceDataAsOf = sourceDataAsOf;
        AnalyzedVideoCount = analyzedVideoCount;
        ResultJson = Guard.Required(resultJson, nameof(resultJson), 200_000);
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid CompetitorChannelId { get; private set; }
    public int Version { get; private set; }
    public Guid AiRunId { get; private set; }
    public string PromptKey { get; private set; } = string.Empty;
    public int PromptVersion { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public DateTimeOffset SourceDataAsOf { get; private set; }
    public int AnalyzedVideoCount { get; private set; }
    public string ResultJson { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
