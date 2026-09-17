using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using YoutubeAiFactory.Application.AI;

namespace YoutubeAiFactory.Application.Outlines;

public static class OutlinePrompt
{
    public const string Key = "outline-generation";
    public const int Version = 1;
    public const string RepairKey = "structured-output-repair";
    public const int RepairVersion = 1;
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static LlmRequest Create(OutlineGenerationContext context, string? correctionDiagnostic = null,
        OutlineGenerationResult? previousCandidate = null) => new(Key, Version,
        """
        Design an evidence-grounded video OUTLINE, never a script. Use only the supplied ResearchReport claims, evidence, conflicts, and gaps. Do not use model memory, perform research, invent facts, sources, claims, IDs, quotes, examples, stakes, or certainty. Preserve Supported, Corroborated, Conflicted, and Unsupported semantics; Unsupported claims cannot be factual support, and conflicted claims must be framed as disputed with the supplied conflict ID. Reference only supplied claim, conflict, and research-gap identities.

        Create canonical planning text in English. Treat the supplied TargetLanguage as content-planning context, not as permission to translate the canonical artifact. Create a planning-level narrative strategy and concise ordered sections: purpose, objective, summary, viewer question, transition intent, and claim placement. Include exactly one final CTA planning section and at least one mid-outline PatternInterrupt section; these must describe intent only, never final spoken copy. For each claim, supplied evidence links include a Support or Contradict stance. Do not present Contradict evidence as support. Do not write voice-over paragraphs, final narration, final thumbnail copy, scenes, or a script. Keep the existing working title and viewer promise unchanged. Respect the Pilot experiment, variable, and control strategy. For a Storytelling experiment, genuinely implement the requested structure in the ordered sections. For a Packaging experiment, preserve the packaging premise and surface evidence limitations. For a Topic experiment, avoid a storytelling confound when the control strategy asks for comparable structure. Use curiosity only to order supported information. Return structured JSON only.
        """, JsonSerializer.Serialize(new { context, correctionDiagnostic, previousCandidate }, SerializerOptions),
        new Dictionary<string, string> { ["max_output_tokens"] = "7000" }, Schema(), AiWorkflowProfiles.OutlineGeneration);

    public static LlmRequest CreateRepair(string malformedOutput, string diagnostic) => new(RepairKey, RepairVersion,
        """
        Repair malformed JSON into the supplied outline schema without changing business meaning. Do not add, remove, reinterpret, or improve facts, claim IDs, conflict IDs, gap indexes, narrative choices, or experiment choices. This is mechanical structural repair only. Return JSON only.
        """, JsonSerializer.Serialize(new { malformedOutput, diagnostic }, SerializerOptions),
        new Dictionary<string, string> { ["max_output_tokens"] = "7000" }, Schema(), AiWorkflowProfiles.StructuredOutputRepair);

    public static JsonNode Schema() => JsonNode.Parse("""
    {
      "type":"object","additionalProperties":false,
      "properties":{
        "narrativeStrategy":{"type":"object","additionalProperties":false,"properties":{
          "structureType":{"type":"string","enum":["Explainer","Chronological","MysteryReveal","RiseAndFall","ProblemExplanation","CauseConsequence","Comparison","CaseStudy","Escalation","Transformation"]},
          "coreQuestion":{"type":"string"},"coreTension":{"type":"string"},"openingHookConcept":{"type":"string"},
          "narrativeProgression":{"type":"string"},"payoff":{"type":"string"},"pacingStrategy":{"type":"string"}
        },"required":["structureType","coreQuestion","coreTension","openingHookConcept","narrativeProgression","payoff","pacingStrategy"]},
        "experimentAlignment":{"type":"object","additionalProperties":false,"properties":{
          "howOutlineImplementsExperiment":{"type":"string"},
          "risksToExperimentIntegrity":{"type":"array","items":{"type":"string"}}
        },"required":["howOutlineImplementsExperiment","risksToExperimentIntegrity"]},
        "sections":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{
          "sequence":{"type":"integer","minimum":1},"heading":{"type":"string"},
          "purpose":{"type":"string","enum":["Hook","Setup","Context","Explanation","Escalation","Example","PatternInterrupt","Counterpoint","Conflict","Payoff","Conclusion","CTA"]},
          "objective":{"type":"string"},"summary":{"type":"string"},"viewerQuestion":{"type":["string","null"]},
          "transitionIntent":{"type":["string","null"]},
          "claimReferences":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{
            "claimId":{"type":"string","format":"uuid"},"usageRole":{"type":"string","enum":["Core","Supporting","Example","Counterpoint","Conflict"]}
          },"required":["claimId","usageRole"]}},
          "conflictIds":{"type":"array","items":{"type":"string","format":"uuid"}},
          "researchGapIndexes":{"type":"array","items":{"type":"integer","minimum":0}},
          "estimatedSeconds":{"type":["integer","null"],"minimum":15,"maximum":1800}
        },"required":["sequence","heading","purpose","objective","summary","viewerQuestion","transitionIntent","claimReferences","conflictIds","researchGapIndexes","estimatedSeconds"]}}
      },"required":["narrativeStrategy","experimentAlignment","sections"]
    }
    """)!.DeepClone();
}
