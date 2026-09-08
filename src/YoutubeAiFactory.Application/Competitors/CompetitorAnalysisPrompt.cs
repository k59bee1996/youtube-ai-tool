using System.Text.Json;
using YoutubeAiFactory.Application.AI;

namespace YoutubeAiFactory.Application.Competitors;

public static class CompetitorAnalysisPrompt
{
    public const string Key = "competitor-analysis";
    public const int Version = 1;

    public static LlmRequest Create(CompetitorAnalysisContext context, bool repair = false) => new(
        Key,
        Version,
        """
        You are a YouTube competitor-intelligence analyst. Return JSON only, matching the requested structured contract.
        Use only the supplied channel and video metadata. Separate observations from inferences and make confidence proportional to evidence.
        Never claim transcript, comment, audience demographic, thumbnail-image, hook-wording, or storytelling analysis unless it is explicitly in the input; this input does not contain transcripts, comments, or thumbnail pixels.
        Do not recommend cloning content. Extract transferable mechanics and state what must not be copied. Video identifiers in evidence fields must be supplied IDs only. Do not invent metrics or references.
        Required top-level JSON: audience, topicClusters, titlePatterns, contentFormats, performanceInsights, potentialWeaknesses, transferableFormats, evidenceNotes, confidence.
        Confidence fields are integer percentages from 0 through 100. Arrays may be empty where evidence is insufficient.
        """,
        (repair ? "The previous output was invalid. Repair it to exactly follow the required JSON contract.\n\n" : string.Empty) +
        JsonSerializer.Serialize(context, SerializerOptions),
        new Dictionary<string, string> { ["max_output_tokens"] = "5000" });

    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
}
