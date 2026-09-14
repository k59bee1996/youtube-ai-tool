using System.Text.Json;
using YoutubeAiFactory.Application.AI;

namespace YoutubeAiFactory.Application.Competitors;

public static class CompetitorAnalysisPrompt
{
    public const string Key = "competitor-analysis";
    public const int Version = 2;
    public static AiModelProfile ModelProfile => AiWorkflowProfiles.CompetitorAnalysis;

    public static LlmRequest Create(CompetitorAnalysisContext context, string? repairDiagnostic = null) => new(
        Key,
        Version,
        """
        You are a YouTube competitor-intelligence analyst. Return JSON only, matching the requested structured contract.
        Use only the supplied channel and video metadata. Separate observations from inferences and make confidence proportional to evidence.
        Never claim transcript, comment, audience demographic, thumbnail-image, hook-wording, or storytelling analysis unless it is explicitly in the input; this input does not contain transcripts, comments, or thumbnail pixels.
        Do not recommend cloning content. Extract transferable mechanics and state what must not be copied. Video identifiers in evidence fields must be supplied IDs only. Do not invent metrics or references.
        Required top-level JSON: audience, topicClusters, titlePatterns, thumbnailPatterns, hookPatterns, contentFormats, performanceInsights, potentialWeaknesses, transferableFormats, evidenceNotes, confidence.
        Since thumbnail pixels and transcripts are unavailable, thumbnailPatterns and hookPatterns must explicitly say evidence is insufficient rather than infer visual or spoken details.
        Confidence fields are integer percentages from 0 through 100. Arrays may be empty where evidence is insufficient.
        Every list field must be a JSON array, even when it has one item. Every evidence ID must be a UUID string from the supplied video IDs.
        Exact item contracts: topicClusters={name:string,description:string,exampleVideoIds:uuid[],frequency:integer,performanceSignal:string,confidence:integer}; titlePatterns={patternName:string,description:string,template:string,exampleTitles:string[],observedFrequency:integer,performanceSignal:string,confidence:integer}; thumbnailPatterns and hookPatterns={patternName:string,observation:string,evidenceVideoIds:uuid[],confidence:integer,limitations:string[]}; contentFormats={format:string,evidenceVideoIds:uuid[],performanceSignal:string,confidence:integer}; performanceInsights={insight:string,supportingVideoIds:uuid[],confidence:integer}; potentialWeaknesses={observation:string,supportingVideoIds:uuid[],confidence:integer}; transferableFormats={format:string,whyItMayWork:string,evidenceVideoIds:uuid[],transferableMechanic:string,doNotCopy:string,confidence:integer}; evidenceNotes={note:string,videoIds:uuid[]}. audience={likelyAgeRange:string|null,likelyInterests:string[],likelyViewerIntent:string[],geographyHints:string[],confidence:integer,evidence:string[]}; confidence={overallConfidence:integer,dataQuality:string,limitations:string[]}.
        """,
        (repairDiagnostic is null ? string.Empty : $"The previous output was invalid: {repairDiagnostic} Repair it to exactly follow every JSON type in the contract.\n\n") +
        JsonSerializer.Serialize(context, SerializerOptions),
        new Dictionary<string, string> { ["max_output_tokens"] = "5000" },
        CompetitorAnalysisOutputSchema.Create(),
        ModelProfile);

    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
}
