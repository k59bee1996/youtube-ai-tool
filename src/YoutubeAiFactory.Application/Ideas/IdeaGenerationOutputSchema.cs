using System.Text.Json.Nodes;

namespace YoutubeAiFactory.Application.Ideas;

/// <summary>Provider-neutral strict contract for persisted idea-generation candidates.</summary>
internal static class IdeaGenerationOutputSchema
{
    private const string Schema = """
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "ideas": { "type": "array", "items": { "$ref": "#/$defs/idea" } }
          },
          "required": ["ideas"],
          "$defs": {
            "stringArray": { "type": "array", "items": { "type": "string" } },
            "uuidArray": { "type": "array", "items": { "type": "string" } },
            "features": {
              "type": "object",
              "additionalProperties": false,
              "properties": {
                "novelty": { "type": "integer" },
                "titlePotential": { "type": "integer" },
                "thumbnailPotential": { "type": "integer" },
                "storyPotential": { "type": "integer" },
                "audienceFit": { "type": "integer" },
                "productionComplexity": { "type": "integer" },
                "competitionRisk": { "type": "integer" },
                "researchRisk": { "type": "integer" }
              },
              "required": ["novelty", "titlePotential", "thumbnailPotential", "storyPotential", "audienceFit", "productionComplexity", "competitionRisk", "researchRisk"]
            },
            "idea": {
              "type": "object",
              "additionalProperties": false,
              "properties": {
                "workingTitle": { "type": "string" },
                "topic": { "type": "string" },
                "angle": { "type": "string" },
                "contentFormat": { "type": "string" },
                "targetAudience": { "type": "string" },
                "viewerIntent": { "type": "string" },
                "hookConcept": { "type": "string" },
                "thumbnailConcept": { "type": "string" },
                "viewerPromise": { "type": "string" },
                "coreQuestion": { "type": "string" },
                "whyViewerWouldCare": { "type": "string" },
                "hypothesis": { "type": "string" },
                "features": { "$ref": "#/$defs/features" },
                "evidenceIds": { "$ref": "#/$defs/uuidArray" },
                "risks": { "$ref": "#/$defs/stringArray" },
                "confidence": { "type": "integer" }
              },
              "required": ["workingTitle", "topic", "angle", "contentFormat", "targetAudience", "viewerIntent", "hookConcept", "thumbnailConcept", "viewerPromise", "coreQuestion", "whyViewerWouldCare", "hypothesis", "features", "evidenceIds", "risks", "confidence"]
            }
          }
        }
        """;

    public static JsonNode Create() => JsonNode.Parse(Schema)!.DeepClone();
}
