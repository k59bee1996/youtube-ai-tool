using System.Text.Json;
using YoutubeAiFactory.Application.AI;

namespace YoutubeAiFactory.Application.Ideas;

public static class IdeaGenerationPrompt
{
    public const string Key = "idea-generation"; public const int Version = 1;
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    public static LlmRequest Create(IdeaGenerationContext context, int count, bool repair = false) => new(Key, Version,
        """You are a YouTube idea strategist. Return JSON only. Create original, concrete video hypotheses inside the supplied approved opportunity. Use competitor data only as evidence of mechanics, never as text to copy: do not reproduce competitor titles, scripts, or near-duplicate noun swaps. Create meaningful variation in subject, angle, tension, scale, comparison, time, case study, failure, mechanism, or what-if. Do not make global demand claims, scripts, thumbnail assets, pilot sequences, or unsupported facts. Every candidate must cite only supplied evidence IDs, include risks, a click hypothesis, hook and thumbnail concepts, and bounded 0-100 subjective features. Required JSON: ideas. Each idea requires workingTitle, topic, angle, contentFormat, targetAudience, viewerIntent, hookConcept, thumbnailConcept, viewerPromise, coreQuestion, whyViewerWouldCare, hypothesis, features { novelty,titlePotential,thumbnailPotential,storyPotential,audienceFit,productionComplexity,competitionRisk,researchRisk }, evidenceIds, risks, confidence.""",
        (repair ? "Repair the prior invalid response. " : string.Empty) + $"Return exactly {count} candidates.\n" + JsonSerializer.Serialize(context, SerializerOptions), new Dictionary<string, string> { ["max_output_tokens"] = "12000" });
}
