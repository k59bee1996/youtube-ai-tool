using System.Text.Json;
using YoutubeAiFactory.Application.AI;

namespace YoutubeAiFactory.Application.Pilots;

public static class PilotGenerationPrompt
{
    public const string Key = "pilot-generation";
    public const int Version = 3;
    public static AiModelProfile ModelProfile => AiWorkflowProfiles.PilotGeneration;
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    public static LlmRequest Create(PilotGenerationContext context, PilotOutputCorrection? correction = null)
    {
        var userContent = correction is null
            ? JsonSerializer.Serialize(context, SerializerOptions)
            : JsonSerializer.Serialize(new { Context = context, Correction = correction }, SerializerOptions);
        return new(
            Key, Version,
            "You are planning a deliberate twelve-video YouTube validation pilot. Return JSON only, matching the supplied JSON Schema. Use only supplied idea IDs. Select each idea once. Slots 1-4 must be Topic, 5-8 Packaging, 9-12 Storytelling. experimentType is an integer: Topic=0, Packaging=1, Storytelling=2. Every slot needs a testable hypothesis, a control strategy, future metric, success signal, and rationale. hypothesis is one plain string that combines the variable, expected effect, and reason; never return an object for hypothesis or any other text field. Return name, objective, videos, experimentSummary, assumptions, and limitations. Maximize learning through coherent variation, not the top twelve scores. Do not create ideas, scripts, thumbnails, analytics, or unsupported claims." + (correction is null ? string.Empty : " The previous candidate and the validator failure are included in the input. Return a complete corrected JSON replacement."),
            userContent,
            outputSchema: PilotGenerationOutputSchema.Create(),
            modelProfile: ModelProfile);
    }
}

public sealed record PilotOutputCorrection(string ValidationFailure, string? PreviousOutput);
