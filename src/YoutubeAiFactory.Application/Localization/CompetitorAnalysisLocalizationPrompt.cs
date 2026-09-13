using System.Text.Json;
using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Competitors;

namespace YoutubeAiFactory.Application.Localization;

public static class CompetitorAnalysisLocalizationPrompt
{
    public const string Key = "competitor-analysis-localization";
    public const int Version = 1;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    public static LlmRequest Create(CompetitorAnalysisResult canonical, bool correcting) => new(Key, Version,
        "Translate only reader-facing analysis explanations into Vietnamese. Return JSON matching LocalizedCompetitorAnalysisContent exactly. Preserve list order and count. Do not include IDs, scores, metrics, URLs, timestamps, versions, model details, evidence references, example titles, or title templates. Do not rewrite, improve, or add analysis. The UI remains English; this is a Vietnamese reading aid only.",
        $"Canonical analysis JSON:\n{JsonSerializer.Serialize(canonical, SerializerOptions)}" + (correcting ? "\nYour prior response failed structural validation. Preserve every list count exactly." : string.Empty),
        new Dictionary<string, string> { ["max_output_tokens"] = "5000" });
}
