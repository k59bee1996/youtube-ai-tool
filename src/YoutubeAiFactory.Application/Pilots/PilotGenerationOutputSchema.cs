using System.Text.Json.Nodes;

namespace YoutubeAiFactory.Application.Pilots;

/// <summary>Provider-neutral strict contract for an executable twelve-video pilot plan.</summary>
internal static class PilotGenerationOutputSchema
{
    private const string Schema = """
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "name": { "type": "string" },
            "objective": { "type": "string" },
            "videos": { "type": "array", "items": { "$ref": "#/$defs/video" } },
            "experimentSummary": { "$ref": "#/$defs/experimentSummary" },
            "assumptions": { "$ref": "#/$defs/stringArray" },
            "limitations": { "$ref": "#/$defs/stringArray" }
          },
          "required": ["name", "objective", "videos", "experimentSummary", "assumptions", "limitations"],
          "$defs": {
            "stringArray": { "type": "array", "items": { "type": "string" } },
            "experimentSummary": {
              "type": "object",
              "additionalProperties": false,
              "properties": {
                "topicCount": { "type": "integer" },
                "packagingCount": { "type": "integer" },
                "storytellingCount": { "type": "integer" }
              },
              "required": ["topicCount", "packagingCount", "storytellingCount"]
            },
            "video": {
              "type": "object",
              "additionalProperties": false,
              "properties": {
                "sequence": { "type": "integer" },
                "videoIdeaId": { "type": "string" },
                "opportunityId": { "type": "string" },
                "experimentType": { "type": "integer", "enum": [0, 1, 2] },
                "hypothesis": { "type": "string" },
                "variableBeingTested": { "type": "string" },
                "controlStrategy": { "type": "string" },
                "primaryMetric": { "type": "string" },
                "successSignal": { "type": "string" },
                "rationale": { "type": "string" },
                "secondaryMetrics": { "$ref": "#/$defs/stringArray" },
                "notes": { "type": ["string", "null"] }
              },
              "required": ["sequence", "videoIdeaId", "opportunityId", "experimentType", "hypothesis", "variableBeingTested", "controlStrategy", "primaryMetric", "successSignal", "rationale", "secondaryMetrics", "notes"]
            }
          }
        }
        """;

    public static JsonNode Create() => JsonNode.Parse(Schema)!.DeepClone();
}
