using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using YoutubeAiFactory.Application.AI;

namespace YoutubeAiFactory.Application.Scripts;

public static class ScriptPrompt
{
    public const string GenerationKey = "script-generation";
    public const int GenerationVersion = 1;
    public const string AuditKey = "script-grounding-audit";
    public const int AuditVersion = 1;
    public const string CorrectionKey = "script-grounding-correction";
    public const int CorrectionVersion = 1;
    public const string RepairKey = "structured-script-repair";
    public const int RepairVersion = 1;

    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static LlmRequest CreateGeneration(ScriptGenerationContext context, string? correctionDiagnostic = null,
        VideoScriptResult? previousCandidate = null) => new(GenerationKey, GenerationVersion,
        """
        Write the complete viewer-facing narration for the supplied APPROVED outline. Follow every outline section in exact order and preserve its narrative purpose, structure, hook concept, pacing, transition intent, payoff, ViewerPromise, audience, and Pilot experiment. Write in ContentLanguage; Analysis reading locale is irrelevant. The WorkingTitle is context and must not be changed.

        Every factual assertion must use only a ResearchClaim supplied inside that same outline section. Do not use model memory, research, fetch sources, create claims, invent IDs, numbers, dates, names, quotes, causes, examples, certainty, or modern conversions. Preserve Supported versus Corroborated strength, uncertainty, approximate dates, units, and conflict semantics. A conflicted claim must remain explicitly uncertain and cite its supplied Conflict ID internally. Exact quotes require supplied claim/evidence support. Paraphrase source material in original wording and never copy long excerpts.

        Return structured sections and narration blocks only. Each section must use its exact OutlineSectionId and sequence. FactualNarration, ConflictExplanation, and Quote blocks require valid Claim IDs; non-factual bridges, rhetorical questions, and payoff phrasing may have none. Keep block sequence contiguous. Claim and Conflict IDs are internal metadata and must not appear as spoken citations. Produce narration prose only: no scenes, B-roll, camera directions, thumbnails, image prompts, TTS, SSML, production notes, or spoken section headings. Avoid generic filler and false curiosity. The closing payoff must answer the supplied core question and ViewerPromise only as far as the research permits. Do not add a generic like/subscribe CTA unless the supplied outline explicitly provides configured spoken CTA content (it does not in V1). Return JSON only.
        """, JsonSerializer.Serialize(new { context, correctionDiagnostic, previousCandidate }, SerializerOptions),
        new Dictionary<string, string> { ["max_output_tokens"] = "12000" }, GenerationSchema(),
        AiWorkflowProfiles.ScriptGeneration);

    public static LlmRequest CreateAudit(ScriptGenerationContext context, VideoScriptResult script) => new(
        AuditKey, AuditVersion,
        """
        Audit only the factual grounding of the supplied Script against the supplied ResearchClaims, evidence stances, and conflicts. Do not rewrite style, perform research, use model memory, create facts, or invent corrections. Flag unsupported factual assertions, specificity, numbers, dates, names, examples, exact quotes, causal overstatement, strengthened uncertainty, claim mismatch, or suppressed conflicts. A claim reference does not automatically prove that every sentence in a block is supported. Return Passed only when no Error issue exists. Return structured issues only.
        """, JsonSerializer.Serialize(new { context, script }, SerializerOptions),
        new Dictionary<string, string> { ["max_output_tokens"] = "5000" }, AuditSchema(),
        AiWorkflowProfiles.ScriptGroundingAudit);

    public static LlmRequest CreateCorrection(ScriptGenerationContext context, VideoScriptResult script,
        IReadOnlyList<ScriptGroundingIssueResult> groundingIssues, string? lengthDiagnostic) => new(
        CorrectionKey, CorrectionVersion,
        """
        Correct only the identified grounding or length problems in the supplied Script. Preserve the approved outline section IDs, order, narrative structure, tone, ViewerPromise, Pilot experiment, and valid Claim/Conflict references. Use only supplied Claims. Do not add facts, sources, IDs, scenes, production directions, TTS, or SSML. For length correction, adjust explanation density and transitions without truncating facts or inventing filler. Return the complete corrected structured Script as JSON.
        """, JsonSerializer.Serialize(new { context, script, groundingIssues, lengthDiagnostic }, SerializerOptions),
        new Dictionary<string, string> { ["max_output_tokens"] = "12000" }, GenerationSchema(),
        AiWorkflowProfiles.ScriptGroundingCorrection);

    public static LlmRequest CreateRepair(string malformedOutput, string diagnostic) => new(RepairKey,
        RepairVersion,
        """
        Repair malformed JSON into the supplied Script schema without changing or inventing narration, facts, section IDs, Claim IDs, Conflict IDs, sequences, or block types. This is mechanical structural repair only. If narration is missing, do not create it. Return JSON only.
        """, JsonSerializer.Serialize(new { malformedOutput, diagnostic }, SerializerOptions),
        new Dictionary<string, string> { ["max_output_tokens"] = "12000" }, GenerationSchema(),
        AiWorkflowProfiles.StructuredOutputRepair);

    public static JsonNode GenerationSchema() => JsonNode.Parse("""
    {
      "type":"object","additionalProperties":false,
      "properties":{
        "sections":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{
          "outlineSectionId":{"type":"string","format":"uuid"},"sequence":{"type":"integer","minimum":1},
          "blocks":{"type":"array","minItems":1,"items":{"type":"object","additionalProperties":false,"properties":{
            "sequence":{"type":"integer","minimum":1},
            "type":{"type":"string","enum":["FactualNarration","NarrativeBridge","RhetoricalQuestion","ConflictExplanation","Quote","Payoff"]},
            "text":{"type":"string"},"claimIds":{"type":"array","items":{"type":"string","format":"uuid"}},
            "conflictIds":{"type":"array","items":{"type":"string","format":"uuid"}}
          },"required":["sequence","type","text","claimIds","conflictIds"]}}
        },"required":["outlineSectionId","sequence","blocks"]}},
        "closingPayoff":{"type":"string"}
      },"required":["sections","closingPayoff"]
    }
    """)!.DeepClone();

    public static JsonNode AuditSchema() => JsonNode.Parse("""
    {
      "type":"object","additionalProperties":false,
      "properties":{
        "status":{"type":"string","enum":["Passed","Failed"]},
        "issues":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{
          "sectionSequence":{"type":"integer","minimum":1},"blockSequence":{"type":"integer","minimum":1},
          "issueType":{"type":"string","enum":["UnsupportedFact","UnsupportedNumber","UnsupportedDate","FabricatedQuote","ClaimOverstatement","ConflictMisrepresented","UncertaintyStrengthened","ClaimMismatch"]},
          "severity":{"type":"string","enum":["Warning","Error"]},"problematicText":{"type":"string"},
          "relevantClaimIds":{"type":"array","items":{"type":"string","format":"uuid"}},"explanation":{"type":"string"}
        },"required":["sectionSequence","blockSequence","issueType","severity","problematicText","relevantClaimIds","explanation"]}}
      },"required":["status","issues"]
    }
    """)!.DeepClone();
}
