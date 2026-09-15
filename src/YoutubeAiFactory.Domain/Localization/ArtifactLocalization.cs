using YoutubeAiFactory.Domain.Common;

namespace YoutubeAiFactory.Domain.Localization;

/// <summary>A read-only localized representation of a versioned artifact. The canonical artifact is never changed.</summary>
public sealed class ArtifactLocalization
{
    private ArtifactLocalization() { }

    public ArtifactLocalization(string artifactType, Guid artifactId, int artifactVersion, string locale, string contentJson,
        Guid aiRunId, string promptKey, int promptVersion, string provider, string model, DateTimeOffset createdAt)
    {
        if (artifactVersion < 1) throw new DomainException("Artifact version must be at least 1.");
        if (promptVersion < 1) throw new DomainException("Prompt version must be at least 1.");
        Id = Guid.NewGuid();
        ArtifactType = Guard.Required(artifactType, nameof(artifactType), 100);
        ArtifactId = Guard.NotEmpty(artifactId, nameof(artifactId));
        ArtifactVersion = artifactVersion;
        Locale = Guard.Required(locale, nameof(locale), 10).ToLowerInvariant();
        ContentJson = Guard.Required(contentJson, nameof(contentJson), 200_000);
        AiRunId = Guard.NotEmpty(aiRunId, nameof(aiRunId));
        PromptKey = Guard.Required(promptKey, nameof(promptKey), 100);
        PromptVersion = promptVersion;
        Provider = Guard.Required(provider, nameof(provider), 100);
        Model = Guard.Required(model, nameof(model), 100);
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string ArtifactType { get; private set; } = string.Empty;
    public Guid ArtifactId { get; private set; }
    public int ArtifactVersion { get; private set; }
    public string Locale { get; private set; } = string.Empty;
    public string ContentJson { get; private set; } = string.Empty;
    public Guid AiRunId { get; private set; }
    public string PromptKey { get; private set; } = string.Empty;
    public int PromptVersion { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}

public static class LocalizableArtifactTypes
{
    public const string CompetitorAnalysis = "competitor-analysis";
    public const string OpportunityReport = "opportunity-report";
    public const string ResearchReport = "research-report";
}
