using System.Text.Json.Nodes;

namespace YoutubeAiFactory.Application.Opportunities;

internal static class OpportunityAnalysisOutputSchema
{
    private const string Schema = """
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "opportunities": { "type": "array", "items": { "$ref": "#/$defs/opportunity" } },
            "limitations": { "$ref": "#/$defs/stringArray" }
          },
          "required": ["opportunities", "limitations"],
          "$defs": {
            "stringArray": { "type": "array", "items": { "type": "string" } },
            "opportunity": {
              "type": "object",
              "additionalProperties": false,
              "properties": {
                "name": { "type": "string" },
                "description": { "type": "string" },
                "audience": { "type": "string" },
                "topic": { "type": "string" },
                "contentFormat": { "type": "string" },
                "angle": { "type": "string" },
                "whyThisOpportunity": { "type": "string" },
                "noveltySignal": { "type": "integer" },
                "audienceFitSignal": { "type": "integer" },
                "transferabilitySignal": { "type": "integer" },
                "storyPotential": { "type": "integer" },
                "productionComplexity": { "type": "integer" },
                "confidence": { "type": "integer" },
                "evidenceIds": { "$ref": "#/$defs/stringArray" },
                "risks": { "$ref": "#/$defs/stringArray" },
                "limitations": { "$ref": "#/$defs/stringArray" }
              },
              "required": ["name", "description", "audience", "topic", "contentFormat", "angle", "whyThisOpportunity", "noveltySignal", "audienceFitSignal", "transferabilitySignal", "storyPotential", "productionComplexity", "confidence", "evidenceIds", "risks", "limitations"]
            }
          }
        }
        """;

    public static JsonNode Create() => JsonNode.Parse(Schema)!.DeepClone();
}
