namespace YoutubeAiFactory.Application.AI;

/// <summary>Explicit product policy for the intelligence required by each implemented AI workflow.</summary>
public static class AiWorkflowProfiles
{
    public static AiModelProfile CompetitorAnalysis => AiModelProfile.Reasoning;
    public static AiModelProfile OpportunityAnalysis => AiModelProfile.Premium;
    public static AiModelProfile IdeaGeneration => AiModelProfile.Reasoning;
    public static AiModelProfile PilotGeneration => AiModelProfile.Premium;
    public static AiModelProfile ArtifactLocalization => AiModelProfile.Fast;
    public static AiModelProfile StructuredOutputRepair => AiModelProfile.Fast;
}
