using YoutubeAiFactory.Application.AI;
using YoutubeAiFactory.Application.Competitors;
using YoutubeAiFactory.Application.Ideas;
using YoutubeAiFactory.Application.Localization;
using YoutubeAiFactory.Application.Opportunities;
using YoutubeAiFactory.Application.Outlines;
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
        Assert.Equal(AiModelProfile.Reasoning, AiWorkflowProfiles.ResearchQueryPlanning);
        Assert.Equal(AiModelProfile.Fast, AiWorkflowProfiles.ResearchSourceRelevance);
        Assert.Equal(AiModelProfile.Reasoning, AiWorkflowProfiles.ResearchEvidenceExtraction);
        Assert.Equal(AiModelProfile.Premium, AiWorkflowProfiles.ResearchContradictionAnalysis);
        Assert.Equal(AiModelProfile.Premium, AiWorkflowProfiles.ResearchSynthesis);
        Assert.Equal(AiModelProfile.Reasoning, AiWorkflowProfiles.OutlineGeneration);
        Assert.Equal(AiModelProfile.Premium, AiWorkflowProfiles.ScriptGeneration);
        Assert.Equal(AiModelProfile.Reasoning, AiWorkflowProfiles.ScriptGroundingAudit);
        Assert.Equal(AiModelProfile.Premium, AiWorkflowProfiles.ScriptGroundingCorrection);
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
        var context = new OutlineGenerationContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, "research", "outline",
            "Title", "Topic", "Angle", "Explainer", "Audience", "Promise", "Hook", "English", "Global",
            YoutubeAiFactory.Domain.Pilots.PilotExperimentType.Packaging, "Hypothesis", "Variable", "Control", "CTR", "CTR improves",
            "Summary", [], [], [], [], [], [], []);
        Assert.Equal(AiWorkflowProfiles.OutlineGeneration, OutlinePrompt.Create(context).ModelProfile);
        Assert.Equal(AiWorkflowProfiles.StructuredOutputRepair, OutlinePrompt.CreateRepair("{}", "invalid").ModelProfile);
    }
}
