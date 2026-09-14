using System.Text.Json;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Competitors;

namespace YoutubeAiFactory.Application.Localization;

public static class CompetitorAnalysisLocalizationPrompt
{
    public const string Key = "competitor-analysis-localization";
    public const int Version = 2;
    public static AiModelProfile ModelProfile => AiWorkflowProfiles.ArtifactLocalization;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    public static LlmRequest Create(CompetitorAnalysisResult canonical, bool correcting) => new(Key, Version,
        "Translate only reader-facing analysis explanations into Vietnamese. Return one complete JSON object matching LocalizedCompetitorAnalysisContent. Never use null for audience, confidence, any list, or any list item. Preserve every list's order and exact count; use [] only when the canonical list is empty. Required shape: audience { likelyAgeRange, likelyInterests, likelyViewerIntent, geographyHints, evidence }; topicClusters[] { name, description, performanceSignal }; titlePatterns[] { patternName, description, performanceSignal }; thumbnailPatterns[] and hookPatterns[] { patternName, observation, limitations }; contentFormats[] { format, performanceSignal }; performanceInsights[] { insight }; potentialWeaknesses[] { observation }; transferableFormats[] { format, whyItMayWork, transferableMechanic, doNotCopy }; evidenceNotes[] { note }; confidence { dataQuality, limitations }. Do not include IDs, scores, metrics, URLs, timestamps, versions, model details, evidence references, example titles, or title templates. Do not rewrite, improve, or add analysis. The UI remains English; this is a Vietnamese reading aid only.",
        $"Canonical analysis JSON:\n{JsonSerializer.Serialize(canonical, SerializerOptions)}" + (correcting ? "\nYour prior response failed structural validation. Preserve every list count exactly." : string.Empty),
        new Dictionary<string, string> { ["max_output_tokens"] = "5000" },
        CompetitorAnalysisLocalizationOutputSchema.Create(),
        ModelProfile);
}
