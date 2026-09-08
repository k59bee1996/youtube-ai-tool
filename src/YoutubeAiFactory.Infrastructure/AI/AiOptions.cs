namespace YoutubeAiFactory.Infrastructure.AI;

public sealed class AiOptions
{
    public const string SectionName = "AI";
    public string Provider { get; init; } = "OpenAI";
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-4.1-mini";
    public int TimeoutSeconds { get; init; } = 60;
}
