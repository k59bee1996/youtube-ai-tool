namespace YoutubeAiFactory.Infrastructure.AI;

public sealed class AiOptions
{
    public const string SectionName = "AI";
    public string ApiKey { get; init; } = string.Empty;
    public Dictionary<string, AiModelProfileOptions> Models { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AiModelProfileOptions
{
    public string Provider { get; init; } = "OpenAI";
    public string Model { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 60;
    public int MaxOutputTokens { get; init; } = 5_000;
}
