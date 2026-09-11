using System.Text.Json;
using YoutubeAiFactory.Application.AI;

namespace YoutubeAiFactory.Application.Pilots;

public static class PilotGenerationPrompt
{
    public const string Key = "pilot-generation";
    public const int Version = 1;
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    public static LlmRequest Create(PilotGenerationContext context, bool correction = false) => new(
        Key, Version,
        "You are planning a deliberate twelve-video YouTube validation pilot. Use only supplied idea IDs. Select each idea once. Slots 1-4 must be Topic, 5-8 Packaging, 9-12 Storytelling. Every slot needs a testable hypothesis (variable, expected effect, reason), a control strategy, future metric, success signal, and rationale. Maximize learning through coherent variation, not the top twelve scores. Do not create ideas, scripts, thumbnails, analytics, or unsupported claims." + (correction ? " Correct the prior response to satisfy every stated invariant." : ""),
        JsonSerializer.Serialize(context, SerializerOptions));
}
