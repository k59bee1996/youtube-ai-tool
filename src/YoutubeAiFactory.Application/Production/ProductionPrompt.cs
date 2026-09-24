using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using YoutubeAiFactory.Application.AI;

namespace YoutubeAiFactory.Application.Production;

public static class ProductionPrompt
{
    public const string GenerationKey = "production-package";
    public const int GenerationVersion = 1;
    public const string AuditKey = "production-grounding-audit";
    public const int AuditVersion = 1;
    public const string CorrectionKey = "production-package-correction";
    public const int CorrectionVersion = 1;
    public const string RepairKey = "structured-production-package-repair";
    public const int RepairVersion = 1;
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static LlmRequest Generate(
        ProductionGenerationContext context,
        string? diagnostic = null,
        ProductionPackageResult? prior = null
    ) =>
        new(
            GenerationKey,
            GenerationVersion,
            """
            Convert the approved Script into a bounded production plan. Preserve narration exactly: do not rewrite, translate, summarize as replacement copy, add claims, or use outside knowledge. Map every ScriptBlock ID exactly once and in order; a scene may contain one or more contiguous blocks. Build executable scenes, shots, reusable asset requirements, on-screen text, motion, transitions, audio and voice direction. Claim IDs may only come from the mapped blocks. Evidence-based depictions, factual names/numbers/dates/quotes/statistics, charts, and maps require claims. Mark reconstructions explicitly. Generated visuals must never imply authentic archival evidence. Sourced media must require rights verification. Use relative shot weights; the application computes exact timing. Respect the Pilot experiment and favor bounded, reusable assets. Return JSON only.
            """,
            JsonSerializer.Serialize(
                new
                {
                    context,
                    diagnostic,
                    prior,
                },
                SerializerOptions
            ),
            new Dictionary<string, string> { { "max_output_tokens", "16000" } },
            GenerationSchema(),
            AiWorkflowProfiles.ProductionPackageGeneration
        );

    public static LlmRequest Audit(
        ProductionGenerationContext context,
        ProductionPackageResult package
    ) =>
        new(
            AuditKey,
            AuditVersion,
            """
            Audit only factual and visual grounding of this production plan against its approved Script blocks and supplied claims. Flag unsupported visual facts, text, numbers, dates, quotes, charts, maps, contradictions, hidden conflicts, claim mismatch, and false archival impressions. Return Passed only with no Error issues. Do not rewrite the package and do not use outside knowledge. Return JSON only.
            """,
            JsonSerializer.Serialize(new { context, package }, SerializerOptions),
            new Dictionary<string, string> { { "max_output_tokens", "6000" } },
            AuditSchema(),
            AiWorkflowProfiles.ProductionGroundingAudit
        );

    public static LlmRequest Correct(
        ProductionGenerationContext context,
        ProductionPackageResult package,
        IReadOnlyList<ProductionGroundingIssueResult> issues
    ) =>
        new(
            CorrectionKey,
            CorrectionVersion,
            """
            Correct only the listed grounding defects. Preserve ScriptBlock mapping, exact narration source, lineage, ordering, Pilot intent, valid claims and bounded scope. Do not add facts or media. Return the complete corrected production package as JSON.
            """,
            JsonSerializer.Serialize(
                new
                {
                    context,
                    package,
                    issues,
                },
                SerializerOptions
            ),
            new Dictionary<string, string> { { "max_output_tokens", "16000" } },
            GenerationSchema(),
            AiWorkflowProfiles.ProductionPackageCorrection
        );

    public static LlmRequest Repair(string malformed, string diagnostic) =>
        new(
            RepairKey,
            RepairVersion,
            "Repair malformed JSON into the production package contract without inventing or altering production content, IDs, claims, mappings, or instructions. Return JSON only.",
            JsonSerializer.Serialize(new { malformed, diagnostic }, SerializerOptions),
            new Dictionary<string, string> { { "max_output_tokens", "16000" } },
            GenerationSchema(),
            AiWorkflowProfiles.StructuredOutputRepair
        );

