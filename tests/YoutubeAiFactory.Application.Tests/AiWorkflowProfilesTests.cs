using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Ideas;
using YoutubeAiFactory.Application.Localization;
using YoutubeAiFactory.Application.Opportunities;
using YoutubeAiFactory.Application.Pilots;

namespace YoutubeAiFactory.Application.Tests;

public sealed class AiWorkflowProfilesTests
{
    [Fact]
    public void Implemented_workflows_request_their_explicit_intelligence_profiles()
    {
        Assert.Equal(AiModelProfile.Reasoning, AiWorkflowProfiles.CompetitorAnalysis);
        Assert.Equal(AiModelProfile.Premium, AiWorkflowProfiles.OpportunityAnalysis);
        Assert.Equal(AiModelProfile.Reasoning, AiWorkflowProfiles.IdeaGeneration);
        Assert.Equal(AiModelProfile.Premium, AiWorkflowProfiles.PilotGeneration);
        Assert.Equal(AiModelProfile.Fast, AiWorkflowProfiles.ArtifactLocalization);
        Assert.Equal(AiModelProfile.Fast, AiWorkflowProfiles.StructuredOutputRepair);
    }

    [Fact]
    public void Workflow_prompt_contracts_bind_to_the_same_explicit_policy()
    {
        Assert.Equal(AiWorkflowProfiles.CompetitorAnalysis, CompetitorAnalysisPrompt.ModelProfile);
        Assert.Equal(AiWorkflowProfiles.OpportunityAnalysis, OpportunityAnalysisPrompt.ModelProfile);
        Assert.Equal(AiWorkflowProfiles.IdeaGeneration, IdeaGenerationPrompt.ModelProfile);
        Assert.Equal(AiWorkflowProfiles.PilotGeneration, PilotGenerationPrompt.ModelProfile);
        Assert.Equal(AiWorkflowProfiles.ArtifactLocalization, CompetitorAnalysisLocalizationPrompt.ModelProfile);
        Assert.Equal(AiWorkflowProfiles.ArtifactLocalization, OpportunityReportLocalizationPrompt.ModelProfile);
    }
}
