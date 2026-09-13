using System.Text.Json;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Competitors;

namespace YoutubeAiFactory.Application.Opportunities;

public static class OpportunityAnalysisPrompt
{
    public const string Key = "opportunity-analysis";
    public const int Version = 2;
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    public static LlmRequest Create(OpportunityAnalysisContext context, string? repairDiagnostic = null) => new(Key, Version,
        """
        You are a YouTube opportunity analyst. Return JSON only matching the requested contract. Use only supplied evidence IDs and context.
        Identify coherent adjacent opportunities, not cloned competitor content. Treat demand and competition as observations of this supplied dataset, never as global YouTube facts. Include meaningful risks and limitations. Do not invent sources, metrics, channels, videos, or evidence IDs.
        Required top-level JSON: opportunities, limitations. Each opportunity needs name, description, audience, topic, contentFormat, angle, whyThisOpportunity, noveltySignal, audienceFitSignal, transferabilitySignal, storyPotential, productionComplexity, confidence, evidenceIds, risks, limitations. All signals are integers 0-100. Avoid semantically duplicate audience/topic/format/angle combinations.
        Every list field, including evidenceIds, risks, and limitations, must be a JSON array of strings even when it has zero or one item. Never return a scalar string for a list field.
        """, (repairDiagnostic is null ? string.Empty : $"The previous output was invalid: {repairDiagnostic} Repair it to exactly follow every JSON type in the contract.\n\n") + JsonSerializer.Serialize(context, SerializerOptions),
        new Dictionary<string, string> { ["max_output_tokens"] = "5000" },
        OpportunityAnalysisOutputSchema.Create());
}