    public static JsonNode GenerationSchema() =>
        JsonNode
            .Parse(
                """
                {"type":"object","additionalProperties":false,"properties":{
                  "visualDirection":{"type":"string"},"pacingDirection":{"type":"string"},"colorDirection":{"type":"string"},"typographyDirection":{"type":"string"},"audioDirection":{"type":"string"},"experimentProductionNotes":{"type":"string"},
                  "scenes":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{
                    "sequence":{"type":"integer","minimum":1},"purpose":{"type":"string","enum":["Hook","Establish","Explain","Escalate","Demonstrate","Compare","Conflict","Transition","Payoff","Conclusion"]},"narrationSummary":{"type":"string"},"visualStrategy":{"type":"string"},"complexity":{"type":"string","enum":["Low","Medium","High"]},"transitionIntent":{"type":"string"},"musicBrief":{"type":"string"},"soundEffectCue":{"type":"string"},"voiceDirection":{"type":"string"},"scriptBlockIds":{"type":"array","minItems":1,"items":{"type":"string","format":"uuid"}},
                    "shots":{"type":"array","minItems":1,"items":{"type":"object","additionalProperties":false,"properties":{"sequence":{"type":"integer","minimum":1},"shotType":{"type":"string","enum":["GeneratedStill","GeneratedMotion","StockFootage","ArchivalImage","ArchivalVideo","Illustration","Diagram","Chart","Map","TextGraphic","ScreenCapture","SimpleBackground"]},"visualDescription":{"type":"string"},"composition":{"type":"string"},"motionSuggestion":{"type":"string"},"relativeDurationWeight":{"type":"number","exclusiveMinimum":0},"factualityMode":{"type":"string","enum":["GenericAtmosphere","IllustrativeReconstruction","EvidenceBasedDepiction","DataVisualization","TextOnly"]},"assetKey":{"type":["string","null"]},"claimIds":{"type":"array","items":{"type":"string","format":"uuid"}},"notes":{"type":"string"}},"required":["sequence","shotType","visualDescription","composition","motionSuggestion","relativeDurationWeight","factualityMode","assetKey","claimIds","notes"]}},
                    "onScreenText":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"sequence":{"type":"integer","minimum":1},"text":{"type":"string"},"type":{"type":"string","enum":["Decorative","SectionCue","Explanation","Name","Number","Date","Quote","Statistic"]},"timingIntent":{"type":"string"},"claimIds":{"type":"array","items":{"type":"string","format":"uuid"}}},"required":["sequence","text","type","timingIntent","claimIds"]}}
                  },"required":["sequence","purpose","narrationSummary","visualStrategy","complexity","transitionIntent","musicBrief","soundEffectCue","voiceDirection","scriptBlockIds","shots","onScreenText"]}},
                  "assetRequirements":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"assetKey":{"type":"string"},"assetType":{"type":"string","enum":["Image","Video","Illustration","Diagram","Chart","Map","TextGraphic","ScreenCapture","Background"]},"acquisitionMode":{"type":"string","enum":["Generate","CreateGraphic","SourceLicensed","SourcePublicDomain","Capture","ExistingAsset"]},"creativeBrief":{"type":"string"},"generationPrompt":{"type":"string"},"sourceSearchBrief":{"type":"string"},"rightsVerificationRequired":{"type":"boolean"},"factualityMode":{"type":"string","enum":["GenericAtmosphere","IllustrativeReconstruction","EvidenceBasedDepiction","DataVisualization","TextOnly"]},"reuseKey":{"type":"string"},"complexity":{"type":"string","enum":["Low","Medium","High"]},"claimIds":{"type":"array","items":{"type":"string","format":"uuid"}}},"required":["assetKey","assetType","acquisitionMode","creativeBrief","generationPrompt","sourceSearchBrief","rightsVerificationRequired","factualityMode","reuseKey","complexity","claimIds"]}},
                  "warnings":{"type":"array","items":{"type":"string"}}
                },"required":["visualDirection","pacingDirection","colorDirection","typographyDirection","audioDirection","experimentProductionNotes","scenes","assetRequirements","warnings"]}
                """
            )!
            .DeepClone();

    public static JsonNode AuditSchema() =>
        JsonNode
            .Parse(
                """
                {"type":"object","additionalProperties":false,"properties":{"status":{"type":"string","enum":["Passed","Failed"]},"issues":{"type":"array","items":{"type":"object","additionalProperties":false,"properties":{"sceneSequence":{"type":"integer","minimum":1},"shotSequence":{"type":["integer","null"]},"assetKey":{"type":["string","null"]},"onScreenTextSequence":{"type":["integer","null"]},"issueType":{"type":"string","enum":["UnsupportedVisualFact","UnsupportedOnScreenText","UnsupportedNumber","UnsupportedDate","UnsupportedMap","UnsupportedChart","VisualScriptContradiction","ConflictMisrepresented","FalseArchivalImpression","ClaimMismatch"]},"severity":{"type":"string","enum":["Warning","Error"]},"problematicText":{"type":"string"},"relevantClaimIds":{"type":"array","items":{"type":"string","format":"uuid"}},"explanation":{"type":"string"}},"required":["sceneSequence","shotSequence","assetKey","onScreenTextSequence","issueType","severity","problematicText","relevantClaimIds","explanation"]}}},"required":["status","issues"]}
                """
            )!
            .DeepClone();
}
