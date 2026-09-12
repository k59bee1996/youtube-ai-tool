using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Domain.Pilots;

namespace YoutubeAiFactory.Application.Pilots;

public static class PilotPlanValidator
{
    public static void Validate(PilotPlanResult result, PilotGenerationContext context)
    {
        if (result is null) throw new StructuredOutputException("Pilot result is required.");
        Require(result.Name, "name", 300); Require(result.Objective, "objective", 4000);
        if (result.ExperimentSummary is null) throw new StructuredOutputException("Pilot experiment summary is required.");
        if (result.Videos is null || result.Videos.Count != 12) throw new StructuredOutputException("A pilot must contain exactly 12 videos.");
        if (result.Videos.Any(video => video is null)) throw new StructuredOutputException("Pilot videos cannot contain null entries.");
        RequireTextList(result.Assumptions, "assumptions"); RequireTextList(result.Limitations, "limitations");
        var ids = context.Ideas.Select(x => x.VideoIdeaId).ToHashSet(); var opportunities = context.Ideas.ToDictionary(x => x.VideoIdeaId, x => x.OpportunityId);
        if (result.Videos.Select(x => x.Sequence).Order().SequenceEqual(Enumerable.Range(1, 12)) is false) throw new StructuredOutputException("Pilot sequences must be unique and cover 1 through 12.");
        if (result.Videos.Select(x => x.VideoIdeaId).Distinct().Count() != 12) throw new StructuredOutputException("A pilot cannot use an idea more than once.");
        foreach (var video in result.Videos)
        {
            if (!ids.Contains(video.VideoIdeaId) || !opportunities.TryGetValue(video.VideoIdeaId, out var opportunity) || opportunity != video.OpportunityId) throw new StructuredOutputException("Pilot contains an unknown or mismatched idea reference.");
            if (ExpectedType(video.Sequence) != video.ExperimentType) throw new StructuredOutputException("Pilot experiment blocks must be Topic 1-4, Packaging 5-8, Storytelling 9-12.");
            Require(video.Hypothesis, "hypothesis", 4000); Require(video.VariableBeingTested, "variable being tested", 2000); Require(video.ControlStrategy, "control strategy", 2000); Require(video.PrimaryMetric, "primary metric", 500); Require(video.SuccessSignal, "success signal", 2000); Require(video.Rationale, "rationale", 4000);
        }
        if (result.ExperimentSummary.TopicCount != 4 || result.ExperimentSummary.PackagingCount != 4 || result.ExperimentSummary.StorytellingCount != 4) throw new StructuredOutputException("Pilot experiment summary must be 4/4/4.");
    }
    public static PilotExperimentType ExpectedType(int sequence) => sequence <= 4 ? PilotExperimentType.Topic : sequence <= 8 ? PilotExperimentType.Packaging : PilotExperimentType.Storytelling;
    private static void Require(string? value, string name, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new StructuredOutputException($"Pilot {name} is required.");
        if (value.Trim().Length > maximumLength) throw new StructuredOutputException($"Pilot {name} must be at most {maximumLength} characters.");
    }

    private static void RequireTextList(IReadOnlyList<string>? values, string name)
    {
        if (values is null) throw new StructuredOutputException($"Pilot {name} are required.");
        if (values.Count > 20) throw new StructuredOutputException($"Pilot {name} cannot contain more than 20 items.");
        foreach (var value in values) Require(value, name, 1000);
    }
}
