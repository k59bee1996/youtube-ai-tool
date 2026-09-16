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
    public static AiModelProfile ResearchQueryPlanning => AiModelProfile.Reasoning;
    public static AiModelProfile ResearchSourceRelevance => AiModelProfile.Fast;
    public static AiModelProfile ResearchEvidenceExtraction => AiModelProfile.Reasoning;
    public static AiModelProfile ResearchContradictionAnalysis => AiModelProfile.Premium;
    public static AiModelProfile ResearchSynthesis => AiModelProfile.Premium;
    public static AiModelProfile OutlineGeneration => AiModelProfile.Reasoning;
}
