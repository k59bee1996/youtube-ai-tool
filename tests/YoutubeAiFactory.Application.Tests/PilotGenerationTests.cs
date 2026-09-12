using YoutubeAiFactory.Application.Common;
using YoutubeAiFactory.Application.Pilots;
using YoutubeAiFactory.Domain.Ideas;
using YoutubeAiFactory.Domain.Opportunities;
using YoutubeAiFactory.Domain.Pilots;

namespace YoutubeAiFactory.Application.Tests;

public sealed class PilotGenerationTests
{
    [Fact]
    public void Validator_accepts_exactly_four_experiments_in_each_required_block()
    {
        var context = Context(); var plan = Plan(context);
        PilotPlanValidator.Validate(plan, context);
    }

    [Fact]
    public void Validator_rejects_duplicate_or_unknown_idea_ids()
    {
        var context = Context(); var plan = Plan(context);
        var duplicate = plan with { Videos = plan.Videos.Select((video, index) => index == 1 ? video with { VideoIdeaId = plan.Videos[0].VideoIdeaId, OpportunityId = plan.Videos[0].OpportunityId } : video).ToArray() };
        Assert.Throws<StructuredOutputException>(() => PilotPlanValidator.Validate(duplicate, context));
        var fabricated = plan with { Videos = plan.Videos.Select((video, index) => index == 0 ? video with { VideoIdeaId = Guid.NewGuid() } : video).ToArray() };
        Assert.Throws<StructuredOutputException>(() => PilotPlanValidator.Validate(fabricated, context));
    }

    [Fact]
    public void Validator_rejects_missing_assumptions_or_limitations()
    {
        var context = Context(); var plan = Plan(context);

        Assert.Throws<StructuredOutputException>(() => PilotPlanValidator.Validate(plan with { Assumptions = null! }, context));
        Assert.Throws<StructuredOutputException>(() => PilotPlanValidator.Validate(plan with { Limitations = null! }, context));
    }

    [Theory]
    [InlineData(11)]
    [InlineData(41)]
    public void Options_reject_candidate_contexts_that_cannot_form_or_bound_a_pilot(int maxCandidateIdeas)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PilotGenerationOptions { MaxCandidateIdeas = maxCandidateIdeas }.Validate());
    }

    [Fact]
    public void Balance_analysis_warns_when_topic_tests_are_concentrated()
    {
        var context = Context(sameTopic: true); var warnings = PilotBalanceAnalyzer.Analyze(Plan(context), context);
        Assert.Contains(warnings, warning => warning.Contains("same subject", StringComparison.Ordinal));
    }

    [Fact]
    public void Balance_analysis_uses_the_current_selected_ideas()
    {
        var context = Context(sameTopic: true);
        var original = PilotBalanceAnalyzer.Analyze(Plan(context), context);
        var replacement = context.Ideas.Select((idea, index) => index == 3 ? idea with { Topic = "Different subject" } : idea).ToArray();
        var updated = PilotBalanceAnalyzer.Analyze(Plan(context).Videos.Select(x => x.VideoIdeaId), replacement);
        Assert.Contains(original, warning => warning.Contains("same subject", StringComparison.Ordinal));
        Assert.DoesNotContain(updated, warning => warning.Contains("same subject", StringComparison.Ordinal));
    }

    [Fact]
    public void Balance_analysis_uses_sequence_blocks_when_the_provider_returns_videos_out_of_order()
    {
        var context = Context();
        var reorderedContext = context with { Ideas = context.Ideas.Select((idea, index) => index is >= 4 and < 8 ? idea with { Topic = "Repeated later topic" } : idea).ToArray() };
        var plan = Plan(reorderedContext);
        var unordered = plan with { Videos = plan.Videos.OrderByDescending(video => video.Sequence).ToArray() };

        var warnings = PilotBalanceAnalyzer.Analyze(unordered, reorderedContext);

        Assert.DoesNotContain(warnings, warning => warning.Contains("same subject", StringComparison.Ordinal));
    }

    [Fact]
    public void Correction_prompt_includes_the_validation_failure_and_previous_candidate()
    {
        var request = PilotGenerationPrompt.Create(Context(), new PilotOutputCorrection("Pilot sequences must be unique.", "{\"videos\":[]}"));

        Assert.Contains("Pilot sequences must be unique.", request.UserContent, StringComparison.Ordinal);
        Assert.Contains("previousOutput", request.UserContent, StringComparison.Ordinal);
        Assert.Contains("videos", request.UserContent, StringComparison.Ordinal);
    }

    private static PilotGenerationContext Context(bool sameTopic = false)
    {
        var opportunity = Guid.NewGuid(); var ideas = Enumerable.Range(1, 12).Select(index => new PilotIdeaContext(Guid.NewGuid(), opportunity, "Historical economics", $"Idea {index}", sameTopic ? "Castle" : $"Topic {index}", $"Angle {index}", index % 2 == 0 ? "Explainer" : "Case study", "History fans", $"Hook {index}", $"Thumbnail {index}", "Promise", "A testable idea hypothesis", 80 - index, 70, index <= 4 ? 60 : 80, 75, 70, IdeaDecisionStatus.Approved, OpportunityDecisionStatus.Approved, 1, 1, 12)).ToArray();
        return new PilotGenerationContext(Guid.NewGuid(), "History", "History fans", ideas.Length, ideas);
    }

    private static PilotPlanResult Plan(PilotGenerationContext context) => new("Pilot", "Learn", context.Ideas.Select((idea, index) => new PilotVideoResult(index + 1, idea.VideoIdeaId, idea.OpportunityId, PilotPlanValidator.ExpectedType(index + 1), "Variable improves the expected result because the audience recognizes the concept.", $"Variable {index + 1}", "Keep the other major dimensions comparable.", "CTR", "Compare against the relevant pilot baseline.", "Creates controlled variation.")).ToArray(), new(4, 4, 4), [], []);
}
