using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Domain.Pilots;

namespace YoutubeAiFactory.Application.Pilots;

public static class PilotPlanValidator
{
    public static void Validate(PilotPlanResult result, PilotGenerationContext context)
    {
        Require(result.Name, "name"); Require(result.Objective, "objective");
        if (result.ExperimentSummary is null) throw new StructuredOutputException("Pilot experiment summary is required.");
        if (result.Videos is null || result.Videos.Count != 12) throw new StructuredOutputException("A pilot must contain exactly 12 videos.");
        var ids = context.Ideas.Select(x => x.VideoIdeaId).ToHashSet(); var opportunities = context.Ideas.ToDictionary(x => x.VideoIdeaId, x => x.OpportunityId);
        if (result.Videos.Select(x => x.Sequence).Order().SequenceEqual(Enumerable.Range(1, 12)) is false) throw new StructuredOutputException("Pilot sequences must be unique and cover 1 through 12.");
        if (result.Videos.Select(x => x.VideoIdeaId).Distinct().Count() != 12) throw new StructuredOutputException("A pilot cannot use an idea more than once.");
        foreach (var video in result.Videos)
        {
            if (!ids.Contains(video.VideoIdeaId) || !opportunities.TryGetValue(video.VideoIdeaId, out var opportunity) || opportunity != video.OpportunityId) throw new StructuredOutputException("Pilot contains an unknown or mismatched idea reference.");
            if (ExpectedType(video.Sequence) != video.ExperimentType) throw new StructuredOutputException("Pilot experiment blocks must be Topic 1-4, Packaging 5-8, Storytelling 9-12.");
            Require(video.Hypothesis, "hypothesis"); Require(video.VariableBeingTested, "variable being tested"); Require(video.ControlStrategy, "control strategy"); Require(video.PrimaryMetric, "primary metric"); Require(video.SuccessSignal, "success signal"); Require(video.Rationale, "rationale");
        }
        if (result.ExperimentSummary.TopicCount != 4 || result.ExperimentSummary.PackagingCount != 4 || result.ExperimentSummary.StorytellingCount != 4) throw new StructuredOutputException("Pilot experiment summary must be 4/4/4.");
    }
    public static PilotExperimentType ExpectedType(int sequence) => sequence <= 4 ? PilotExperimentType.Topic : sequence <= 8 ? PilotExperimentType.Packaging : PilotExperimentType.Storytelling;
    private static void Require(string value, string name) { if (string.IsNullOrWhiteSpace(value)) throw new StructuredOutputException($"Pilot {name} is required."); }
}
