using System.Text.Json.Nodes;

namespace YoutubeAiFactory.Application.Localization;

internal static class CompetitorAnalysisLocalizationOutputSchema
{
    private const string Schema = """
        {
          "type": "object", "additionalProperties": false,
          "properties": {
            "audience": { "$ref": "#/$defs/audience" },
            "topicClusters": { "type": "array", "items": { "$ref": "#/$defs/topicCluster" } },
            "titlePatterns": { "type": "array", "items": { "$ref": "#/$defs/titlePattern" } },
            "thumbnailPatterns": { "type": "array", "items": { "$ref": "#/$defs/evidencePattern" } },
            "hookPatterns": { "type": "array", "items": { "$ref": "#/$defs/evidencePattern" } },
            "contentFormats": { "type": "array", "items": { "$ref": "#/$defs/contentFormat" } },
            "performanceInsights": { "type": "array", "items": { "$ref": "#/$defs/performanceInsight" } },
            "potentialWeaknesses": { "type": "array", "items": { "$ref": "#/$defs/weaknessInsight" } },
            "transferableFormats": { "type": "array", "items": { "$ref": "#/$defs/transferableFormat" } },
            "evidenceNotes": { "type": "array", "items": { "$ref": "#/$defs/evidenceNote" } },
            "confidence": { "$ref": "#/$defs/confidence" }
          },
          "required": ["audience", "topicClusters", "titlePatterns", "thumbnailPatterns", "hookPatterns", "contentFormats", "performanceInsights", "potentialWeaknesses", "transferableFormats", "evidenceNotes", "confidence"],
          "$defs": {
            "stringArray": { "type": "array", "items": { "type": "string" } },
            "audience": { "type": "object", "additionalProperties": false, "properties": { "likelyAgeRange": { "type": ["string", "null"] }, "likelyInterests": { "$ref": "#/$defs/stringArray" }, "likelyViewerIntent": { "$ref": "#/$defs/stringArray" }, "geographyHints": { "$ref": "#/$defs/stringArray" }, "evidence": { "$ref": "#/$defs/stringArray" } }, "required": ["likelyAgeRange", "likelyInterests", "likelyViewerIntent", "geographyHints", "evidence"] },
            "topicCluster": { "type": "object", "additionalProperties": false, "properties": { "name": { "type": "string" }, "description": { "type": "string" }, "performanceSignal": { "type": "string" } }, "required": ["name", "description", "performanceSignal"] },
            "titlePattern": { "type": "object", "additionalProperties": false, "properties": { "patternName": { "type": "string" }, "description": { "type": "string" }, "performanceSignal": { "type": "string" } }, "required": ["patternName", "description", "performanceSignal"] },
            "evidencePattern": { "type": "object", "additionalProperties": false, "properties": { "patternName": { "type": "string" }, "observation": { "type": "string" }, "limitations": { "$ref": "#/$defs/stringArray" } }, "required": ["patternName", "observation", "limitations"] },
            "contentFormat": { "type": "object", "additionalProperties": false, "properties": { "format": { "type": "string" }, "performanceSignal": { "type": "string" } }, "required": ["format", "performanceSignal"] },
            "performanceInsight": { "type": "object", "additionalProperties": false, "properties": { "insight": { "type": "string" } }, "required": ["insight"] },
            "weaknessInsight": { "type": "object", "additionalProperties": false, "properties": { "observation": { "type": "string" } }, "required": ["observation"] },
            "transferableFormat": { "type": "object", "additionalProperties": false, "properties": { "format": { "type": "string" }, "whyItMayWork": { "type": "string" }, "transferableMechanic": { "type": "string" }, "doNotCopy": { "type": "string" } }, "required": ["format", "whyItMayWork", "transferableMechanic", "doNotCopy"] },
            "evidenceNote": { "type": "object", "additionalProperties": false, "properties": { "note": { "type": "string" } }, "required": ["note"] },
            "confidence": { "type": "object", "additionalProperties": false, "properties": { "dataQuality": { "type": "string" }, "limitations": { "$ref": "#/$defs/stringArray" } }, "required": ["dataQuality", "limitations"] }
          }
        }
        """;

    public static JsonNode Create() => JsonNode.Parse(Schema)!.DeepClone();
}

internal static class OpportunityReportLocalizationOutputSchema
{
    private const string Schema = """
        {
          "type": "object", "additionalProperties": false,
          "properties": {
            "limitations": { "$ref": "#/$defs/stringArray" },
            "opportunities": { "type": "array", "items": { "$ref": "#/$defs/opportunity" } }
          },
          "required": ["limitations", "opportunities"],
          "$defs": {
            "stringArray": { "type": "array", "items": { "type": "string" } },
            "opportunity": { "type": "object", "additionalProperties": false, "properties": { "name": { "type": "string" }, "description": { "type": "string" }, "audience": { "type": "string" }, "topic": { "type": "string" }, "contentFormat": { "type": "string" }, "angle": { "type": "string" }, "whyThisOpportunity": { "type": "string" }, "evidenceSummaries": { "$ref": "#/$defs/stringArray" }, "risks": { "$ref": "#/$defs/stringArray" }, "limitations": { "$ref": "#/$defs/stringArray" } }, "required": ["name", "description", "audience", "topic", "contentFormat", "angle", "whyThisOpportunity", "evidenceSummaries", "risks", "limitations"] }
          }
        }
        """;

    public static JsonNode Create() => JsonNode.Parse(Schema)!.DeepClone();
}
