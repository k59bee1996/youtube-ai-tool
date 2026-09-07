namespace YoutubeAiFactory.Application.AI;

public sealed record LlmResult<T>(
    T Value,
    string Provider,
    string Model,
    int? InputTokens,
    int? OutputTokens,
    string? RawOutput);
