using System.Text.Json;
using YoutubeAiFactory.Application.AI;

namespace YoutubeAiFactory.Application.Pilots;

public static class PilotGenerationPrompt
{
    public const string Key = "pilot-generation";
    public const int Version = 1;
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    public static LlmRequest Create(PilotGenerationContext context, PilotOutputCorrection? correction = null)
    {
        var userContent = correction is null
            ? JsonSerializer.Serialize(context, SerializerOptions)
            : JsonSerializer.Serialize(new { Context = context, Correction = correction }, SerializerOptions);
        return new(
            Key, Version,
            "You are planning a deliberate twelve-video YouTube validation pilot. Use only supplied idea IDs. Select each idea once. Slots 1-4 must be Topic, 5-8 Packaging, 9-12 Storytelling. Every slot needs a testable hypothesis (variable, expected effect, reason), a control strategy, future metric, success signal, and rationale. Return name, objective, videos, experimentSummary, assumptions, and limitations. Maximize learning through coherent variation, not the top twelve scores. Do not create ideas, scripts, thumbnails, analytics, or unsupported claims." + (correction is null ? string.Empty : " The previous candidate and the validator failure are included in the input. Return a complete corrected replacement."),
            userContent);
    }
}

public sealed record PilotOutputCorrection(string ValidationFailure, string? PreviousOutput);
