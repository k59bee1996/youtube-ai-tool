using System.Text.Json.Nodes;

namespace YoutubeAiFactory.Application.Competitors;

internal static class CompetitorAnalysisOutputSchema
{
    private const string Schema = """
        {
          "type": "object",
          "additionalProperties": false,
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
            "confidence": { "$ref": "#/$defs/analysisConfidence" }
          },
          "required": ["audience", "topicClusters", "titlePatterns", "thumbnailPatterns", "hookPatterns", "contentFormats", "performanceInsights", "potentialWeaknesses", "transferableFormats", "evidenceNotes", "confidence"],
          "$defs": {
            "stringArray": { "type": "array", "items": { "type": "string" } },
            "uuidArray": { "type": "array", "items": { "type": "string", "format": "uuid" } },
            "audience": {
              "type": "object", "additionalProperties": false,
              "properties": {
                "likelyAgeRange": { "type": ["string", "null"] }, "likelyInterests": { "$ref": "#/$defs/stringArray" }, "likelyViewerIntent": { "$ref": "#/$defs/stringArray" }, "geographyHints": { "$ref": "#/$defs/stringArray" }, "confidence": { "type": "integer" }, "evidence": { "$ref": "#/$defs/stringArray" }
              },
              "required": ["likelyAgeRange", "likelyInterests", "likelyViewerIntent", "geographyHints", "confidence", "evidence"]
            },
            "topicCluster": {
              "type": "object", "additionalProperties": false,
              "properties": { "name": { "type": "string" }, "description": { "type": "string" }, "exampleVideoIds": { "$ref": "#/$defs/uuidArray" }, "frequency": { "type": "integer" }, "performanceSignal": { "type": "string" }, "confidence": { "type": "integer" } },
              "required": ["name", "description", "exampleVideoIds", "frequency", "performanceSignal", "confidence"]
            },
            "titlePattern": {
              "type": "object", "additionalProperties": false,
              "properties": { "patternName": { "type": "string" }, "description": { "type": "string" }, "template": { "type": "string" }, "exampleTitles": { "$ref": "#/$defs/stringArray" }, "observedFrequency": { "type": "integer" }, "performanceSignal": { "type": "string" }, "confidence": { "type": "integer" } },
              "required": ["patternName", "description", "template", "exampleTitles", "observedFrequency", "performanceSignal", "confidence"]
            },
            "evidencePattern": {
              "type": "object", "additionalProperties": false,
              "properties": { "patternName": { "type": "string" }, "observation": { "type": "string" }, "evidenceVideoIds": { "$ref": "#/$defs/uuidArray" }, "confidence": { "type": "integer" }, "limitations": { "$ref": "#/$defs/stringArray" } },
              "required": ["patternName", "observation", "evidenceVideoIds", "confidence", "limitations"]
            },
            "contentFormat": {
              "type": "object", "additionalProperties": false,
              "properties": { "format": { "type": "string" }, "evidenceVideoIds": { "$ref": "#/$defs/uuidArray" }, "performanceSignal": { "type": "string" }, "confidence": { "type": "integer" } },
              "required": ["format", "evidenceVideoIds", "performanceSignal", "confidence"]
            },
            "performanceInsight": {
              "type": "object", "additionalProperties": false,
              "properties": { "insight": { "type": "string" }, "supportingVideoIds": { "$ref": "#/$defs/uuidArray" }, "confidence": { "type": "integer" } },
              "required": ["insight", "supportingVideoIds", "confidence"]
            },
            "weaknessInsight": {
              "type": "object", "additionalProperties": false,
              "properties": { "observation": { "type": "string" }, "supportingVideoIds": { "$ref": "#/$defs/uuidArray" }, "confidence": { "type": "integer" } },
              "required": ["observation", "supportingVideoIds", "confidence"]
            },
            "transferableFormat": {
              "type": "object", "additionalProperties": false,
              "properties": { "format": { "type": "string" }, "whyItMayWork": { "type": "string" }, "evidenceVideoIds": { "$ref": "#/$defs/uuidArray" }, "transferableMechanic": { "type": "string" }, "doNotCopy": { "type": "string" }, "confidence": { "type": "integer" } },
              "required": ["format", "whyItMayWork", "evidenceVideoIds", "transferableMechanic", "doNotCopy", "confidence"]
            },
            "evidenceNote": {
              "type": "object", "additionalProperties": false,
              "properties": { "note": { "type": "string" }, "videoIds": { "$ref": "#/$defs/uuidArray" } },
              "required": ["note", "videoIds"]
            },
            "analysisConfidence": {
              "type": "object", "additionalProperties": false,
              "properties": { "overallConfidence": { "type": "integer" }, "dataQuality": { "type": "string" }, "limitations": { "$ref": "#/$defs/stringArray" } },
              "required": ["overallConfidence", "dataQuality", "limitations"]
            }
          }
        }
        """;

    public static JsonNode Create() => JsonNode.Parse(Schema)!.DeepClone();
}
