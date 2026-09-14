namespace YoutubeAiFactory.Application.AI;

/// <summary>Resolves a logical intelligence class to the configured provider execution settings.</summary>
public interface IAiModelResolver
{
    ResolvedAiModel Resolve(AiModelProfile profile);
}

public sealed record ResolvedAiModel(
    AiModelProfile Profile,
    string Provider,
    string Model,
    int TimeoutSeconds,
    int MaxOutputTokens);
